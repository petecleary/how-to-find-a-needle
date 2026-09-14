using System.Diagnostics;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using PI.SearchApi.Pipeline;

namespace PI.SearchApi.Embeddings;

/// <summary>
/// Applies the provider's rules around <see cref="IEmbeddingGenerator{TInput, TEmbedding}"/> (ADR-0009).
/// </summary>
/// <remarks>
/// Nomic Embed is an <em>asymmetric</em> model: it was trained with a task prefix on every input, so a
/// query and a document are embedded differently on purpose. Leave the prefixes off and search still
/// "works" — just noticeably worse. That's why they're applied here, once, and never by callers.
/// </remarks>
public sealed class SearchEmbedder(
    IEmbeddingGenerator<string, Embedding<float>> generator,
    IOptions<EmbeddingsOptions> options) : ISearchEmbedder
{
    public const string QueryPrefix = "search_query: ";
    public const string DocumentPrefix = "search_document: ";

    public string Provider => options.Value.Provider;

    public string ModelId => generator.GetService<EmbeddingGeneratorMetadata>()?.DefaultModelId ?? "unknown";

    public async Task<QueryEmbedding> EmbedQueryAsync(string query, CancellationToken ct)
    {
        using var activity = PipelineTelemetry.Source.StartActivity("Embed query");
        var start = Stopwatch.GetTimestamp();

        EnsureProviderIsBuilt();

        var text = QueryPrefix + query;
        var embeddings = await generator.GenerateAsync([text], cancellationToken: ct);
        var embedding = embeddings[0];
        var vector = embedding.Vector.ToArray();

        var step = new TraceStep
        {
            Stage = "vector",
            Title = "Query embedding (Nomic Embed v1.5, ONNX Runtime)",
            DurationMs = PipelineTelemetry.ElapsedMs(start),
            Details = new Dictionary<string, object?>
            {
                ["provider"] = Provider,
                ["model"] = ModelId,
                ["prefix"] = QueryPrefix,
                ["embeddedText"] = text,
                ["tokenCount"] = embedding.AdditionalProperties?.GetValueOrDefault("tokenCount"),
                ["truncated"] = embedding.AdditionalProperties?.GetValueOrDefault("truncated"),
                ["dimensions"] = vector.Length,
                // Illustration only: 8 of 768 numbers. No single dimension "means" anything on its own.
                ["firstDimensions"] = vector.Take(8).Select(v => Math.Round(v, 4)).ToArray(),
            },
            Notes =
            [
                "Tokenizer → network → mean pooling → L2 normalisation. The vector has length 1, so cosine similarity is a dot product.",
                "Nomic is asymmetric: queries get \"search_query: \" and products get \"search_document: \".",
            ],
        };

        return new QueryEmbedding(vector, step);
    }

    public async Task<IReadOnlyList<float[]>> EmbedDocumentsAsync(IReadOnlyList<string> documents, CancellationToken ct)
    {
        EnsureProviderIsBuilt();

        var embeddings = await generator.GenerateAsync(documents.Select(d => DocumentPrefix + d), cancellationToken: ct);

        return [.. embeddings.Select(e => e.Vector.ToArray())];
    }

    private void EnsureProviderIsBuilt()
    {
        if (!string.Equals(Provider, "nomic", StringComparison.OrdinalIgnoreCase))
        {
            throw new EmbeddingModelUnavailableException(
                $"Embeddings:Provider is '{Provider}', but only the local 'nomic' provider is built so far " +
                "(the OpenAI provider arrives in roadmap Phase 5). Set Embeddings:Provider to 'nomic'.");
        }
    }
}

using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using PI.SearchApi.Embeddings;
using Xunit;

namespace PI.SearchApi.Tests.Embeddings;

public sealed class SearchEmbedderTests
{
    [Fact]
    public async Task EmbedQueryAsync_AddsTheSearchQueryPrefix()
    {
        var generator = new RecordingGenerator();
        var embedder = new SearchEmbedder(generator, Options.Create(new EmbeddingsOptions()));

        var result = await embedder.EmbedQueryAsync("power brick", TestContext.Current.CancellationToken);

        Assert.Equal(["search_query: power brick"], generator.Inputs);
        Assert.Equal("search_query: power brick", result.Trace.Details!["embeddedText"]);
    }

    [Fact]
    public async Task EmbedDocumentsAsync_AddsTheSearchDocumentPrefixToEveryDocument()
    {
        var generator = new RecordingGenerator();
        var embedder = new SearchEmbedder(generator, Options.Create(new EmbeddingsOptions()));

        await embedder.EmbedDocumentsAsync(["a charger", "a laptop"], TestContext.Current.CancellationToken);

        Assert.Equal(["search_document: a charger", "search_document: a laptop"], generator.Inputs);
    }

    [Fact]
    public async Task EmbedQueryAsync_ProviderNotBuiltYet_ThrowsUnavailable()
    {
        var embedder = new SearchEmbedder(new RecordingGenerator(), Options.Create(new EmbeddingsOptions { Provider = "openai" }));

        await Assert.ThrowsAsync<EmbeddingModelUnavailableException>(
            () => embedder.EmbedQueryAsync("anything", TestContext.Current.CancellationToken));
    }

    private sealed class RecordingGenerator : IEmbeddingGenerator<string, Embedding<float>>
    {
        public List<string> Inputs { get; } = [];

        public Task<GeneratedEmbeddings<Embedding<float>>> GenerateAsync(
            IEnumerable<string> values, EmbeddingGenerationOptions? options = null, CancellationToken cancellationToken = default)
        {
            var list = values.ToList();
            Inputs.AddRange(list);

            return Task.FromResult(new GeneratedEmbeddings<Embedding<float>>(list.Select(_ => new Embedding<float>(new float[] { 1f, 0f }))));
        }

        public object? GetService(Type serviceType, object? serviceKey = null) =>
            serviceType == typeof(EmbeddingGeneratorMetadata) ? new EmbeddingGeneratorMetadata("test", null, "test-model", 2) : null;

        public void Dispose()
        {
        }
    }
}

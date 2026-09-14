using PI.SearchApi.Pipeline;

namespace PI.SearchApi.Embeddings;

/// <summary>
/// Embeds queries and product documents for search (ADR-0009). Callers never touch the generator
/// directly, so provider details — like Nomic's required task prefixes — can't be forgotten.
/// </summary>
public interface ISearchEmbedder
{
    /// <summary>The configured provider, e.g. "nomic".</summary>
    string Provider { get; }

    /// <summary>The model ID stored beside every vector, e.g. "nomic-embed-text-v1.5-int8".</summary>
    string ModelId { get; }

    /// <summary>Embeds a search query, returning the vector and a trace step describing how.</summary>
    /// <exception cref="EmbeddingModelUnavailableException">The model or provider isn't available (→ 503).</exception>
    Task<QueryEmbedding> EmbedQueryAsync(string query, CancellationToken ct);

    /// <summary>Embeds product documents (the text from <c>ProductDocument.BuildText</c>) in one batch.</summary>
    /// <exception cref="EmbeddingModelUnavailableException">The model or provider isn't available.</exception>
    Task<IReadOnlyList<float[]>> EmbedDocumentsAsync(IReadOnlyList<string> documents, CancellationToken ct);
}

/// <summary>A query vector plus the trace step that shows how it was produced.</summary>
public sealed record QueryEmbedding(float[] Vector, TraceStep Trace);

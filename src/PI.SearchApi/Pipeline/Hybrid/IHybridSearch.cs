using PI.SearchApi.Contracts;
using PI.SearchApi.Pipeline.Keyword;

namespace PI.SearchApi.Pipeline.Hybrid;

/// <summary>
/// Stage 4: keyword and vector search run side by side, fused with Reciprocal Rank Fusion (ADR-0011).
/// Stage 6 re-runs this same pipeline with an ontology-expanded query (ADR-0004).
/// </summary>
public interface IHybridSearch
{
    /// <param name="keywordExpansion">Stage 6's expanded keyword query; null for plain hybrid search.</param>
    /// <param name="embeddingText">Stage 6's expanded embedding text; null embeds the query as typed.</param>
    Task<StageResult> SearchAsync(
        SearchRequest request,
        KeywordExpansion? keywordExpansion,
        string? embeddingText,
        CancellationToken ct);
}

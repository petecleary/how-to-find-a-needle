using PI.SearchApi.Contracts;

namespace PI.SearchApi.Pipeline.Keyword;

/// <summary>
/// Stage 2: Postgres full-text search ranked with ts_rank_cd — "BM25-style", not BM25 (ADR-0008).
/// </summary>
public interface IKeywordSearch
{
    /// <summary>
    /// Returns up to <c>options.candidateDepth</c> candidates in ts_rank_cd order, with the request's
    /// filters applied first.
    /// </summary>
    /// <param name="expansion">Stage 6's ontology expansion; null for plain keyword search.</param>
    Task<StageResult> SearchAsync(SearchRequest request, KeywordExpansion? expansion, CancellationToken ct);
}

using PI.SearchApi.Contracts;

namespace PI.SearchApi.Pipeline.Structured;

/// <summary>
/// Stage 1: exact filtering with parameterised SQL (ADR-0007). Reads only <c>filters</c> and paging;
/// the query text is ignored, because structured search has no notion of relevance.
/// </summary>
public interface IStructuredSearch
{
    /// <summary>Returns one page of matching products (ordered by price, then ID) and a real total count.</summary>
    Task<StageResult> SearchAsync(SearchRequest request, CancellationToken ct);
}

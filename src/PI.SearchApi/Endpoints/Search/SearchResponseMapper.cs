using PI.SearchApi.Contracts;
using PI.SearchApi.Pipeline;

namespace PI.SearchApi.Endpoints.Search;

/// <summary>
/// Maps a stage's candidates to the shared <see cref="SearchResponse"/>, paging exactly once.
/// "Retrieve deep, page late" (ADR-0004): services rank <c>candidateDepth</c> items, and only here
/// is that list cut down to the requested page.
/// </summary>
public static class SearchResponseMapper
{
    /// <param name="alreadyPaged">
    /// True for Stage 1, which pages in SQL because it isn't bounded by candidate depth (ADR-0004).
    /// </param>
    public static SearchResponse ToResponse(
        string stage,
        SearchRequest request,
        StageResult result,
        double executionTimeMs,
        bool alreadyPaged = false)
    {
        var page = alreadyPaged
            ? result.Candidates
            : result.Candidates.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToList();

        return new SearchResponse
        {
            Stage = stage,
            Query = request.Query,
            Page = request.Page,
            PageSize = request.PageSize,
            TotalResults = result.TotalResults ?? result.Candidates.Count,
            ExecutionTimeMs = Math.Round(executionTimeMs, 2),
            Results = [.. page.Select(ToProductResult)],
            DebugTrace = new DebugTrace(result.Trace),
        };
    }

    private static ProductResult ToProductResult(Candidate candidate) => new()
    {
        Id = candidate.Product.Id,
        Name = candidate.Product.Name,
        Brand = candidate.Product.Brand,
        Categories = candidate.Product.Categories,
        Price = candidate.Product.Price,
        Specs = candidate.Product.Specs,
        Score = candidate.Score,
        Signals = candidate.Signals,
        Compatibility = candidate.Compatibility,
    };
}

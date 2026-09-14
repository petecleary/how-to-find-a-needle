namespace PI.SearchApi.Contracts;

/// <summary>
/// The one response every search stage returns (ADR-0003), so the UI can switch stages without
/// special cases. It never contains LLM text; Stages 6–7 stream that from their answer endpoints.
/// </summary>
public sealed record SearchResponse
{
    /// <summary>The stage slug: structured, keyword, vector, hybrid, ontology…</summary>
    public required string Stage { get; init; }

    public required string Query { get; init; }

    public required int Page { get; init; }

    public required int PageSize { get; init; }

    /// <summary>
    /// Stage 1: a real COUNT(*) over the filters. Ranked stages: the number of candidates
    /// retrieved, which is bounded by candidateDepth — not a count of the whole catalog.
    /// </summary>
    public required int TotalResults { get; init; }

    /// <summary>Total time spent in the endpoint, in milliseconds.</summary>
    public required double ExecutionTimeMs { get; init; }

    public required IReadOnlyList<ProductResult> Results { get; init; }

    /// <summary>Every pipeline step that produced these results, in order (ADR-0003).</summary>
    public required DebugTrace DebugTrace { get; init; }
}

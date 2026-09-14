namespace PI.SearchApi.Contracts;

/// <summary>
/// The one request every search stage accepts (ADR-0003). Keeping it identical across stages
/// is what makes the talk's experiment fair: same input, different technique.
/// </summary>
public sealed record SearchRequest
{
    /// <summary>Free-text query. Required for Stages 2–7; Stage 1 ignores it and says so in its trace.</summary>
    public string Query { get; init; } = "";

    /// <summary>1-based page number. Paging happens once, in the endpoint, after ranking.</summary>
    public int Page { get; init; } = 1;

    /// <summary>Results per page, 1–50.</summary>
    public int PageSize { get; init; } = 10;

    /// <summary>Hard pre-filters, applied the same way in every stage (ADR-0007).</summary>
    public SearchFilters Filters { get; init; } = new();

    /// <summary>What the shopper already owns; used by Stage 5's compatibility rules.</summary>
    public SearchContext Context { get; init; } = new();

    /// <summary>Stage-specific tuning. Stages ignore options that don't apply to them.</summary>
    public SearchOptions Options { get; init; } = new();
}

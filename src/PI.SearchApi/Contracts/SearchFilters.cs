using System.Text.Json;

namespace PI.SearchApi.Contracts;

/// <summary>
/// Structured pre-filters (ADR-0007). They are the only input to Stage 1, and every later stage
/// applies them first, so a filter means exactly the same thing whichever stage you're on.
/// </summary>
public sealed record SearchFilters
{
    /// <summary>Exact brand, case-insensitive.</summary>
    public string? Brand { get; init; }

    /// <summary>
    /// Taxonomy notations (e.g. "chargers"). A product matches if it is in any of them or in a
    /// narrower concept, so "chargers" also matches "laptop-chargers".
    /// </summary>
    public IReadOnlyList<string>? Categories { get; init; }

    /// <summary>Minimum price in GBP, inclusive.</summary>
    public decimal? MinPrice { get; init; }

    /// <summary>Maximum price in GBP, inclusive.</summary>
    public decimal? MaxPrice { get; init; }

    /// <summary>
    /// Spec key/value pairs matched by JSONB containment, e.g. <c>{ "voltageV": 18 }</c>.
    /// Numbers stay numbers, so 18 matches 18 but not "18".
    /// </summary>
    public IReadOnlyDictionary<string, JsonElement>? Specs { get; init; }
}

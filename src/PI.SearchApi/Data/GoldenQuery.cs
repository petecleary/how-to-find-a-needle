using System.Text.Json;
using System.Text.Json.Serialization;

namespace PI.SearchApi.Data;

/// <summary>
/// One golden query (ADR-0005): a talk moment, the request that demonstrates it, and the
/// expected outcome per stage. Golden queries are integration test cases, UI presets and
/// talk-mode stage steps, from the one file.
/// </summary>
public sealed record GoldenQuery(
    string Id,
    string Title,
    string Moment,
    GoldenQueryRequest Request,
    IReadOnlyDictionary<string, IReadOnlyList<GoldenQueryExpectation>> Expectations);

/// <summary>
/// The request shape a golden query drives (a subset of the full <c>SearchRequest</c> —
/// Phase 2 introduces the request contract itself; this is the JSON as authored).
/// </summary>
public sealed record GoldenQueryRequest(
    string Query,
    GoldenQueryFilters? Filters,
    GoldenQueryContext? Context);

public sealed record GoldenQueryFilters(
    string? Brand,
    [property: JsonPropertyName("maxPrice")] decimal? MaxPrice,
    IReadOnlyDictionary<string, JsonElement>? Specs);

public sealed record GoldenQueryContext(string? TargetProductId);

/// <summary>
/// One expected outcome for one product on one stage. Only the fields relevant to the
/// expectation are set — loose bounds, never exact scores or positions (ADR-0005).
/// </summary>
public sealed record GoldenQueryExpectation(
    string ProductId,
    RankBound? Rank,
    int? AbsentFromTop,
    bool? Present,
    string? Compatibility,
    string? ConceptMatch,
    RankBound? KeywordRank);

public sealed record RankBound(int? Min, int? Max);

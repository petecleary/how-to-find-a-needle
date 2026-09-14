using System.Text.Json;
using System.Text.Json.Nodes;
using PI.SearchApi.IntegrationTests.SearchApi;

namespace PI.SearchApi.IntegrationTests.GoldenQueries;

/// <summary>
/// One golden query from <c>golden-queries.json</c> (ADR-0005): a talk moment, the request that
/// demonstrates it, and loose per-stage expectations — ranks within a bound, never exact scores.
/// </summary>
public sealed record GoldenQueryCase(
    string Id,
    string Title,
    string Moment,
    JsonObject Request,
    IReadOnlyDictionary<string, IReadOnlyList<GoldenExpectation>> Expectations)
{
    public static GoldenQueryCase Load(string id)
    {
        var json = File.ReadAllText(RepositoryPaths.GoldenQueriesFile);
        var array = JsonNode.Parse(json)!.AsArray();
        var node = array.Single(n => n!["id"]!.GetValue<string>() == id)!.AsObject();

        var expectations = node["expectations"]!.Deserialize<Dictionary<string, List<GoldenExpectation>>>(SearchApiClient.JsonOptions)!;

        return new GoldenQueryCase(
            id,
            node["title"]!.GetValue<string>(),
            node["moment"]!.GetValue<string>(),
            node["request"]!.DeepClone().AsObject(),
            expectations.ToDictionary(kv => kv.Key, kv => (IReadOnlyList<GoldenExpectation>)kv.Value));
    }
}

/// <summary>One expected outcome for one product on one stage; only the relevant fields are set.</summary>
public sealed record GoldenExpectation(
    string ProductId,
    RankBound? Rank,
    int? AbsentFromTop,
    bool? Present,
    string? Compatibility,
    string? ConceptMatch,
    RankBound? KeywordRank);

public sealed record RankBound(int? Min, int? Max);

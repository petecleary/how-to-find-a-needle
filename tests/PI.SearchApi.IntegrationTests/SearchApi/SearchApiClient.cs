using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace PI.SearchApi.IntegrationTests.SearchApi;

/// <summary>
/// Posts search requests as raw JSON and reads the response into small wire-shaped records. The tests
/// deliberately don't reference the API's C# contracts: they check what actually travels over HTTP.
/// </summary>
public static class SearchApiClient
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// Posts to <c>/api/search/{stage}</c>. The page size is raised to 50 so a golden query's rank
    /// bounds are checked against the whole retrieved list, not just the default first page of 10.
    /// </summary>
    public static async Task<SearchResponseDto> SearchAsync(HttpClient client, string stage, JsonObject request, CancellationToken ct)
    {
        var body = request.DeepClone().AsObject();
        body["pageSize"] ??= 50;

        using var response = await client.PostAsJsonAsync($"/api/search/{stage}", body, ct);
        var text = await response.Content.ReadAsStringAsync(ct);

        Assert.True(response.IsSuccessStatusCode, $"POST /api/search/{stage} returned {(int)response.StatusCode}: {text}");

        return JsonSerializer.Deserialize<SearchResponseDto>(text, JsonOptions)
            ?? throw new InvalidOperationException("The search response was empty.");
    }
}

public sealed record SearchResponseDto(
    string Stage,
    string Query,
    int Page,
    int PageSize,
    int TotalResults,
    double ExecutionTimeMs,
    IReadOnlyList<ProductResultDto> Results,
    DebugTraceDto DebugTrace);

public sealed record ProductResultDto(
    string Id,
    string Name,
    IReadOnlyList<string> Categories,
    double? Score,
    SignalsDto Signals,
    CompatibilityDto Compatibility);

public sealed record SignalsDto(
    bool? StructuredMatch,
    int? KeywordRank,
    double? KeywordScore,
    int? VectorRank,
    double? VectorDistance,
    int? FusedRank,
    string? ConceptMatch);

public sealed record CompatibilityDto(string Status, IReadOnlyList<string> Reasons);

public sealed record DebugTraceDto(IReadOnlyList<TraceStepDto> Steps);

public sealed record TraceStepDto(
    string Stage,
    string Title,
    double DurationMs,
    string? Sql,
    Dictionary<string, JsonElement>? Parameters,
    Dictionary<string, JsonElement>? Details,
    IReadOnlyList<string> Notes);

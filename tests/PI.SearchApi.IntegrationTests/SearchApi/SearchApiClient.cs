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
    /// Posts to <c>/api/search/{stage}</c> and reads every page, so a golden query's rank bounds are checked
    /// against the whole retrieved list, not just the first page. Hybrid fusion returns the union of the keyword
    /// and vector lists, so a stage can return more results than <c>candidateDepth</c>; Stage 5 sorts flagged
    /// items last, and at catalog scale they land beyond a page of 50. The trace comes from the first page.
    /// </summary>
    public static async Task<SearchResponseDto> SearchAsync(HttpClient client, string stage, JsonObject request, CancellationToken ct)
    {
        var body = request.DeepClone().AsObject();
        body["pageSize"] ??= 50;

        var first = await PostAsync(client, stage, body, ct);
        var results = new List<ProductResultDto>(first.Results);

        // A request that sets its own page asks for that page only.
        if (request.ContainsKey("page"))
        {
            return first;
        }

        for (var page = 2; results.Count < first.TotalResults; page++)
        {
            body["page"] = page;
            var next = await PostAsync(client, stage, body, ct);

            if (next.Results.Count == 0)
            {
                break;
            }

            results.AddRange(next.Results);
        }

        return first with { Results = results };
    }

    private static async Task<SearchResponseDto> PostAsync(HttpClient client, string stage, JsonObject body, CancellationToken ct)
    {
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

public sealed record CompatibilityDto(string Status, IReadOnlyList<string> Reasons, string? Source = null, IReadOnlyList<DeviceFitDto>? Fits = null);

public sealed record DeviceFitDto(string DeviceType, string DeviceTypeLabel, int Total, IReadOnlyList<FittingDeviceDto> Devices);

public sealed record FittingDeviceDto(string Id, string Name);

public sealed record DebugTraceDto(IReadOnlyList<TraceStepDto> Steps);

public sealed record TraceStepDto(
    string Stage,
    string Title,
    double DurationMs,
    string? Sql,
    Dictionary<string, JsonElement>? Parameters,
    Dictionary<string, JsonElement>? Details,
    IReadOnlyList<string> Notes);

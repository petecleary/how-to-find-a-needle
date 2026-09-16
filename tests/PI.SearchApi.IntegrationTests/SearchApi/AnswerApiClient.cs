using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace PI.SearchApi.IntegrationTests.SearchApi;

/// <summary>
/// Calls the Stage 6–7 answer endpoints. When the API says the LLM is unavailable (503), the test skips with the API's
/// own fix-it guidance, because an LLM that isn't running is a setup state, not a failing feature (tests CLAUDE.md).
/// </summary>
public static class AnswerApiClient
{
    /// <summary>Posts to <c>/api/search/{stage}/answer</c> with <c>Accept: application/json</c> and waits for the whole answer.</summary>
    public static async Task<AnswerResponseDto> AnswerAsync(HttpClient client, string stage, JsonObject request, CancellationToken ct)
    {
        using var message = Request(stage, request, "application/json");
        using var response = await client.SendAsync(message, ct);
        var text = await response.Content.ReadAsStringAsync(ct);

        SkipIfLlmUnavailable(response.StatusCode, text);
        Assert.True(response.IsSuccessStatusCode, $"POST /api/search/{stage}/answer returned {(int)response.StatusCode}: {text}");

        return JsonSerializer.Deserialize<AnswerResponseDto>(text, SearchApiClient.JsonOptions)
            ?? throw new InvalidOperationException("The answer response was empty.");
    }

    /// <summary>Posts with <c>Accept: text/event-stream</c> and returns every event's name and data, in order.</summary>
    public static async Task<IReadOnlyList<(string Name, string Data)>> ReadEventStreamAsync(HttpClient client, string stage, JsonObject request, CancellationToken ct)
    {
        using var message = Request(stage, request, "text/event-stream");
        using var response = await client.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, ct);

        if (!response.IsSuccessStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync(ct);
            SkipIfLlmUnavailable(response.StatusCode, body);
            Assert.Fail($"POST /api/search/{stage}/answer returned {(int)response.StatusCode}: {body}");
        }

        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);

        var events = new List<(string Name, string Data)>();
        string? name = null;
        var data = new StringBuilder();

        using var reader = new StreamReader(await response.Content.ReadAsStreamAsync(ct));
        while (await reader.ReadLineAsync(ct) is { } line)
        {
            if (line.StartsWith("event: ", StringComparison.Ordinal))
            {
                name = line[7..];
            }
            else if (line.StartsWith("data: ", StringComparison.Ordinal))
            {
                data.Append(line[6..]);
            }
            else if (line.Length == 0 && name is not null)
            {
                events.Add((name, data.ToString()));
                name = null;
                data.Clear();
            }
        }

        // `meta` is sent as soon as retrieval finishes, before the model is called, so an unavailable LLM arrives as an
        // error event on a 200 stream rather than a 503 (ADR-0016). Either way the test skips instead of failing.
        var error = events.FirstOrDefault(e => e.Name == "error").Data;
        SkipIfLlmUnavailable(HttpStatusCode.ServiceUnavailable, error ?? "");

        return events;
    }

    private static HttpRequestMessage Request(string stage, JsonObject request, string accept)
    {
        var body = request.DeepClone().AsObject();
        var message = new HttpRequestMessage(HttpMethod.Post, $"/api/search/{stage}/answer") { Content = JsonContent.Create(body) };
        message.Headers.Accept.ParseAdd(accept);
        return message;
    }

    private static void SkipIfLlmUnavailable(HttpStatusCode status, string body) =>
        Assert.SkipWhen(
            status == HttpStatusCode.ServiceUnavailable && body.Contains("LLM unavailable", StringComparison.Ordinal),
            $"The LLM isn't available, so the answer can't be generated: {body}");
}

public sealed record AnswerResponseDto(
    string Stage,
    string Provider,
    string Model,
    IReadOnlyList<string> Evidence,
    IReadOnlyList<AnswerFinalDto> Sections,
    double? TimeToFirstTokenMs,
    double TotalMs,
    IReadOnlyList<TraceStepDto> Trace);

public sealed record AnswerFinalDto(
    string Section,
    string Markdown,
    IReadOnlyList<string> Citations,
    IReadOnlyList<string> InvalidCitations,
    bool InsufficientEvidence,
    IReadOnlyList<string> Warnings,
    ExplanationStructureDto? Structure);

public sealed record ExplanationStructureDto(
    ExplanationProductDto Decision,
    IReadOnlyList<string> Concepts,
    ExplanationProductDto NearMiss,
    string? RuleOfThumb,
    string? NextStep);

public sealed record ExplanationProductDto(string? ProductId);

using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http.Features;

namespace PI.SearchApi.Endpoints.Search;

/// <summary>
/// Writes Server-Sent Events (ADR-0003, ADR-0016): <c>event: name</c>, one <c>data:</c> line per line of JSON, then a
/// blank line, flushed immediately so each chunk reaches the browser as the model produces it. SSE is plain
/// HTTP: a one-way stream of text, which is all an answer needs, with none of a WebSocket's two-way machinery.
/// </summary>
public static class ServerSentEventWriter
{
    public const string ContentType = "text/event-stream";

    /// <summary>Formats one event. A payload containing newlines gets one <c>data:</c> line per line, as the SSE format requires.</summary>
    public static string Format(string eventName, string json)
    {
        var text = new StringBuilder().Append("event: ").Append(eventName).Append('\n');

        foreach (var line in json.ReplaceLineEndings("\n").Split('\n'))
        {
            text.Append("data: ").Append(line).Append('\n');
        }

        return text.Append('\n').ToString();
    }

    /// <summary>Sends the headers and turns off response buffering, so nothing waits for the stream to end.</summary>
    public static async Task StartAsync(HttpResponse response, CancellationToken ct)
    {
        response.StatusCode = StatusCodes.Status200OK;
        response.ContentType = ContentType;
        response.Headers.CacheControl = "no-cache";
        // Proxies such as nginx buffer responses unless told not to; harmless where there is no proxy.
        response.Headers["X-Accel-Buffering"] = "no";
        response.HttpContext.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();

        await response.Body.FlushAsync(ct);
    }

    public static async Task WriteAsync(HttpResponse response, string eventName, object data, JsonSerializerOptions jsonOptions, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(data, data.GetType(), jsonOptions);

        await response.WriteAsync(Format(eventName, json), ct);
        await response.Body.FlushAsync(ct);
    }
}

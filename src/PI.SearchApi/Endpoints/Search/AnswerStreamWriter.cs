using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using PI.SearchApi.Contracts;
using PI.SearchApi.Llm;
using PI.SearchApi.Pipeline;
using PI.SearchApi.Pipeline.Rag;

namespace PI.SearchApi.Endpoints.Search;

/// <summary>
/// Sends an answer's events to the client (ADR-0003, ADR-0016), shared by the Stage 6 and Stage 7 answer endpoints:
/// as Server-Sent Events by default, or gathered into one <see cref="AnswerResponse"/> for <c>Accept: application/json</c>.
/// </summary>
public static class AnswerStreamWriter
{
    /// <summary>JSON only when the client asks for it and doesn't also accept an event stream.</summary>
    public static bool WantsJson(HttpRequest request)
    {
        var accept = request.Headers.Accept.ToString();

        return accept.Contains("application/json", StringComparison.OrdinalIgnoreCase)
            && !accept.Contains(ServerSentEventWriter.ContentType, StringComparison.OrdinalIgnoreCase);
    }

    public static Task SendAsync(
        HttpContext httpContext,
        string stage,
        IAsyncEnumerable<AnswerEvent> events,
        JsonSerializerOptions jsonOptions,
        ILogger logger,
        CancellationToken ct) =>
        WantsJson(httpContext.Request)
            ? SendJsonAsync(httpContext, stage, events, jsonOptions, ct)
            : SendEventStreamAsync(httpContext, events, jsonOptions, logger, ct);

    // Waits for the whole answer. An unavailable LLM propagates to the exception handler as an ordinary 503.
    private static async Task SendJsonAsync(
        HttpContext httpContext,
        string stage,
        IAsyncEnumerable<AnswerEvent> events,
        JsonSerializerOptions jsonOptions,
        CancellationToken ct)
    {
        AnswerMeta? meta = null;
        AnswerDone? done = null;
        var sections = new List<AnswerFinal>();

        await foreach (var answerEvent in events.WithCancellation(ct))
        {
            switch (answerEvent.Data)
            {
                case AnswerMeta m: meta = m; break;
                case AnswerFinal f: sections.Add(f); break;
                case AnswerDone d: done = d; break;
            }
        }

        var response = new AnswerResponse
        {
            Stage = stage,
            Provider = meta?.Provider ?? "",
            Model = meta?.Model ?? "",
            Evidence = meta?.Evidence ?? [],
            Sections = sections,
            TimeToFirstTokenMs = done?.TimeToFirstTokenMs,
            TotalMs = done?.TotalMs ?? 0,
            Trace = done?.Trace ?? [],
        };

        await httpContext.Response.WriteAsJsonAsync(response, jsonOptions, ct);
    }

    private static async Task SendEventStreamAsync(
        HttpContext httpContext,
        IAsyncEnumerable<AnswerEvent> events,
        JsonSerializerOptions jsonOptions,
        ILogger logger,
        CancellationToken ct)
    {
        var response = httpContext.Response;
        var hasStarted = false;
        await using var enumerator = events.GetAsyncEnumerator(ct);

        while (true)
        {
            AnswerEvent answerEvent;

            try
            {
                if (!await enumerator.MoveNextAsync())
                {
                    break;
                }

                answerEvent = enumerator.Current;
            }
            // Before the first event nothing has been sent, so a failure is an ordinary ProblemDetails response
            // (the exception handler's 503). Once streaming, the status line is gone: report it as an error event.
            catch (Exception exception) when (hasStarted && !ct.IsCancellationRequested)
            {
                await ServerSentEventWriter.WriteAsync(response, AnswerEvent.ErrorName, ProblemFor(exception, logger), jsonOptions, ct);
                return;
            }

            if (!hasStarted)
            {
                await ServerSentEventWriter.StartAsync(response, ct);
                hasStarted = true;
            }

            await ServerSentEventWriter.WriteAsync(response, answerEvent.Name, answerEvent.Data, jsonOptions, ct);
        }
    }

    private static ProblemDetails ProblemFor(Exception exception, ILogger logger)
    {
        if (exception is LlmUnavailableException)
        {
            logger.LogWarning("Answer stream stopped, LLM unavailable: {Reason}", exception.Message);

            return new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = "LLM unavailable",
                Detail = exception.Message,
            };
        }

        logger.LogError(exception, "Answer stream failed");

        return new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "Answer generation failed",
            Detail = "The answer stopped because of an unexpected error. The API log has the details.",
        };
    }
}

using System.ClientModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.AI;

namespace PI.SearchApi.Llm;

/// <summary>
/// Streams one LLM call as text chunks, and records what happened in an <see cref="LlmGeneration"/> (ADR-0015).
/// Both AI stages use it, so time to first token is measured the same way everywhere, and a provider that is down
/// or times out always becomes an <see cref="LlmUnavailableException"/> with fix-it guidance (→ 503).
/// </summary>
public static class LlmStreaming
{
    public static async IAsyncEnumerable<string> StreamTextAsync(
        IChatClient chatClient,
        IList<ChatMessage> messages,
        ChatOptions chatOptions,
        LlmOptions options,
        LlmGeneration generation,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var start = Stopwatch.GetTimestamp();
        var text = new StringBuilder();
        var updates = chatClient.GetStreamingResponseAsync(messages, chatOptions, ct).GetAsyncEnumerator(ct);

        try
        {
            while (true)
            {
                ChatResponseUpdate update;

                // A yield can't sit inside a catch block, so only the read from the provider is wrapped.
                try
                {
                    if (!await updates.MoveNextAsync())
                    {
                        break;
                    }

                    update = updates.Current;
                }
                catch (Exception exception) when (IsProviderFailure(exception, ct))
                {
                    throw exception is TaskCanceledException or TimeoutException
                        ? LlmUnavailableException.TimedOut(options, exception)
                        : LlmUnavailableException.Unreachable(options, exception);
                }

                foreach (var usage in update.Contents.OfType<UsageContent>())
                {
                    generation.InputTokens = usage.Details.InputTokenCount ?? generation.InputTokens;
                    generation.OutputTokens = usage.Details.OutputTokenCount ?? generation.OutputTokens;
                }

                generation.FinishReason = update.FinishReason?.Value ?? generation.FinishReason;

                if (string.IsNullOrEmpty(update.Text))
                {
                    continue;
                }

                // Time to first token: the moment the audience sees the answer start, not when it finishes.
                generation.TimeToFirstTokenMs ??= Math.Round(Stopwatch.GetElapsedTime(start).TotalMilliseconds, 2);
                text.Append(update.Text);

                yield return update.Text;
            }
        }
        finally
        {
            await updates.DisposeAsync();
            generation.Text = text.ToString();
            generation.TotalMs = Math.Round(Stopwatch.GetElapsedTime(start).TotalMilliseconds, 2);
        }
    }

    // Network failures, HTTP error statuses from either SDK, and the client's own timeout. A cancellation the
    // caller asked for (the user switched stage) is not a failure: it propagates as OperationCanceledException.
    private static bool IsProviderFailure(Exception exception, CancellationToken ct) =>
        !ct.IsCancellationRequested
        && exception is not LlmUnavailableException
        && (exception is HttpRequestException or ClientResultException or TaskCanceledException or TimeoutException
            || exception.GetType().Namespace?.StartsWith("Anthropic", StringComparison.Ordinal) == true);
}

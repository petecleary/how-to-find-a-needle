using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace PI.SearchApi.Llm;

/// <summary>
/// Development only, Ollama only: sends one tiny request in the background at startup, so the model is loaded into
/// memory before the first demo question and that question isn't the slow "cold" one (ADR-0015). It never blocks
/// startup and never fails it: an unavailable LLM is logged, and Stages 6–7 report it as a 503 when asked.
/// </summary>
public sealed class LlmWarmUpService(
    IServiceProvider services,
    IOptions<LlmOptions> options,
    ILogger<LlmWarmUpService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var settings = options.Value;
        if (settings.Provider != LlmProviders.Ollama)
        {
            return;
        }

        // Yield first, so the host carries on starting while the model loads.
        await Task.Yield();
        var start = System.Diagnostics.Stopwatch.GetTimestamp();

        try
        {
            var chatClient = services.GetRequiredService<IChatClient>();
            var chatOptions = LlmChatOptions.For(settings);
            chatOptions.MaxOutputTokens = 1;

            await chatClient.GetResponseAsync("Reply with OK.", chatOptions, stoppingToken);

            logger.LogInformation(
                "Warmed up {Provider} model {Model} in {ElapsedMs:F0} ms",
                settings.Provider,
                settings.Model,
                System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalMilliseconds);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning("LLM warm-up skipped: {Reason}", exception.Message);
        }
    }
}

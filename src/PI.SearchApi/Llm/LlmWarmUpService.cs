using Microsoft.Extensions.AI;

namespace PI.SearchApi.Llm;

/// <summary>
/// Development only, Ollama only: sends one tiny request to the default model in the background at startup, so it is
/// loaded into memory before the first demo question and that question isn't the slow "cold" one (ADR-0015). It never blocks
/// startup and never fails it: an unavailable LLM is logged, and Stages 6–7 report it as a 503 when asked.
/// </summary>
public sealed class LlmWarmUpService(
    LlmModelRegistry models,
    ILogger<LlmWarmUpService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Only the default model: a learner's other choices are warmed by their first question.
        if (models.DefaultModel.Provider != LlmProviders.Ollama)
        {
            return;
        }

        // Yield first, so the host carries on starting while the model loads.
        await Task.Yield();
        var start = System.Diagnostics.Stopwatch.GetTimestamp();

        try
        {
            var llm = models.Resolve(model: null);
            var chatOptions = llm.ChatOptions.Clone();
            chatOptions.MaxOutputTokens = 1;

            await llm.Client.GetResponseAsync("Reply with OK.", chatOptions, stoppingToken);

            logger.LogInformation(
                "Warmed up {Provider} model {Model} in {ElapsedMs:F0} ms",
                llm.Options.Provider,
                llm.Options.Model,
                System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalMilliseconds);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning("LLM warm-up skipped: {Reason}", exception.Message);
        }
    }
}

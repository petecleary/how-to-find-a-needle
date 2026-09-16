using Microsoft.Extensions.AI;

namespace PI.SearchApi.Llm;

/// <summary>
/// The per-call settings for each provider (ADR-0015). Providers disagree about sampling and reasoning, and
/// this is where those differences live, next to <see cref="LlmClientFactory"/>, so the stages never see them.
/// </summary>
public static class LlmChatOptions
{
    /// <summary>Low temperature: predictable wording from run to run, which a rehearsed demo needs.</summary>
    public const float OpenAICompatibleTemperature = 0.1f;

    public static ChatOptions For(LlmOptions options)
    {
        var chatOptions = new ChatOptions { MaxOutputTokens = options.MaxOutputTokens };

        switch (options.Provider)
        {
            case LlmProviders.Ollama:
                chatOptions.Temperature = OpenAICompatibleTemperature;
                // Qwen 3.6 and Gemma 4 "think" before answering by default. Through Ollama's /v1 API that reasoning
                // arrives before any answer text: a 60-token test spent every token reasoning and wrote no answer.
                // Effort None is sent as reasoning_effort: "none", which turns it off (first token in under 0.4 s).
                // Ollama's own "think": false field is ignored on /v1.
                chatOptions.Reasoning = new ReasoningOptions { Effort = ReasoningEffort.None };
                break;

            case LlmProviders.OpenAI:
                // Tested late (roadmap Phase 5): reasoning settings depend on the model the learner picks.
                chatOptions.Temperature = OpenAICompatibleTemperature;
                break;

            case LlmProviders.Anthropic:
                // Current Claude models reject sampling parameters, so no temperature. Depth is traded for latency
                // with effort instead: low keeps adaptive thinking brief for a short, grounded answer.
                chatOptions.Reasoning = new ReasoningOptions { Effort = ReasoningEffort.Low };
                break;
        }

        return chatOptions;
    }
}

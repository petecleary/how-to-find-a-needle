namespace PI.SearchApi.Llm;

/// <summary>The <c>Llm:Provider</c> values the API understands (ADR-0015).</summary>
public static class LlmProviders
{
    /// <summary>A local Ollama server, reached through its OpenAI-compatible API. The presenter's default: no key, works offline.</summary>
    public const string Ollama = "ollama";

    /// <summary>The OpenAI API, with the learner's own key.</summary>
    public const string OpenAI = "openai";

    /// <summary>The Claude API through Anthropic's official SDK, with the learner's own key.</summary>
    public const string Anthropic = "anthropic";

    public static readonly IReadOnlyList<string> All = [Ollama, OpenAI, Anthropic];
}

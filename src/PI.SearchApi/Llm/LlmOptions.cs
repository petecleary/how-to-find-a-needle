namespace PI.SearchApi.Llm;

/// <summary>
/// The <c>Llm</c> configuration section (ADR-0015). Non-secret defaults live in <c>appsettings.json</c>;
/// <see cref="ApiKey"/> comes from <c>dotnet user-secrets</c>. The provider and model are fixed for a run:
/// one <c>IChatClient</c> is built from these values, so Stages 6–7 never know which provider answers.
/// </summary>
public sealed class LlmOptions
{
    public const string SectionName = "Llm";

    /// <summary><c>ollama</c>, <c>openai</c> or <c>anthropic</c> (see <see cref="LlmProviders"/>).</summary>
    public string Provider { get; set; } = LlmProviders.Ollama;

    /// <summary>The provider's model ID, e.g. <c>qwen3.6:35b</c> or <c>claude-sonnet-5</c>.</summary>
    public string Model { get; set; } = "";

    /// <summary>Ollama only: the local server, e.g. <c>http://localhost:11434</c>. Its OpenAI-compatible API is under <c>/v1</c>.</summary>
    public string? Endpoint { get; set; }

    /// <summary>OpenAI and Anthropic only. Set with <c>dotnet user-secrets</c>; never committed, logged or traced.</summary>
    public string? ApiKey { get; set; }

    /// <summary>A cap sized for a short summary or explanation, so a rambling model can't hold the demo up.</summary>
    public int MaxOutputTokens { get; set; } = 1500;

    /// <summary>How long one call may take before it fails visibly. There are no retries (ADR-0015).</summary>
    public int TimeoutSeconds { get; set; } = 60;
}

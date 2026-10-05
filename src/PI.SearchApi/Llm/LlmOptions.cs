namespace PI.SearchApi.Llm;

/// <summary>
/// The <c>Llm</c> configuration section (ADR-0015): the <b>default</b> model, used when a request doesn't name one
/// in <c>options.model</c> (ADR-0019). Non-secret values live in <c>appsettings.json</c>; keys come from
/// <c>dotnet user-secrets</c>. <see cref="LlmModelRegistry"/> also builds one of these per request, with the
/// requested provider's address and key, so Stages 6–7 never know which provider answers.
/// </summary>
public sealed class LlmOptions
{
    public const string SectionName = "Llm";

    /// <summary>A provider ID from <see cref="LlmProviders"/>: <c>ollama</c>, <c>openai</c>, <c>anthropic</c>, <c>azure</c>, <c>google</c> or <c>compat</c>.</summary>
    public string Provider { get; set; } = LlmProviders.Ollama;

    /// <summary>The provider's model ID, e.g. <c>qwen3.6:35b</c> or <c>claude-sonnet-5</c>.</summary>
    public string Model { get; set; } = "";

    /// <summary>
    /// The provider's base URL. In configuration it is Ollama's address, e.g. <c>http://localhost:11434</c> (its
    /// OpenAI-compatible API is under <c>/v1</c>); per request the registry sets it for whichever provider answers.
    /// </summary>
    public string? Endpoint { get; set; }

    /// <summary>
    /// The default provider's key, kept for ADR-0015 setups. Prefer the provider's own variable (e.g.
    /// <c>ANTHROPIC_API_KEY</c>) in <c>dotnet user-secrets</c> (ADR-0019). Never committed, logged or traced.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>A cap sized for a short summary or explanation, so a rambling model can't hold the demo up.</summary>
    public int MaxOutputTokens { get; set; } = 1500;

    /// <summary>How long one call may take before it fails visibly. There are no retries (ADR-0015).</summary>
    public int TimeoutSeconds { get; set; } = 60;
}

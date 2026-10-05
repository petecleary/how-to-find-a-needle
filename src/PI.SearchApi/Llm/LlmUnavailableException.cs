namespace PI.SearchApi.Llm;

/// <summary>
/// The LLM can't be used right now: Ollama isn't running, the model isn't pulled, or a hosted provider has no
/// key. This is an expected "unavailable" state, not a bug, so it maps to <c>503 Service Unavailable</c> with the
/// fix in the message (ADR-0003, ADR-0015). Stages 1–5, and Stages 6–7's results, keep working.
/// </summary>
public sealed class LlmUnavailableException(string message, Exception? innerException = null)
    : Exception(message, innerException)
{
    /// <summary>A setting is missing or wrong, e.g. no API key for a hosted provider.</summary>
    public static LlmUnavailableException Misconfigured(LlmOptions options, string problem) =>
        new($"{problem} {HowToConfigure(options)}");

    /// <summary>The provider was configured but couldn't be reached, or rejected the call.</summary>
    public static LlmUnavailableException Unreachable(LlmOptions options, Exception innerException) =>
        new($"{UnreachableGuidance(options)} ({innerException.Message})", innerException);

    /// <summary>The call took longer than Llm:TimeoutSeconds. There are no retries, so it fails visibly.</summary>
    public static LlmUnavailableException TimedOut(LlmOptions options, Exception innerException) =>
        new($"The {options.Provider} model '{options.Model}' didn't finish within {options.TimeoutSeconds} s (Llm:TimeoutSeconds). "
            + (options.Provider == LlmProviders.Ollama ? "A large model on first use can be slow to load: try again, or choose a smaller model." : "Try again, or choose a faster model."),
            innerException);

    // Each provider's fix, naming both routes: the UI's panel for this session, or configuration for every run (ADR-0019).
    private static string HowToConfigure(LlmOptions options) => LlmProviders.Find(options.Provider) switch
    {
        null => $"Choose one of: {string.Join(", ", LlmProviders.All)}.",
        { Id: LlmProviders.Ollama } => "Set Ollama's base URL in Models and API keys (or Llm:Endpoint), and choose a model you have pulled.",
        { KeyVariable: { } variable, Name: var name } =>
            $"Add your {name} key in Models and API keys (kept for this session), or set it for every run with: dotnet user-secrets set \"{variable}\" \"<your key>\" --project src/PI.SearchApi",
        { Name: var name } => $"Check {name} in Models and API keys.",
    };

    private static string UnreachableGuidance(LlmOptions options) => options.Provider switch
    {
        LlmProviders.Ollama =>
            $"Is Ollama running at {options.Endpoint}? Start it with `ollama serve`, and make sure the model is pulled: `ollama pull {options.Model}`.",
        LlmProviders.Compatible =>
            $"Is the OpenAI-compatible server running at {options.Endpoint}, and does it serve '{options.Model}'?",
        _ => $"The {options.Provider} API couldn't be used with model '{options.Model}'. Check the model name and that the key is valid (Test connection in Models and API keys).",
    };
}

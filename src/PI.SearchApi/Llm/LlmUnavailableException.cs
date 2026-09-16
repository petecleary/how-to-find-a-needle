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
            + (options.Provider == LlmProviders.Ollama ? "A large model on first use can be slow to load: try again, or choose a smaller Llm:Model." : "Try again, or choose a faster Llm:Model."),
            innerException);

    private static string HowToConfigure(LlmOptions options) => options.Provider switch
    {
        LlmProviders.Ollama => "Set Llm:Endpoint (e.g. http://localhost:11434) and Llm:Model in appsettings.json.",
        _ => $"Set the key with: dotnet user-secrets set \"Llm:ApiKey\" \"<your {options.Provider} key>\" --project src/PI.SearchApi",
    };

    private static string UnreachableGuidance(LlmOptions options) => options.Provider switch
    {
        LlmProviders.Ollama =>
            $"Is Ollama running at {options.Endpoint}? Start it with `ollama serve`, and make sure the model is pulled: `ollama pull {options.Model}`.",
        _ => $"The {options.Provider} API couldn't be used with model '{options.Model}'. Check Llm:Model and that the Llm:ApiKey user secret is valid.",
    };
}

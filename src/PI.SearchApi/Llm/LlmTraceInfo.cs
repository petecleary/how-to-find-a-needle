using Microsoft.Extensions.AI;

namespace PI.SearchApi.Llm;

/// <summary>
/// What the trace says about the model behind an answer (ADR-0015): provider, model, the endpoint's host and the
/// settings sent. It is built from an allow-list of fields, so the API key can never reach the trace or the UI.
/// </summary>
public sealed record LlmTraceInfo(
    string Provider,
    string Model,
    string? EndpointHost,
    IReadOnlyDictionary<string, object?> Settings)
{
    public static LlmTraceInfo From(LlmOptions options, ChatOptions chatOptions) => new(
        options.Provider,
        options.Model,
        EndpointHostOf(options),
        new Dictionary<string, object?>
        {
            ["temperature"] = chatOptions.Temperature,
            ["maxOutputTokens"] = chatOptions.MaxOutputTokens,
            ["reasoningEffort"] = chatOptions.Reasoning?.Effort?.ToString(),
            ["timeoutSeconds"] = options.TimeoutSeconds,
            ["retries"] = 0,
        });

    // Only Ollama has a configurable endpoint; hosted providers use their SDK's default address.
    private static string? EndpointHostOf(LlmOptions options) =>
        options.Provider == LlmProviders.Ollama && Uri.TryCreate(options.Endpoint, UriKind.Absolute, out var uri)
            ? uri.Authority
            : null;
}

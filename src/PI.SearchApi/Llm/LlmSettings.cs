namespace PI.SearchApi.Llm;

/// <summary>
/// The learner's non-secret model settings, saved in <c>~/.needle/settings.json</c> (ADR-0019). Keys are never
/// stored here: they come from configuration or are held in memory for the session (<see cref="ISecretStore"/>).
/// The default model is not here either: it stays in the <c>Llm</c> configuration section (ADR-0015).
/// </summary>
public sealed record LlmSettings
{
    /// <summary>Per-provider settings, keyed by provider ID. A provider with no entry uses its defaults.</summary>
    public IReadOnlyDictionary<string, LlmProviderSettings> Providers { get; init; } =
        new Dictionary<string, LlmProviderSettings>();

    public LlmProviderSettings For(string providerId) =>
        Providers.TryGetValue(providerId, out var settings) ? settings : new LlmProviderSettings();
}

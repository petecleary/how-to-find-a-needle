using System.Collections.Concurrent;

namespace PI.SearchApi.Llm;

/// <summary>
/// Keys from, in order (ADR-0019): a key pasted into the UI for this session; the provider's own variable
/// (<c>ANTHROPIC_API_KEY</c> and so on) from configuration, so environment variables and <c>dotnet user-secrets</c>
/// both work; and, for the default provider only, <c>Llm:ApiKey</c>, the setting ADR-0015 introduced.
/// </summary>
public sealed class ConfigurationAndSessionSecretStore(IConfiguration configuration) : ISecretStore
{
    private readonly ConcurrentDictionary<string, string> sessionKeys = new();

    public string? Get(string providerId) =>
        sessionKeys.TryGetValue(providerId, out var key) ? key : FromConfiguration(providerId);

    public KeySource SourceOf(string providerId) =>
        sessionKeys.ContainsKey(providerId) ? KeySource.Session
        : FromConfiguration(providerId) is not null ? KeySource.Configuration
        : KeySource.None;

    public void SetForSession(string providerId, string key) => sessionKeys[providerId] = key.Trim();

    public void RemoveSessionKey(string providerId) => sessionKeys.TryRemove(providerId, out _);

    private string? FromConfiguration(string providerId)
    {
        var variable = LlmProviders.Find(providerId)?.KeyVariable;
        if (variable is not null && configuration[variable] is { Length: > 0 } key)
        {
            return key.Trim();
        }

        // Llm:ApiKey predates per-provider keys, so it belongs to whichever provider Llm:Provider names.
        var defaultProvider = configuration[$"{LlmOptions.SectionName}:{nameof(LlmOptions.Provider)}"] ?? LlmProviders.Ollama;
        return providerId == defaultProvider && configuration[$"{LlmOptions.SectionName}:{nameof(LlmOptions.ApiKey)}"] is { Length: > 0 } legacy
            ? legacy.Trim()
            : null;
    }
}

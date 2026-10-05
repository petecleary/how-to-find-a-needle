namespace PI.SearchApi.Llm;

/// <summary>
/// One provider in the catalogue (ADR-0019): what it is called, where it lives by default, and where its key comes
/// from. Fixed in code; the learner's changes (base URL, enabled) live in <see cref="LlmSettingsStore"/>.
/// </summary>
/// <param name="Id">The provider half of a model reference: <c>anthropic</c> in <c>anthropic/claude-sonnet-5</c>.</param>
/// <param name="KeyVariable">The configuration key (environment variable or user secret) holding the API key, if any.</param>
/// <param name="DefaultBaseUrl">The address used when the learner hasn't set one; null when there is no sensible default (Azure).</param>
/// <param name="NeedsKey">True when the provider refuses calls without a key.</param>
/// <param name="IsLocal">True for a server on this machine, which gets a short timeout when listing models.</param>
/// <param name="EnabledByDefault">Whether the picker lists it before the learner has changed anything.</param>
public sealed record LlmProviderDefinition(
    string Id,
    string Name,
    string Detail,
    string? KeyVariable,
    string? DefaultBaseUrl,
    bool NeedsKey,
    bool IsLocal,
    bool EnabledByDefault);

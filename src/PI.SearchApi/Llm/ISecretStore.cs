namespace PI.SearchApi.Llm;

/// <summary>
/// Where API keys come from (ADR-0019). Endpoints may report a key's <see cref="KeySource"/>, never the key itself.
/// An OS keychain would be another implementation of this interface.
/// </summary>
public interface ISecretStore
{
    /// <summary>The key for a provider, or null if there is none.</summary>
    string? Get(string providerId);

    /// <summary>Where <see cref="Get"/> would find the key, without revealing it.</summary>
    KeySource SourceOf(string providerId);

    /// <summary>Holds a key in memory until the API stops. It is never written to disk.</summary>
    void SetForSession(string providerId, string key);

    /// <summary>Forgets a session key. A key from configuration is unaffected.</summary>
    void RemoveSessionKey(string providerId);
}

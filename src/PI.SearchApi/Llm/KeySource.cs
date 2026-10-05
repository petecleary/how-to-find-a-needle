namespace PI.SearchApi.Llm;

/// <summary>Where a provider's API key comes from (ADR-0019). Reported to the UI instead of the key.</summary>
public enum KeySource
{
    /// <summary>No key.</summary>
    None,

    /// <summary>An environment variable or user secret, read at startup.</summary>
    Configuration,

    /// <summary>Pasted into the UI and held in memory until the API stops.</summary>
    Session,
}

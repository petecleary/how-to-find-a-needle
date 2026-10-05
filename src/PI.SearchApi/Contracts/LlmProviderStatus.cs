using PI.SearchApi.Llm;

namespace PI.SearchApi.Contracts;

/// <summary>
/// One provider as the Models and API keys panel shows it (ADR-0019): its catalogue entry, the learner's settings,
/// and where its key comes from. Built without network calls; <c>POST /api/providers/{id}/test</c> checks the connection.
/// </summary>
public sealed record LlmProviderStatus
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string Detail { get; init; }

    public required bool NeedsKey { get; init; }

    public required bool IsLocal { get; init; }

    public required bool Enabled { get; init; }

    /// <summary>The environment variable or user secret that holds the key, e.g. <c>ANTHROPIC_API_KEY</c>.</summary>
    public required string? KeyVariable { get; init; }

    /// <summary>Where the key comes from. The key itself is never returned.</summary>
    public required KeySource KeySource { get; init; }

    public required string? DefaultBaseUrl { get; init; }

    /// <summary>The address in use: the learner's setting, else the default. Null when one is needed (Azure).</summary>
    public required string? BaseUrl { get; init; }

    public required IReadOnlyList<string> ExtraModels { get; init; }
}

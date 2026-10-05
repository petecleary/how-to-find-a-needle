namespace PI.SearchApi.Contracts;

/// <summary>
/// One enabled provider's models (ADR-0019). When they couldn't be listed, <see cref="Problem"/> says why: a provider
/// that is down or has no key is shown, never silently left out.
/// </summary>
public sealed record ProviderModels
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required bool IsLocal { get; init; }

    public required IReadOnlyList<ModelInfo> Models { get; init; }

    public string? Problem { get; init; }
}

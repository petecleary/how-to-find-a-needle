namespace PI.SearchApi.Contracts;

/// <summary>The result of <c>POST /api/providers/{id}/test</c> (ADR-0019): did listing the models work, and if not, why.</summary>
public sealed record ConnectionTest
{
    public required bool Ok { get; init; }

    public required string Message { get; init; }

    public required int Models { get; init; }
}

namespace PI.SearchApi.Contracts;

/// <summary>
/// Whether a candidate works with the target device, and why (ADR-0013). Reasons quote the
/// domain rule and the spec values compared, so a flagged item is never just "hidden".
/// </summary>
public sealed record CompatibilityResult(CompatibilityStatus Status, IReadOnlyList<string> Reasons)
{
    /// <summary>The value every stage before Stage 5 returns: no rule was checked.</summary>
    public static CompatibilityResult NotEvaluated { get; } = new(CompatibilityStatus.NotEvaluated, []);
}

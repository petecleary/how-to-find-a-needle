namespace PI.SearchApi.Pipeline.Rag;

/// <summary>
/// How many products of each kind the model is given (ADR-0016). Bounded on purpose: a small model grounds better on
/// ten clear items than on fifty, and the answer is predictable. The trace shows exactly what was left in.
/// </summary>
public static class EvidenceLimits
{
    public const int Compatible = 5;

    /// <summary>Negative evidence: enough near misses to warn about, not so many that they drown the answer.</summary>
    public const int Incompatible = 3;

    public const int Unknown = 2;

    public const int NotChecked = 5;

    public static int For(EvidenceRole role) => role switch
    {
        EvidenceRole.Compatible => Compatible,
        EvidenceRole.Incompatible => Incompatible,
        EvidenceRole.Unknown => Unknown,
        EvidenceRole.NotChecked => NotChecked,
        _ => 1,
    };
}

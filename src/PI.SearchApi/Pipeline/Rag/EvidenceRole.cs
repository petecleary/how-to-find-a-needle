namespace PI.SearchApi.Pipeline.Rag;

/// <summary>Why a product is in the evidence set (ADR-0016). Each role has its own limit (<see cref="EvidenceLimits"/>).</summary>
public enum EvidenceRole
{
    /// <summary>The device the shopper owns: what the rules were checked against.</summary>
    TargetDevice,

    /// <summary>Every rule check passed: a candidate to recommend.</summary>
    Compatible,

    /// <summary>A rule check failed: given to the model so the answer can warn about it, with the reasons.</summary>
    Incompatible,

    /// <summary>A rule applies but couldn't be decided, e.g. no target device.</summary>
    Unknown,

    /// <summary>No rule was checked: constraints are off, or no rule covers this kind of product.</summary>
    NotChecked,
}

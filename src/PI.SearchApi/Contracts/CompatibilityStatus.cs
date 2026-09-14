namespace PI.SearchApi.Contracts;

/// <summary>The outcome of checking a candidate against the target device (ADR-0013).</summary>
public enum CompatibilityStatus
{
    /// <summary>No rule applies to this pair of product types, or constraints were switched off.</summary>
    NotEvaluated,

    /// <summary>Every check in every applicable rule passed.</summary>
    Compatible,

    /// <summary>At least one check failed. The item is kept, demoted, with its reasons.</summary>
    Incompatible,

    /// <summary>A rule applies but can't be decided: no target device, or a spec is missing.</summary>
    Unknown,
}

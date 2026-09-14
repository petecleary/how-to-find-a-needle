namespace PI.SearchApi.Contracts;

/// <summary>What the shopper brings to the search besides the words they typed (ADR-0003).</summary>
public sealed record SearchContext
{
    /// <summary>
    /// The target device: a product ID the shopper owns (e.g. "PROD-0001"). Stage 6 checks
    /// candidates against it with the ontology's compatibility rules (ADR-0013).
    /// </summary>
    public string? TargetProductId { get; init; }
}

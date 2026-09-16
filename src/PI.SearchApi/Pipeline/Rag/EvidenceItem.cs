using PI.SearchApi.Contracts;

namespace PI.SearchApi.Pipeline.Rag;

/// <summary>One product the model is given, with its compatibility verdict and why it was chosen (ADR-0016).</summary>
/// <param name="Description">Truncated to about 300 characters; null until read from the database.</param>
/// <param name="Rank">The product's position in Stage 5's order (1-based); null for the target device.</param>
public sealed record EvidenceItem(
    ProductSummary Product,
    string? Description,
    CompatibilityResult Compatibility,
    ConceptMatch? ConceptMatch,
    int? Rank,
    EvidenceRole Role,
    string WhyIncluded);

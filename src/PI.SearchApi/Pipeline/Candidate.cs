using PI.SearchApi.Contracts;

namespace PI.SearchApi.Pipeline;

/// <summary>
/// A retrieved product with its score and per-technique signals (ADR-0004). Candidates flow
/// between pipeline services; the endpoint pages them and maps them to <see cref="ProductResult"/>.
/// </summary>
/// <param name="Score">Null in Stage 1, which has no notion of relevance (ADR-0007).</param>
/// <param name="Compatibility">NotEvaluated until Stage 5 checks domain rules.</param>
public sealed record Candidate(
    ProductSummary Product,
    double? Score,
    CandidateSignals Signals,
    CompatibilityResult Compatibility);

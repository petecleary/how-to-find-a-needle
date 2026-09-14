namespace PI.SearchApi.Pipeline.Fusion;

/// <summary>One document after Reciprocal Rank Fusion (ADR-0011).</summary>
/// <param name="Score">RRF(d) = Σ wᵢ / (k + rᵢ(d)).</param>
/// <param name="FusedRank">1-based position in the fused order.</param>
/// <param name="Ranks">This document's 1-based rank in each input list, by list name; null where it was absent.</param>
/// <param name="Formula">The sum with the real numbers filled in, e.g. "PROD-0012: 1/(60+2) + 1/(60+1) = 0.03252".</param>
public sealed record FusedItem(
    string Id,
    double Score,
    int FusedRank,
    IReadOnlyDictionary<string, int?> Ranks,
    string Formula);

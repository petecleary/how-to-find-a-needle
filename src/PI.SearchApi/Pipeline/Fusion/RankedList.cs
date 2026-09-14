namespace PI.SearchApi.Pipeline.Fusion;

/// <summary>One retriever's opinion, as fusion sees it: an ordered list of IDs and a weight (ADR-0011).</summary>
/// <param name="Name">The retriever, e.g. "keyword" or "vector"; used in formula strings.</param>
/// <param name="Weight">wᵢ in RRF. 1.0 is plain RRF; 0 removes the list's influence.</param>
/// <param name="OrderedIds">Best first. Only the order matters — the retriever's raw scores are ignored.</param>
public sealed record RankedList(string Name, double Weight, IReadOnlyList<string> OrderedIds);

namespace PI.SearchApi.Contracts;

/// <summary>
/// Each technique's raw opinion about one product (ADR-0003). A null means that technique didn't
/// run, or didn't retrieve the product. Ranks are 1-based.
/// </summary>
public sealed record CandidateSignals
{
    /// <summary>Stage 1: true when the product matched every filter.</summary>
    public bool? StructuredMatch { get; init; }

    public int? KeywordRank { get; init; }

    /// <summary>ts_rank_cd — "BM25-style", not BM25 (ADR-0008).</summary>
    public double? KeywordScore { get; init; }

    public int? VectorRank { get; init; }

    /// <summary>Cosine distance: 0 means identical direction, 2 means opposite.</summary>
    public double? VectorDistance { get; init; }

    /// <summary>Stage 5 (optional, built last).</summary>
    public int? BgeDenseRank { get; init; }

    /// <summary>Stage 5 (optional, built last).</summary>
    public int? BgeSparseRank { get; init; }

    /// <summary>Rank after Reciprocal Rank Fusion.</summary>
    public int? FusedRank { get; init; }

    /// <summary>Stage 6: how the product relates to the concepts found in the query.</summary>
    public ConceptMatch? ConceptMatch { get; init; }
}

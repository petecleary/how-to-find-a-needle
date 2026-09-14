namespace PI.SearchApi.Contracts;

/// <summary>
/// Stage-specific tuning values (ADR-0003). Every stage accepts all of them and uses only the
/// ones that apply; its trace lists the options it actually used.
/// </summary>
public sealed record SearchOptions
{
    /// <summary>
    /// How many candidates each retriever returns (10–200). "Retrieve deep, page late": fusion and
    /// rule checks run over this many items, so RRF isn't limited to the first page (ADR-0004).
    /// </summary>
    public int CandidateDepth { get; init; } = 50;

    /// <summary>RRF's k (1–1000). Larger k flattens the advantage of being ranked first (ADR-0011).</summary>
    public int RrfK { get; init; } = 60;

    /// <summary>Weight of the keyword list in RRF (0–10). 1.0 is plain RRF.</summary>
    public double KeywordWeight { get; init; } = 1.0;

    /// <summary>Weight of the vector list in RRF (0–10). 1.0 is plain RRF.</summary>
    public double VectorWeight { get; init; } = 1.0;

    /// <summary>Stage 6: expand matched concepts into synonyms and narrower concepts (ADR-0013).</summary>
    public bool ExpandSynonyms { get; init; } = true;

    /// <summary>Stage 6: classify candidates and check domain rules against the target device (ADR-0013).</summary>
    public bool ApplyConstraints { get; init; } = true;

    /// <summary>Stage 8: <c>novice</c>, <c>enthusiast</c> or <c>expert</c> (ADR-0017).</summary>
    public string Audience { get; init; } = "novice";

    /// <summary>Adds Postgres's EXPLAIN plan to vector trace steps (ADR-0010).</summary>
    public bool Explain { get; init; }
}

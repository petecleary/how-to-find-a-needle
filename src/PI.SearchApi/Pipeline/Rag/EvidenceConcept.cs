namespace PI.SearchApi.Pipeline.Rag;

/// <summary>
/// A concept the query matched, in the domain's own words (ADR-0016, ADR-0017): its English preferred label, its
/// English alternative labels (everyday words Stage 7 can use for a novice) and its skos:definition. Hidden labels
/// (misspellings) are never included: they're for matching, not for writing.
/// </summary>
public sealed record EvidenceConcept(
    string Notation,
    string PrefLabel,
    IReadOnlyList<string> AltLabels,
    string? Definition);

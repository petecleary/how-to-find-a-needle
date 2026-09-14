namespace PI.SearchApi.Pipeline.Keyword;

/// <summary>
/// A keyword query rewritten by Stage 6's ontology expansion (ADR-0008, ADR-0013).
/// </summary>
/// <param name="OrGroups">
/// One group per phrase the ontology recognised, holding that phrase and its synonyms, e.g.
/// ["power brick", "ac adapter", "charger"]. Terms within a group are OR-ed; groups are AND-ed.
/// </param>
/// <param name="RemainingText">The words no label matched, parsed as normal web-search input.</param>
public sealed record KeywordExpansion(IReadOnlyList<IReadOnlyList<string>> OrGroups, string RemainingText);

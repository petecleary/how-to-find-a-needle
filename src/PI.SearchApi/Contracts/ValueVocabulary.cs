namespace PI.SearchApi.Contracts;

/// <summary>
/// One value vocabulary returned by <c>GET /api/vocabularies</c> (ADR-0013): the allowed values of a
/// constrained spec, such as connectors. The UI builds its spec filters from it, so a value added to
/// the ontology appears in the filters without a UI change.
/// </summary>
public sealed record ValueVocabulary
{
    /// <summary>The scheme's notation, e.g. "connectors".</summary>
    public required string Notation { get; init; }

    /// <summary>The scheme's English name, e.g. "Connectors".</summary>
    public required string Label { get; init; }

    /// <summary>
    /// The product spec keys whose values come from this vocabulary, read from the compatibility
    /// rules, e.g. ["chargingPort", "connector"]. A filter sends one of them in <c>filters.specs</c>.
    /// Empty when no rule uses the vocabulary.
    /// </summary>
    public required IReadOnlyList<string> Specs { get; init; }

    public required IReadOnlyList<ValueVocabularyEntry> Values { get; init; }
}

namespace PI.SearchApi.Pipeline.Ontology;

/// <summary>
/// Read-only access to the domain ontology (ADR-0013): the taxonomy of product
/// categories, value vocabularies for constrained spec values, and class-level
/// compatibility rules. Loaded once from <c>domain-ontology.ttl</c>.
/// </summary>
/// <remarks>
/// This is the Phase 1 surface: catalog validation tests use it to check that
/// <c>products.json</c> only references real categories, vocabulary values and
/// rule-required specs. Phase 2 (ADR-0013, Stage 6) builds the label matcher,
/// synonym expansion and rule evaluation on top of the same loaded graph.
/// </remarks>
public interface IOntology
{
    /// <summary>Every taxonomy concept (product category), keyed by nothing in particular — see <see cref="TryGetConcept"/>.</summary>
    IReadOnlyList<OntologyConcept> Concepts { get; }

    /// <summary>Every class-level compatibility rule.</summary>
    IReadOnlyList<CompatibilityRule> Rules { get; }

    /// <summary>Looks up one taxonomy concept by its notation (e.g. "laptop-chargers").</summary>
    bool TryGetConcept(string notation, out OntologyConcept concept);

    /// <summary>
    /// True if <paramref name="notation"/> is <paramref name="ancestorNotation"/> itself, or reachable
    /// from it by one or more skos:broader steps. This is how a rule's accessoryType of "ssds" also
    /// matches a product categorised as the narrower "nvme-ssds".
    /// </summary>
    bool IsNarrowerOrSelf(string notation, string ancestorNotation);

    /// <summary>
    /// Every concept in a value vocabulary scheme (e.g. "connectors"), with every label that can
    /// identify it. Empty if the scheme notation is unknown.
    /// </summary>
    IReadOnlyList<VocabularyValue> VocabularyValues(string schemeNotation);
}

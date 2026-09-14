namespace PI.SearchApi.Pipeline.Ontology;

/// <summary>
/// Read-only access to the domain ontology (ADR-0013): the taxonomy of product categories, every
/// label in every language, value vocabularies for constrained spec values, and class-level
/// compatibility rules. Loaded once from <c>domain-ontology.ttl</c> and registered as a singleton.
/// </summary>
/// <remarks>
/// One domain model serves many jobs: category filters (Stage 1), the taxonomy endpoint, catalog
/// validation tests, and Stage 5's query understanding, classification and rule checks.
/// </remarks>
public interface IOntology
{
    /// <summary>Every taxonomy concept (product category), ordered by notation.</summary>
    IReadOnlyList<OntologyConcept> Concepts { get; }

    /// <summary>Every label (preferred, alternative, hidden) for every concept, taxonomy and vocabulary alike.</summary>
    IReadOnlyList<ConceptLabel> Labels { get; }

    /// <summary>
    /// Every value vocabulary (connectors, storage interfaces, …) with its values, ordered by notation.
    /// <c>GET /api/vocabularies</c> returns them so the UI's spec filters come from the ontology.
    /// </summary>
    IReadOnlyList<OntologyVocabulary> Vocabularies { get; }

    /// <summary>Every class-level compatibility rule.</summary>
    IReadOnlyList<CompatibilityRule> Rules { get; }

    /// <summary>The SPARQL text of <c>rules.rq</c>, so Stage 5's trace can show the query behind the rules.</summary>
    string RulesSparql { get; }

    /// <summary>Looks up one taxonomy concept by its notation (e.g. "laptop-chargers").</summary>
    bool TryGetConcept(string notation, out OntologyConcept concept);

    /// <summary>
    /// True if <paramref name="notation"/> is <paramref name="ancestorNotation"/> itself, or reachable
    /// from it by one or more skos:broader steps. This is how a rule's accessoryType of "ssds" also
    /// matches a product categorised as the narrower "nvme-ssds".
    /// </summary>
    bool IsNarrowerOrSelf(string notation, string ancestorNotation);

    /// <summary>
    /// The concept itself plus every concept below it, e.g. "chargers" → chargers, laptop-chargers,
    /// phone-chargers, usb-c-pd-chargers. Empty if the notation is unknown.
    /// </summary>
    IReadOnlySet<string> NarrowerOrSelf(string notation);

    /// <summary>
    /// The chain from <paramref name="notation"/> up to the root, e.g. laptop-chargers → chargers → power.
    /// Stage 5's trace uses it to show why a candidate is "in concept".
    /// </summary>
    IReadOnlyList<string> BroaderChain(string notation);

    /// <summary>
    /// Every concept in a value vocabulary scheme (e.g. "connectors"), with every label that can
    /// identify it. Empty if the scheme notation is unknown.
    /// </summary>
    IReadOnlyList<VocabularyValue> VocabularyValues(string schemeNotation);

    /// <summary>
    /// Resolves a spec value to its vocabulary notation by notation or label, case-insensitively:
    /// "Type-C" and "usb-c" both resolve to "usb-c" in the "connectors" scheme.
    /// </summary>
    bool TryResolveVocabularyValue(string schemeNotation, string value, out string notation);

    /// <summary>
    /// The rules that apply when an accessory in <paramref name="accessoryCategories"/> meets a
    /// device in <paramref name="deviceCategories"/>, matching narrower categories too.
    /// </summary>
    IReadOnlyList<CompatibilityRule> RulesFor(
        IEnumerable<string> accessoryCategories,
        IEnumerable<string> deviceCategories);
}

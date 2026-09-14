namespace PI.SearchApi.Pipeline.Ontology;

/// <summary>
/// One SKOS concept from the taxonomy: a product category such as "laptop-chargers".
/// </summary>
public sealed record OntologyConcept(
    string Notation,
    string? BroaderNotation,
    bool IsDeviceType,
    string? Icon,
    IReadOnlyDictionary<string, string> PrefLabels,
    IReadOnlyList<string> AltLabels,
    string? Definition);

/// <summary>Which SKOS labelling property a label came from.</summary>
public enum LabelKind
{
    /// <summary>skos:prefLabel — the name to display, one per language.</summary>
    Preferred,

    /// <summary>skos:altLabel — a synonym people use ("power brick").</summary>
    Alternative,

    /// <summary>skos:hiddenLabel — matched but never displayed, e.g. a common misspelling ("chager").</summary>
    Hidden,
}

/// <summary>
/// One label for one concept, in one language. Stage 5's label matcher reads every label, in every
/// language, from the ontology — so "cargador" finds Chargers without a line of Spanish-aware code.
/// </summary>
/// <param name="SchemeNotation">Null for taxonomy concepts; the vocabulary scheme (e.g. "connectors") otherwise.</param>
public sealed record ConceptLabel(
    string ConceptNotation,
    string? SchemeNotation,
    string Label,
    string Language,
    LabelKind Kind)
{
    /// <summary>True for product categories; false for value-vocabulary concepts such as "usb-c".</summary>
    public bool IsTaxonomyConcept => SchemeNotation is null;
}

/// <summary>
/// One concept from a value vocabulary (for example a connector or a battery platform),
/// with every label that can identify it — used to check that a product's spec value
/// is a known notation or a known label ("Type-C" as well as "usb-c").
/// </summary>
public sealed record VocabularyValue(string Notation, IReadOnlyList<string> Labels);

/// <summary>
/// One check inside a compatibility rule: compare an accessory's spec against a device's
/// spec with an operator. <see cref="ValueSchemeNotation"/> is set when the values are
/// vocabulary concepts (ADR-0013) rather than raw numbers.
/// </summary>
public sealed record RuleCheck(
    string AccessorySpec,
    string Operator,
    string DeviceSpec,
    string? ValueSchemeNotation,
    string Definition);

/// <summary>
/// A class-level compatibility rule: every check that must pass for an accessory type
/// to be compatible with a device type (ADR-0013). Products never carry rules — only
/// the spec values the rules compare.
/// </summary>
public sealed record CompatibilityRule(
    string AccessoryTypeNotation,
    string DeviceTypeNotation,
    IReadOnlyList<RuleCheck> Checks);

namespace PI.SearchApi.Pipeline.Ontology;

/// <summary>
/// One SKOS concept from the taxonomy: a product category such as "laptop-chargers".
/// </summary>
public sealed record OntologyConcept(
    string Notation,
    string? BroaderNotation,
    bool IsDeviceType,
    string? Icon,
    IReadOnlyDictionary<string, string> PrefLabels);

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

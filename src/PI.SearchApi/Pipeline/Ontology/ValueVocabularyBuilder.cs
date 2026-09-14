using PI.SearchApi.Contracts;

namespace PI.SearchApi.Pipeline.Ontology;

/// <summary>
/// Shapes the ontology's value vocabularies for <c>GET /api/vocabularies</c> (ADR-0013). Which spec
/// keys a vocabulary fills comes from the compatibility rules: a check with ex:valueScheme names the
/// accessory spec and the device spec that hold its values. So the ontology, not the UI, decides
/// which spec a filter targets.
/// </summary>
public static class ValueVocabularyBuilder
{
    public static IReadOnlyList<ValueVocabulary> Build(
        IReadOnlyList<OntologyVocabulary> vocabularies,
        IReadOnlyList<CompatibilityRule> rules)
    {
        // "connectors" → ["chargingPort", "connector"]: both sides of every check that names the scheme.
        // "memory-types" → ["memoryType"]: the laptop and the module use the same key, listed once.
        var vocabularyChecks = rules
            .SelectMany(rule => rule.Checks)
            .Where(check => check.ValueSchemeNotation is not null);

        var specsByScheme = vocabularyChecks
            .SelectMany(check => new[]
            {
                (Scheme: check.ValueSchemeNotation!, Spec: check.AccessorySpec),
                (Scheme: check.ValueSchemeNotation!, Spec: check.DeviceSpec),
            })
            .GroupBy(pair => pair.Scheme)
            .ToDictionary(
                group => group.Key,
                group => group.Select(pair => pair.Spec).Distinct().Order(StringComparer.Ordinal).ToList());

        return
        [
            .. vocabularies.Select(vocabulary => new ValueVocabulary
            {
                Notation = vocabulary.Notation,
                Label = vocabulary.Label,
                Specs = specsByScheme.GetValueOrDefault(vocabulary.Notation) ?? [],
                Values = [.. vocabulary.Concepts.Select(ToEntry)],
            }),
        ];
    }

    private static ValueVocabularyEntry ToEntry(OntologyVocabularyConcept concept) => new()
    {
        Notation = concept.Notation,
        Label = concept.PrefLabels.GetValueOrDefault("en") ?? concept.Notation,
        Labels = concept.PrefLabels,
        AltLabels = concept.AltLabels,
    };
}

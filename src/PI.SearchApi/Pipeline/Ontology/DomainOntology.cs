using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Query;

namespace PI.SearchApi.Pipeline.Ontology;

// Stage 6 vocabulary — Domain ontology loader (ADR-0013)
//
// What:     Loads domain-ontology.ttl (SKOS taxonomy, value vocabularies and class-level
//           compatibility rules) into an in-memory RDF graph, and answers lookups with the
//           SPARQL queries in assets/data/queries/*.rq via the Leviathan engine.
// Strength: The taxonomy, synonyms and rules are data, not code: adding a category or a rule
//           never touches C#, and the exact SPARQL a learner reads is the SPARQL that runs.
// Failure:  This is the Phase 1 vocabulary surface only — no label matching, no rule
//           evaluation. Those need a candidate and a target device, and land with the
//           Stage 6 pipeline in Phase 2.
// Decision: docs/adr/0013-domain-ontology-and-compatibility.md
public sealed class DomainOntology : IOntology
{
    private readonly Dictionary<string, OntologyConcept> _conceptsByNotation;

    // ancestor notation -> every notation reachable by skos:broader* (including itself).
    private readonly Dictionary<string, HashSet<string>> _narrowerOrSelfByAncestor;

    private readonly Dictionary<string, List<VocabularyValue>> _vocabularyValuesByScheme;

    public IReadOnlyList<OntologyConcept> Concepts { get; }

    public IReadOnlyList<CompatibilityRule> Rules { get; }

    /// <param name="dataDirectory">The assets/data directory containing domain-ontology.ttl and queries/.</param>
    public DomainOntology(string dataDirectory)
    {
        var ttlPath = Path.Combine(dataDirectory, "domain-ontology.ttl");
        var queriesDirectory = Path.Combine(dataDirectory, "queries");

        var graph = new Graph();
        FileLoader.Load(graph, ttlPath);

        _conceptsByNotation = LoadConcepts(graph, queriesDirectory);
        Concepts = [.. _conceptsByNotation.Values.OrderBy(c => c.Notation, StringComparer.Ordinal)];

        _narrowerOrSelfByAncestor = LoadNarrowerOrSelf(graph, queriesDirectory);
        _vocabularyValuesByScheme = LoadVocabularyValues(graph, queriesDirectory);
        Rules = LoadRules(graph, queriesDirectory);
    }

    public bool TryGetConcept(string notation, out OntologyConcept concept) =>
        _conceptsByNotation.TryGetValue(notation, out concept!);

    public bool IsNarrowerOrSelf(string notation, string ancestorNotation) =>
        _narrowerOrSelfByAncestor.TryGetValue(ancestorNotation, out var descendants)
        && descendants.Contains(notation);

    public IReadOnlyList<VocabularyValue> VocabularyValues(string schemeNotation) =>
        _vocabularyValuesByScheme.TryGetValue(schemeNotation, out var values)
            ? values
            : [];

    private static Dictionary<string, OntologyConcept> LoadConcepts(IGraph graph, string queriesDirectory)
    {
        var results = RunQuery(graph, queriesDirectory, "taxonomy.rq");

        // taxonomy.rq returns one row per (concept, label) pair, so a concept with an
        // English and a Spanish label produces two rows sharing the same broader/icon/
        // isDeviceType bindings. Group by notation and merge the labels.
        var byNotation = new Dictionary<string, (string? Broader, string? Icon, bool IsDeviceType, Dictionary<string, string> Labels)>();

        foreach (var row in results)
        {
            var notation = LiteralValue(row, "notation")!;
            var lang = LiteralValue(row, "lang") ?? "";
            var label = LiteralValue(row, "label")!;

            if (!byNotation.TryGetValue(notation, out var entry))
            {
                entry = (
                    LiteralValue(row, "broaderNotation"),
                    LiteralValue(row, "icon"),
                    ParseBool(LiteralValue(row, "isDeviceType")),
                    []);
            }

            entry.Labels[lang] = label;
            byNotation[notation] = entry;
        }

        return byNotation.ToDictionary(
            kv => kv.Key,
            kv => new OntologyConcept(kv.Key, kv.Value.Broader, kv.Value.IsDeviceType, kv.Value.Icon, kv.Value.Labels));
    }

    private static Dictionary<string, HashSet<string>> LoadNarrowerOrSelf(IGraph graph, string queriesDirectory)
    {
        var results = RunQuery(graph, queriesDirectory, "narrower.rq");
        var byAncestor = new Dictionary<string, HashSet<string>>();

        foreach (var row in results)
        {
            var ancestor = LiteralValue(row, "ancestorNotation")!;
            var descendant = LiteralValue(row, "descendantNotation")!;

            if (!byAncestor.TryGetValue(ancestor, out var descendants))
            {
                descendants = [];
                byAncestor[ancestor] = descendants;
            }

            descendants.Add(descendant);
        }

        return byAncestor;
    }

    private static Dictionary<string, List<VocabularyValue>> LoadVocabularyValues(IGraph graph, string queriesDirectory)
    {
        var results = RunQuery(graph, queriesDirectory, "labels.rq");

        // Group by (scheme, concept notation), collecting preferred and alternate labels.
        // Hidden labels (misspellings) exist for query-time tolerance, not as "known" values,
        // so they don't count here.
        var byScheme = new Dictionary<string, Dictionary<string, List<string>>>();

        foreach (var row in results)
        {
            var schemeNotation = LiteralValue(row, "schemeNotation");
            if (schemeNotation is null)
            {
                continue; // Taxonomy concepts (ex:Taxonomy has no notation) aren't a value vocabulary.
            }

            var kind = LiteralValue(row, "kind");
            if (kind == "hidden")
            {
                continue;
            }

            var notation = LiteralValue(row, "notation")!;
            var label = LiteralValue(row, "label")!;

            if (!byScheme.TryGetValue(schemeNotation, out var byNotation))
            {
                byNotation = [];
                byScheme[schemeNotation] = byNotation;
            }

            if (!byNotation.TryGetValue(notation, out var labels))
            {
                labels = [];
                byNotation[notation] = labels;
            }

            labels.Add(label);
        }

        return byScheme.ToDictionary(
            scheme => scheme.Key,
            scheme => scheme.Value
                .Select(concept => new VocabularyValue(concept.Key, concept.Value))
                .ToList());
    }

    private static List<CompatibilityRule> LoadRules(IGraph graph, string queriesDirectory)
    {
        var results = RunQuery(graph, queriesDirectory, "rules.rq");

        // One row per check; group into rules by (accessoryType, deviceType).
        var checksByType = new Dictionary<(string AccessoryType, string DeviceType), List<RuleCheck>>();

        foreach (var row in results)
        {
            var key = (LiteralValue(row, "accessoryTypeNotation")!, LiteralValue(row, "deviceTypeNotation")!);
            var check = new RuleCheck(
                LiteralValue(row, "accessorySpec")!,
                UriLocalName(row, "operator")!,
                LiteralValue(row, "deviceSpec")!,
                LiteralValue(row, "valueSchemeNotation"),
                LiteralValue(row, "definition")!);

            if (!checksByType.TryGetValue(key, out var checks))
            {
                checks = [];
                checksByType[key] = checks;
            }

            checks.Add(check);
        }

        return [.. checksByType.Select(kv => new CompatibilityRule(kv.Key.AccessoryType, kv.Key.DeviceType, kv.Value))];
    }

    private static SparqlResultSet RunQuery(IGraph graph, string queriesDirectory, string fileName)
    {
        var queryText = File.ReadAllText(Path.Combine(queriesDirectory, fileName));

        return graph.ExecuteQuery(queryText) as SparqlResultSet
            ?? throw new InvalidOperationException($"{fileName} did not produce a SPARQL result set.");
    }

    private static string? LiteralValue(ISparqlResult row, string variable) =>
        row.HasValue(variable) && row[variable] is ILiteralNode literal ? literal.Value : null;

    private static string? UriLocalName(ISparqlResult row, string variable) =>
        row.HasValue(variable) && row[variable] is IUriNode uri ? uri.Uri.Fragment.TrimStart('#') : null;

    private static bool ParseBool(string? value) =>
        value is not null && bool.TryParse(value, out var result) && result;
}

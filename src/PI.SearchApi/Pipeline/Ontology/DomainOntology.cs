using VDS.RDF;
using VDS.RDF.Parsing;
using VDS.RDF.Query;

namespace PI.SearchApi.Pipeline.Ontology;

// Stage 5 knowledge — Domain ontology loader (ADR-0013)
//
// What:     Loads domain-ontology.ttl (SKOS taxonomy, multilingual labels, value vocabularies
//           and class-level compatibility rules) into an in-memory RDF graph, and answers
//           lookups with the SPARQL queries in assets/data/queries/*.rq (Leviathan engine).
// Strength: The taxonomy, synonyms and rules are data, not code: adding a category, a Spanish
//           label or a rule never touches C#, and the SPARQL a learner reads is what runs.
// Failure:  No OWL reasoning and no instance data: subsumption is skos:broader* only, and the
//           ontology knows product *types*, never individual products. That boundary is on
//           purpose; past it you're building a knowledge graph.
// Decision: docs/decisions/0013-domain-ontology-and-compatibility.md
public sealed class DomainOntology : IOntology
{
    private readonly Dictionary<string, OntologyConcept> _conceptsByNotation;

    // ancestor notation -> every notation reachable by skos:broader* (including itself).
    private readonly Dictionary<string, HashSet<string>> _narrowerOrSelfByAncestor;

    private readonly Dictionary<string, List<VocabularyValue>> _vocabularyValuesByScheme;

    public IReadOnlyList<OntologyConcept> Concepts { get; }

    public IReadOnlyList<ConceptLabel> Labels { get; }

    public IReadOnlyList<OntologyVocabulary> Vocabularies { get; }

    public IReadOnlyList<CompatibilityRule> Rules { get; }

    public string RulesSparql { get; }

    /// <param name="dataDirectory">The assets/data directory containing domain-ontology.ttl and queries/.</param>
    public DomainOntology(string dataDirectory)
    {
        var ttlPath = Path.Combine(dataDirectory, "domain-ontology.ttl");
        var queriesDirectory = Path.Combine(dataDirectory, "queries");

        var graph = new Graph();
        FileLoader.Load(graph, ttlPath);

        Labels = LoadLabels(graph, queriesDirectory);

        _conceptsByNotation = LoadConcepts(graph, queriesDirectory, Labels);
        Concepts = [.. _conceptsByNotation.Values.OrderBy(c => c.Notation, StringComparer.Ordinal)];

        _narrowerOrSelfByAncestor = LoadNarrowerOrSelf(graph, queriesDirectory);
        _vocabularyValuesByScheme = GroupVocabularyValues(Labels);
        Vocabularies = LoadVocabularies(graph, queriesDirectory, Labels);

        RulesSparql = File.ReadAllText(Path.Combine(queriesDirectory, "rules.rq"));
        Rules = LoadRules(graph, RulesSparql);
    }

    public bool TryGetConcept(string notation, out OntologyConcept concept) =>
        _conceptsByNotation.TryGetValue(notation, out concept!);

    public bool IsNarrowerOrSelf(string notation, string ancestorNotation) =>
        _narrowerOrSelfByAncestor.TryGetValue(ancestorNotation, out var descendants)
        && descendants.Contains(notation);

    public IReadOnlySet<string> NarrowerOrSelf(string notation) =>
        _narrowerOrSelfByAncestor.TryGetValue(notation, out var descendants)
            ? descendants
            : new HashSet<string>();

    public IReadOnlyList<string> BroaderChain(string notation)
    {
        var chain = new List<string>();
        var current = notation;

        // The taxonomy is a tree (one skos:broader per concept), so walking up terminates at a root.
        while (_conceptsByNotation.TryGetValue(current, out var concept))
        {
            chain.Add(concept.Notation);

            if (concept.BroaderNotation is null)
            {
                break;
            }

            current = concept.BroaderNotation;
        }

        return chain;
    }

    public IReadOnlyList<VocabularyValue> VocabularyValues(string schemeNotation) =>
        _vocabularyValuesByScheme.TryGetValue(schemeNotation, out var values)
            ? values
            : [];

    public bool TryResolveVocabularyValue(string schemeNotation, string value, out string notation)
    {
        var match = VocabularyValues(schemeNotation).FirstOrDefault(v =>
            string.Equals(v.Notation, value, StringComparison.OrdinalIgnoreCase)
            || v.Labels.Any(label => string.Equals(label, value, StringComparison.OrdinalIgnoreCase)));

        notation = match?.Notation ?? "";
        return match is not null;
    }

    public IReadOnlyList<CompatibilityRule> RulesFor(
        IEnumerable<string> accessoryCategories,
        IEnumerable<string> deviceCategories)
    {
        var accessory = accessoryCategories.ToList();
        var device = deviceCategories.ToList();

        return
        [
            .. Rules.Where(rule =>
                accessory.Any(c => IsNarrowerOrSelf(c, rule.AccessoryTypeNotation))
                && device.Any(c => IsNarrowerOrSelf(c, rule.DeviceTypeNotation))),
        ];
    }

    private static List<ConceptLabel> LoadLabels(IGraph graph, string queriesDirectory)
    {
        var results = RunQuery(graph, File.ReadAllText(Path.Combine(queriesDirectory, "labels.rq")), "labels.rq");

        return
        [
            .. results.Select(row => new ConceptLabel(
                LiteralValue(row, "notation")!,
                LiteralValue(row, "schemeNotation"),
                LiteralValue(row, "label")!,
                LiteralValue(row, "lang") ?? "",
                LiteralValue(row, "kind") switch
                {
                    "pref" => LabelKind.Preferred,
                    "alt" => LabelKind.Alternative,
                    _ => LabelKind.Hidden,
                })),
        ];
    }

    private static Dictionary<string, OntologyConcept> LoadConcepts(
        IGraph graph, string queriesDirectory, IReadOnlyList<ConceptLabel> labels)
    {
        var results = RunQuery(graph, File.ReadAllText(Path.Combine(queriesDirectory, "taxonomy.rq")), "taxonomy.rq");

        // taxonomy.rq returns one row per (concept, preferred label) pair, so a concept with an
        // English and a Spanish label produces two rows sharing the same broader/icon/definition
        // bindings. Group by notation and merge the labels.
        var byNotation = new Dictionary<string, (string? Broader, string? Icon, bool IsDeviceType, string? Definition, Dictionary<string, string> Labels)>();

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
                    LiteralValue(row, "definition"),
                    []);
            }

            entry.Labels[lang] = label;
            byNotation[notation] = entry;
        }

        var altLabelsByNotation = labels
            .Where(l => l.IsTaxonomyConcept && l.Kind == LabelKind.Alternative)
            .GroupBy(l => l.ConceptNotation)
            .ToDictionary(g => g.Key, g => g.Select(l => l.Label).ToList());

        return byNotation.ToDictionary(
            kv => kv.Key,
            kv => new OntologyConcept(
                kv.Key,
                kv.Value.Broader,
                kv.Value.IsDeviceType,
                kv.Value.Icon,
                kv.Value.Labels,
                altLabelsByNotation.GetValueOrDefault(kv.Key) ?? [],
                kv.Value.Definition));
    }

    private static Dictionary<string, HashSet<string>> LoadNarrowerOrSelf(IGraph graph, string queriesDirectory)
    {
        var results = RunQuery(graph, File.ReadAllText(Path.Combine(queriesDirectory, "narrower.rq")), "narrower.rq");
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

    private static Dictionary<string, List<VocabularyValue>> GroupVocabularyValues(IReadOnlyList<ConceptLabel> labels)
    {
        // Group vocabulary labels by (scheme, concept notation), collecting preferred and alternative
        // labels. Hidden labels (misspellings) exist for query-time tolerance, not as "known" values,
        // so they don't count here. Taxonomy concepts have no scheme notation and are skipped.
        return labels
            .Where(l => !l.IsTaxonomyConcept && l.Kind != LabelKind.Hidden)
            .GroupBy(l => l.SchemeNotation!)
            .ToDictionary(
                scheme => scheme.Key,
                scheme => scheme
                    .GroupBy(l => l.ConceptNotation)
                    .Select(concept => new VocabularyValue(concept.Key, [.. concept.Select(l => l.Label)]))
                    .ToList());
    }

    private static List<OntologyVocabulary> LoadVocabularies(
        IGraph graph, string queriesDirectory, IReadOnlyList<ConceptLabel> labels)
    {
        var results = RunQuery(graph, File.ReadAllText(Path.Combine(queriesDirectory, "vocabularies.rq")), "vocabularies.rq");

        // Like taxonomy.rq, vocabularies.rq returns one row per (value, preferred label) pair, so a
        // value with labels in two languages is two rows. Collect each scheme's name, then merge
        // each value's labels by language.
        var schemeLabels = new Dictionary<string, string>();
        var prefLabelsByValue = new Dictionary<(string Scheme, string Notation), Dictionary<string, string>>();

        foreach (var row in results)
        {
            var scheme = LiteralValue(row, "schemeNotation")!;
            var notation = LiteralValue(row, "notation")!;
            schemeLabels[scheme] = LiteralValue(row, "schemeLabel")!;

            if (!prefLabelsByValue.TryGetValue((scheme, notation), out var prefLabels))
            {
                prefLabels = [];
                prefLabelsByValue[(scheme, notation)] = prefLabels;
            }

            prefLabels[LiteralValue(row, "lang") ?? ""] = LiteralValue(row, "label")!;
        }

        // Synonyms come from labels.rq. Hidden labels (misspellings) help match a typed query;
        // a filter never displays them, so they're left out.
        var altLabelsByValue = labels
            .Where(l => !l.IsTaxonomyConcept && l.Kind == LabelKind.Alternative)
            .GroupBy(l => (Scheme: l.SchemeNotation!, Notation: l.ConceptNotation))
            .ToDictionary(g => g.Key, g => g.Select(l => l.Label).ToList());

        return
        [
            .. schemeLabels.Keys.Order(StringComparer.Ordinal).Select(scheme => new OntologyVocabulary(
                scheme,
                schemeLabels[scheme],
                [
                    .. prefLabelsByValue.Keys
                        .Where(key => key.Scheme == scheme)
                        .OrderBy(key => key.Notation, StringComparer.Ordinal)
                        .Select(key => new OntologyVocabularyConcept(
                            key.Notation,
                            prefLabelsByValue[key],
                            altLabelsByValue.GetValueOrDefault(key) ?? [])),
                ])),
        ];
    }

    private static List<CompatibilityRule> LoadRules(IGraph graph, string rulesSparql)
    {
        var results = RunQuery(graph, rulesSparql, "rules.rq");

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

    private static SparqlResultSet RunQuery(IGraph graph, string queryText, string fileName) =>
        graph.ExecuteQuery(queryText) as SparqlResultSet
            ?? throw new InvalidOperationException($"{fileName} did not produce a SPARQL result set.");

    private static string? LiteralValue(ISparqlResult row, string variable) =>
        row.HasValue(variable) && row[variable] is ILiteralNode literal ? literal.Value : null;

    private static string? UriLocalName(ISparqlResult row, string variable) =>
        row.HasValue(variable) && row[variable] is IUriNode uri ? uri.Uri.Fragment.TrimStart('#') : null;

    private static bool ParseBool(string? value) =>
        value is not null && bool.TryParse(value, out var result) && result;
}

using System.Diagnostics;
using PI.SearchApi.Contracts;
using PI.SearchApi.Pipeline.Hybrid;

namespace PI.SearchApi.Pipeline.Ontology;

// Stage 6 — Ontology: SKOS concepts, expansion and domain rules
//
// What:     Matches the query to SKOS concepts, expands them into synonyms and narrower
//           concepts, re-runs hybrid search with that better input, then classifies each
//           candidate (in or out of concept) and checks class-level rules against the target
//           device. Flagged items are moved down, never removed.
// Strength: Answers "how is it related and constrained?": recall improves through synonyms,
//           precision through classification, and correctness through rules with reasons.
// Failure:  Knows only what the ontology states. Unlisted synonyms, unmodelled product types or
//           rules beyond equals/≥/≤/in are invisible to it — and it still needs retrieval to find
//           the candidates in the first place.
// Decision: docs/adr/0013-domain-ontology-and-compatibility.md
public sealed class OntologySearch(
    IOntology ontology,
    LabelMatcher labelMatcher,
    QueryExpander expander,
    IHybridSearch hybridSearch,
    ConceptClassifier classifier,
    TargetDeviceResolver deviceResolver,
    CompatibilityEvaluator evaluator) : IOntologySearch
{
    public async Task<StageResult> SearchAsync(SearchRequest request, CancellationToken ct)
    {
        using var activity = PipelineTelemetry.Source.StartActivity("Stage 6: ontology search");
        var options = request.Options;

        // 1. Understand
        var start = Stopwatch.GetTimestamp();
        var understanding = labelMatcher.Understand(request.Query);
        var understandStep = UnderstandStep(understanding, PipelineTelemetry.ElapsedMs(start));

        // 2. Expand (optional)
        start = Stopwatch.GetTimestamp();
        var expansion = options.ExpandSynonyms && understanding.TaxonomyConcepts.Count > 0
            ? expander.Expand(understanding)
            : null;
        var expandStep = ExpandStep(options, understanding, expansion, PipelineTelemetry.ElapsedMs(start));

        // 3. Retrieve: the Stage 4 pipeline, with the expanded input
        var retrieved = await hybridSearch.SearchAsync(request, expansion?.Keyword, expansion?.EmbeddingText, ct);

        // 4. Classify
        start = Stopwatch.GetTimestamp();
        var matchedConcepts = understanding.TaxonomyConcepts;
        var classifications = retrieved.Candidates.ToDictionary(
            c => c.Product.Id,
            c => classifier.Classify(c.Product.Categories, matchedConcepts));
        var classifyStep = ClassifyStep(options, matchedConcepts, retrieved.Candidates, classifications, PipelineTelemetry.ElapsedMs(start));

        // 5. Constrain (optional)
        start = Stopwatch.GetTimestamp();
        var device = options.ApplyConstraints ? await deviceResolver.ResolveAsync(request, ct) : null;
        var evaluations = options.ApplyConstraints
            ? retrieved.Candidates.ToDictionary(c => c.Product.Id, c => evaluator.Evaluate(c.Product, device!.Product))
            : [];

        var evaluated = retrieved.Candidates.Select(candidate =>
        {
            var id = candidate.Product.Id;
            var compatibility = evaluations.TryGetValue(id, out var evaluation) ? evaluation.Result : CompatibilityResult.NotEvaluated;

            return candidate with
            {
                Signals = candidate.Signals with { ConceptMatch = classifications[id].Match },
                Compatibility = compatibility,
            };
        }).ToList();

        // Order, with flagged items kept (ADR-0013): unflagged, then OutOfConcept, then Incompatible.
        // OrderBy is stable, so each group keeps its fused order.
        var ordered = options.ApplyConstraints
            ? evaluated.OrderBy(FlagGroup).ToList()
            : evaluated;

        var constrainStep = ConstrainStep(options, device, evaluations, ordered, PipelineTelemetry.ElapsedMs(start));

        return new StageResult(ordered, [understandStep, expandStep, .. retrieved.Trace, classifyStep, constrainStep]);
    }

    private static int FlagGroup(Candidate candidate) =>
        candidate.Compatibility.Status == CompatibilityStatus.Incompatible ? 2
        : candidate.Signals.ConceptMatch == ConceptMatch.OutOfConcept ? 1
        : 0;

    private static TraceStep UnderstandStep(QueryUnderstanding understanding, double durationMs) => new()
    {
        Stage = "ontology",
        Title = "Understand: match query phrases to SKOS labels",
        DurationMs = durationMs,
        Details = new Dictionary<string, object?>
        {
            ["tokens"] = understanding.Tokens.Select(t => new { t.Original, t.Folded }).ToList(),
            ["matches"] = understanding.Matches.Select(m => new
            {
                phrase = m.Phrase,
                labels = m.Labels.Select(l => new
                {
                    concept = l.ConceptNotation,
                    scheme = l.SchemeNotation ?? "taxonomy",
                    label = l.Label,
                    language = l.Language,
                    kind = l.Kind.ToString(),
                }).ToList(),
            }).ToList(),
            ["taxonomyConcepts"] = understanding.TaxonomyConcepts,
            ["remainingText"] = understanding.RemainingText,
        },
        Notes =
        [
            $"Longest match first, no overlaps, phrases of 1–{LabelMatcher.MaxPhraseWords} words, labels in every language.",
            "Matching is lexical: lower case, accents folded and simple plurals folded. \"brick\" alone would not match \"power brick\".",
            understanding.TaxonomyConcepts.Count == 0
                ? "No category was recognised, so there is nothing to expand or classify against."
                : "Value concepts (such as a connector) are shown, but only categories are expanded and used for classification.",
        ],
    };

    private static TraceStep ExpandStep(SearchOptions options, QueryUnderstanding understanding, QueryExpansion? expansion, double durationMs) => new()
    {
        Stage = "ontology",
        Title = "Expand: synonyms and narrower concepts",
        DurationMs = durationMs,
        Details = new Dictionary<string, object?>
        {
            ["expandSynonyms"] = options.ExpandSynonyms,
            ["phrases"] = expansion?.Phrases.Select(p => new { phrase = p.Phrase, concepts = p.Concepts, terms = p.Terms }).ToList(),
            ["keywordOrGroups"] = expansion?.Keyword.OrGroups,
            ["keywordRemainingText"] = expansion?.Keyword.RemainingText,
            ["embeddingText"] = expansion?.EmbeddingText ?? understanding.Query,
        },
        Notes = expansion is null
            ? [options.ExpandSynonyms ? "Nothing to expand: no category was recognised in the query." : "expandSynonyms is off: keyword and vector search receive the query as typed."]
            :
            [
                $"Each matched phrase becomes an OR group of up to {QueryExpander.MaxTermsPerConcept} terms per concept, AND-ed with the rest of the query.",
                "Vector search embeds the query with the matched concepts' names appended.",
            ],
    };

    private TraceStep ClassifyStep(
        SearchOptions options,
        IReadOnlyList<string> matchedConcepts,
        IReadOnlyList<Candidate> candidates,
        Dictionary<string, ConceptClassification> classifications,
        double durationMs) => new()
    {
        Stage = "ontology",
        Title = "Classify: in or out of concept",
        DurationMs = durationMs,
        Details = new Dictionary<string, object?>
        {
            ["matchedConcepts"] = matchedConcepts,
            ["classifications"] = candidates.Select(c => new
            {
                id = c.Product.Id,
                categories = c.Product.Categories,
                conceptMatch = classifications[c.Product.Id].Match.ToString(),
                matchedConcept = classifications[c.Product.Id].MatchedConcept,
                broaderChain = classifications[c.Product.Id].Chain,
                // For OutOfConcept items, where each category actually sits in the taxonomy.
                categoryChains = c.Product.Categories.ToDictionary(cat => cat, cat => ontology.BroaderChain(cat)),
            }).ToList(),
        },
        Notes =
        [
            "InConcept: a category is a matched concept or narrower than one (skos:broader*). OutOfConcept: none is.",
            options.ApplyConstraints
                ? "OutOfConcept items are moved below InConcept ones, keeping their fused order. Nothing is removed."
                : "applyConstraints is off: the classification is shown, but nothing is demoted.",
        ],
    };

    private TraceStep ConstrainStep(
        SearchOptions options,
        TargetDevice? device,
        Dictionary<string, CompatibilityEvaluation> evaluations,
        IReadOnlyList<Candidate> ordered,
        double durationMs)
    {
        var details = new Dictionary<string, object?> { ["applyConstraints"] = options.ApplyConstraints };
        var notes = new List<string>();

        if (!options.ApplyConstraints)
        {
            notes.Add("applyConstraints is off: no rules were checked, and every result is NotEvaluated.");
        }
        else
        {
            var checks = evaluations.Values.SelectMany(e => e.Checks).ToList();

            details["targetDevice"] = device?.Product is { } p ? new { p.Id, p.Name, p.Categories } : null;
            details["targetDeviceMethod"] = device?.Method;
            details["rulesSparql"] = ontology.RulesSparql;
            details["rulesApplied"] = checks.Select(c => c.Rule).Distinct().ToList();
            details["checks"] = checks.Select(c => new
            {
                candidateId = c.CandidateId,
                rule = c.Rule,
                definition = c.Definition,
                accessorySpec = c.AccessorySpec,
                accessoryValue = c.AccessoryValue,
                @operator = c.Operator,
                deviceSpec = c.DeviceSpec,
                deviceValue = c.DeviceValue,
                result = c.Result.ToString(),
            }).ToList();
            details["flagged"] = ordered
                .Where(c => c.Compatibility.Status == CompatibilityStatus.Incompatible || c.Signals.ConceptMatch == ConceptMatch.OutOfConcept)
                .Select(c => new
                {
                    id = c.Product.Id,
                    name = c.Product.Name,
                    conceptMatch = c.Signals.ConceptMatch?.ToString(),
                    compatibility = c.Compatibility.Status.ToString(),
                    reasons = c.Compatibility.Reasons,
                })
                .ToList();
            details["finalOrder"] = ordered.Select(c => c.Product.Id).ToList();

            notes.Add("Rules are data from domain-ontology.ttl, found with rules.rq. The evaluator runs their checks and never names a rule.");
            notes.Add("Order: unflagged first, then OutOfConcept, then Incompatible; each group keeps its fused order. Nothing is removed.");

            if (device?.Product is null)
            {
                notes.Add("No target device, so any product a rule applies to is Unknown rather than Compatible.");
            }
        }

        return new TraceStep
        {
            Stage = "ontology",
            Title = "Constrain: domain rules vs the target device",
            DurationMs = durationMs,
            Details = details,
            Notes = notes,
        };
    }
}

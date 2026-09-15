using System.Diagnostics;
using PI.SearchApi.Contracts;
using PI.SearchApi.Pipeline.Hybrid;

namespace PI.SearchApi.Pipeline.Ontology;

// Stage 5 — Ontology: SKOS concepts, expansion and domain rules
//
// What:     Understands the query first — which device the shopper owns, which categories they
//           want — then expands the wanted concepts into synonyms and narrower concepts, re-runs
//           hybrid search with that better input, classifies each candidate (in or out of concept)
//           and checks class-level rules against the target device. Flagged items move down,
//           never out.
// Strength: Answers "how is it related and constrained?": a device name becomes context instead
//           of search text, recall improves through synonyms, precision through classification,
//           and correctness through rules with reasons.
// Failure:  Knows only what the ontology and catalog state. Unlisted synonyms, partial device names
//           ("my Aerobook"), unmodelled product types or rules beyond equals/≥/≤/in are invisible
//           to it — and it still needs retrieval to find the candidates in the first place.
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
    public async Task<StageResult> SearchAsync(SearchRequest request, CancellationToken ct) =>
        (await SearchWithContextAsync(request, ct)).Result;

    public async Task<OntologySearchResult> SearchWithContextAsync(SearchRequest request, CancellationToken ct)
    {
        using var activity = PipelineTelemetry.Source.StartActivity("Stage 5: ontology search");
        var options = request.Options;

        // 1. Understand: resolve the device first, so its name is claimed before label matching,
        //    then separate what the shopper wants from what only describes what they own.
        var start = Stopwatch.GetTimestamp();
        var device = await deviceResolver.ResolveAsync(request, TextNormaliser.Tokenise(request.Query), ct);
        var understanding = labelMatcher.Understand(request.Query, device.Mention);
        understanding = understanding with { ContextConcepts = ContextConceptsFor(understanding, device.Product) };
        var understandStep = UnderstandStep(understanding, device, PipelineTelemetry.ElapsedMs(start));

        // 2. Expand (optional). Also runs when the query only needs its device name removed.
        start = Stopwatch.GetTimestamp();
        var expansion = options.ExpandSynonyms && (understanding.WantedConcepts.Count > 0 || understanding.DeviceMention is not null)
            ? expander.Expand(understanding)
            : null;
        var expandStep = ExpandStep(options, understanding, expansion, PipelineTelemetry.ElapsedMs(start));

        // 3. Retrieve: the Stage 4 pipeline, with the rewritten input
        var retrieved = await hybridSearch.SearchAsync(request, expansion?.Keyword, expansion?.EmbeddingText, ct);

        // 4. Classify against the wanted concepts only
        start = Stopwatch.GetTimestamp();
        var wantedConcepts = understanding.WantedConcepts;
        var classifications = retrieved.Candidates.ToDictionary(
            c => c.Product.Id,
            c => classifier.Classify(c.Product.Categories, wantedConcepts));
        var classifyStep = ClassifyStep(options, understanding, retrieved.Candidates, classifications, PipelineTelemetry.ElapsedMs(start));

        // 5. Constrain (optional), using the device resolved in step 1
        start = Stopwatch.GetTimestamp();
        var targetId = device.Product?.Id;
        var evaluations = options.ApplyConstraints
            ? retrieved.Candidates.ToDictionary(c => c.Product.Id, c => evaluator.Evaluate(c.Product, device.Product))
            : [];

        var evaluated = retrieved.Candidates.Select(candidate =>
        {
            var id = candidate.Product.Id;
            var compatibility = evaluations.TryGetValue(id, out var evaluation) ? evaluation.Result : CompatibilityResult.NotEvaluated;

            if (options.ApplyConstraints && id == targetId)
            {
                compatibility = new CompatibilityResult(
                    CompatibilityStatus.NotEvaluated,
                    [$"This is your target device, {candidate.Product.Name}: it's shown below the products for it, not removed."]);
            }

            return candidate with
            {
                Signals = candidate.Signals with { ConceptMatch = classifications[id].Match },
                Compatibility = compatibility,
            };
        }).ToList();

        // Order, with flagged items kept (ADR-0013): unflagged, then OutOfConcept and the device itself,
        // then Incompatible. OrderBy is stable, so each group keeps its fused order.
        var ordered = options.ApplyConstraints
            ? evaluated.OrderBy(c => FlagGroup(c, targetId)).ToList()
            : evaluated;

        var constrainStep = ConstrainStep(options, device, evaluations, ordered, targetId, PipelineTelemetry.ElapsedMs(start));

        return new OntologySearchResult(
            new StageResult(ordered, [understandStep, expandStep, .. retrieved.Trace, classifyStep, constrainStep]),
            device,
            understanding,
            [.. evaluations.Values.SelectMany(e => e.Checks)]);
    }

    // A matched device-type concept that the target device belongs to describes what the shopper owns:
    // "laptop" in "power adapter for my laptop" when the device is a laptop.
    private IReadOnlyList<string> ContextConceptsFor(QueryUnderstanding understanding, ProductSummary? device) =>
        device is null
            ? []
            : [.. understanding.TaxonomyConcepts.Where(concept =>
                ontology.TryGetConcept(concept, out var c) && c.IsDeviceType
                && device.Categories.Any(category => ontology.IsNarrowerOrSelf(category, concept)))];

    private static int FlagGroup(Candidate candidate, string? targetId) =>
        candidate.Compatibility.Status == CompatibilityStatus.Incompatible ? 2
        : candidate.Signals.ConceptMatch == ConceptMatch.OutOfConcept || candidate.Product.Id == targetId ? 1
        : 0;

    private static TraceStep UnderstandStep(QueryUnderstanding understanding, TargetDevice device, double durationMs)
    {
        var notes = new List<string>
        {
            $"Longest match first, no overlaps, phrases of 1–{LabelMatcher.MaxPhraseWords} words, labels in every language.",
            "Matching is lexical: lower case, accents folded and simple plurals folded. \"brick\" alone would not match \"power brick\".",
        };

        if (understanding.DeviceMentionText is { } mention)
        {
            notes.Add($"\"{mention}\" names the target device: it's context, not something to search for, so it's removed from the search text.");
        }

        if (understanding.ContextConcepts.Count > 0)
        {
            notes.Add($"{string.Join(", ", understanding.ContextConcepts)} describes the device you own, so it isn't expanded or used for classification.");
        }

        notes.Add(understanding.WantedConcepts.Count == 0
            ? "No wanted category was recognised, so there is nothing to expand or classify against."
            : "Value concepts (such as a connector) are shown, but only wanted categories are expanded and used for classification.");

        return new TraceStep
        {
            Stage = "ontology",
            Title = "Understand: target device, and query phrases matched to SKOS labels",
            DurationMs = durationMs,
            Details = new Dictionary<string, object?>
            {
                ["targetDevice"] = device.Product is { } p ? new { p.Id, p.Name, p.Categories } : null,
                ["targetDeviceMethod"] = device.Method,
                ["deviceMention"] = understanding.DeviceMentionText,
                ["queryWithoutDevice"] = understanding.QueryWithoutDevice,
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
                ["wantedConcepts"] = understanding.WantedConcepts,
                ["contextConcepts"] = understanding.ContextConcepts,
                ["remainingText"] = understanding.RemainingText,
            },
            Notes = notes,
        };
    }

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
            ? [options.ExpandSynonyms ? "Nothing to rewrite: no wanted category and no device name in the query." : "expandSynonyms is off: keyword and vector search receive the query as typed."]
            :
            [
                $"Each wanted phrase becomes an OR group of up to {QueryExpander.MaxTermsPerConcept} terms per concept, AND-ed with the rest of the query.",
                "Vector search embeds the query, without the device name, with the wanted concepts' names appended.",
            ],
    };

    private TraceStep ClassifyStep(
        SearchOptions options,
        QueryUnderstanding understanding,
        IReadOnlyList<Candidate> candidates,
        Dictionary<string, ConceptClassification> classifications,
        double durationMs) => new()
    {
        Stage = "ontology",
        Title = "Classify: in or out of concept",
        DurationMs = durationMs,
        Details = new Dictionary<string, object?>
        {
            ["wantedConcepts"] = understanding.WantedConcepts,
            ["contextConcepts"] = understanding.ContextConcepts,
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
            "InConcept: a category is a wanted concept or narrower than one (skos:broader*). OutOfConcept: none is.",
            options.ApplyConstraints
                ? "OutOfConcept items are moved below InConcept ones, keeping their fused order. Nothing is removed."
                : "applyConstraints is off: the classification is shown, but nothing is demoted.",
        ],
    };

    private TraceStep ConstrainStep(
        SearchOptions options,
        TargetDevice device,
        Dictionary<string, CompatibilityEvaluation> evaluations,
        IReadOnlyList<Candidate> ordered,
        string? targetId,
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

            details["targetDevice"] = device.Product is { } p ? new { p.Id, p.Name, p.Categories } : null;
            details["targetDeviceMethod"] = device.Method;
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
                .Where(c => c.Compatibility.Status == CompatibilityStatus.Incompatible
                    || c.Signals.ConceptMatch == ConceptMatch.OutOfConcept
                    || c.Product.Id == targetId)
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
            notes.Add("Order: unflagged first, then OutOfConcept and the target device itself, then Incompatible; each group keeps its fused order. Nothing is removed.");

            if (device.Product is null)
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

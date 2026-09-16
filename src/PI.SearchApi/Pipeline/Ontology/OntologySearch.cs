using System.Diagnostics;
using PI.SearchApi.Contracts;
using PI.SearchApi.Pipeline.Hybrid;

namespace PI.SearchApi.Pipeline.Ontology;

// Stage 5 — Ontology: SKOS concepts, expansion and domain rules
//
// What:     Understands the query first — which device the shopper owns, which categories they
//           want — then expands the wanted concepts into synonyms and narrower concepts, re-runs
//           hybrid search with that better input, classifies each candidate (in or out of concept)
//           and checks class-level rules against the target device — or, with no device, against the
//           requirements the query states ("65W USB-C"), listing which catalog devices each product
//           fits. Flagged items move down, never out.
// Strength: Answers "how is it related and constrained?": a device name becomes context instead
//           of search text, recall improves through synonyms, precision through classification,
//           and correctness through rules with reasons.
// Failure:  Knows only what the ontology and catalog state. Unlisted synonyms, partial device names
//           ("my Aerobook"), unmodelled product types or rules beyond equals/≥/≤/in are invisible
//           to it — and it still needs retrieval to find the candidates in the first place.
// Decision: docs/decisions/0013-domain-ontology-and-compatibility.md
public sealed class OntologySearch(
    IOntology ontology,
    LabelMatcher labelMatcher,
    QueryExpander expander,
    IHybridSearch hybridSearch,
    ConceptClassifier classifier,
    TargetDeviceResolver deviceResolver,
    CompatibilityEvaluator evaluator,
    QueryRequirementExtractor requirementExtractor,
    DeviceFitFinder fitFinder,
    ProductLookup products) : IOntologySearch
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

        //    What the query itself asks for ("65W USB-C charger"), for when there is no device to check against.
        //    With a device, it's still shown, and any requirement the device contradicts is reported.
        var requirements = requirementExtractor.Extract(understanding);
        var conflicts = device.Product is { } owned && requirements.Any
            ? evaluator.ConflictsWithDevice(requirements.Requirements, owned)
            : [];
        var understandStep = UnderstandStep(understanding, device, requirements, conflicts, PipelineTelemetry.ElapsedMs(start));

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

        // 5. Constrain (optional): against the device resolved in step 1; without one, against the query's
        //    requirements; and without a device, also list the catalog devices each product fits.
        start = Stopwatch.GetTimestamp();
        var targetId = device.Product?.Id;
        var evaluations = options.ApplyConstraints
            ? retrieved.Candidates.ToDictionary(c => c.Product.Id, c => Evaluate(c.Product, device.Product, requirements))
            : [];

        IReadOnlyList<ProductSummary> catalogDevices = options.ApplyConstraints && device.Product is null
            ? await products.GetDevicesAsync(ct)
            : [];

        var evaluated = retrieved.Candidates.Select(candidate =>
        {
            var id = candidate.Product.Id;
            var compatibility = evaluations.TryGetValue(id, out var evaluation) ? evaluation.Result : CompatibilityResult.NotEvaluated;

            // "Fits 6 of 13 laptops": context only, so it never changes the status or the order.
            if (catalogDevices.Count > 0 && fitFinder.FitsFor(candidate.Product, catalogDevices) is { } fits)
            {
                compatibility = compatibility with { Fits = fits };
            }

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

        var constrainStep = ConstrainStep(options, device, requirements, evaluations, ordered, targetId, PipelineTelemetry.ElapsedMs(start));

        return new OntologySearchResult(
            new StageResult(ordered, [understandStep, expandStep, .. retrieved.Trace, classifyStep, constrainStep]),
            device,
            understanding,
            [.. evaluations.Values.SelectMany(e => e.Checks)])
        {
            Requirements = device.Product is null ? requirements : QueryRequirements.None,
        };
    }

    // The device decides when there is one; otherwise stated requirements do; otherwise the rules can't be decided.
    private CompatibilityEvaluation Evaluate(ProductSummary candidate, ProductSummary? device, QueryRequirements requirements) =>
        device is null && requirements.Any
            ? evaluator.EvaluateAgainstRequirements(candidate, requirements.Requirements)
            : evaluator.Evaluate(candidate, device);

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

    private static TraceStep UnderstandStep(
        QueryUnderstanding understanding,
        TargetDevice device,
        QueryRequirements requirements,
        IReadOnlyList<string> conflicts,
        double durationMs)
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

        if (requirements.Any)
        {
            notes.Add(device.Product is null
                ? "Values and quantities the rules compare (a connector, a wattage) are requirements: with no target device, candidates are checked against them."
                : "The query states requirements, but a target device was given: its specs decide, and the requirements are only shown.");
        }

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
                ["requirements"] = requirements.Requirements.Select(r => new
                {
                    phrase = r.Phrase,
                    accessorySpec = r.AccessorySpec,
                    @operator = r.Operator,
                    value = r.Value,
                    scheme = r.ValueSchemeNotation,
                }).ToList(),
                ["requirementsApplied"] = requirements.Any && device.Product is null,
                ["unusedValuePhrases"] = requirements.UnusedPhrases,
                ["requirementConflicts"] = conflicts,
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
        QueryRequirements requirements,
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
            details["checkedAgainst"] = device.Product is not null ? "device" : requirements.Any ? "query" : "none";
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
                source = c.Source.ToString(),
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
            details["fits"] = ordered
                .Where(c => c.Compatibility.Fits is not null)
                .Select(c => new
                {
                    id = c.Product.Id,
                    fits = c.Compatibility.Fits!.Select(f => new { deviceType = f.DeviceType, fitting = f.Devices.Count, total = f.Total }).ToList(),
                })
                .ToList();

            notes.Add("Rules are data from domain-ontology.ttl, found with rules.rq. The evaluator runs their checks and never names a rule.");
            notes.Add("Order: unflagged first, then OutOfConcept and the target device itself, then Incompatible; each group keeps its fused order. Nothing is removed.");

            if (device.Product is null && requirements.Any)
            {
                notes.Add("No target device: each check ran against what the query asked for. A check the query says nothing about stays Unknown.");
            }
            else if (device.Product is null)
            {
                notes.Add("No target device and no stated requirement, so any product a rule applies to is Unknown rather than Compatible.");
            }

            if (device.Product is null)
            {
                notes.Add("\"Fits\" runs the same rules against every catalog device of the type they name. It's context: it never changes a status or the order.");
            }
        }

        return new TraceStep
        {
            Stage = "ontology",
            Title = device.Product is null && requirements.Any
                ? "Constrain: domain rules vs what the query asked for"
                : "Constrain: domain rules vs the target device",
            DurationMs = durationMs,
            Details = details,
            Notes = notes,
        };
    }
}

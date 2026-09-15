using PI.SearchApi.Contracts;
using PI.SearchApi.Pipeline.Ontology;

namespace PI.SearchApi.Pipeline.Rag;

// Stage 6, step 1 — Evidence: choose what the model may use
//
// What:     Picks a bounded evidence set from Stage 5's ranked results: the target device, up to 5 Compatible,
//           3 Incompatible (with reasons), 2 Unknown and 5 not-checked products, in Stage 5's order, leaving
//           out products that aren't in the wanted concept. Adds the matched concepts' labels and definitions,
//           and the rules that were checked.
// Strength: Context construction is most of RAG. The model gets the verdicts and the *negative* evidence (why
//           the near miss fails), so it can warn instead of recommending; and it's small enough for a local model.
// Failure:  A bound is a cut-off. A relevant product beyond the limits is invisible to the model, however good;
//           the trace lists exactly what was included, and why, so the omission can be seen.
// Decision: docs/adr/0016-rag-grounding-and-citations.md
public sealed class EvidenceSetBuilder(IOntology ontology)
{
    /// <param name="ranked">Stage 5's candidates, in its final order.</param>
    /// <param name="matchedConcepts">Every taxonomy concept the query matched: wanted and context concepts.</param>
    /// <param name="checks">Every rule check Stage 5 ran, so the rules can be quoted in the domain's own words.</param>
    public EvidenceSet Build(
        IReadOnlyList<Candidate> ranked,
        ProductSummary? targetDevice,
        IReadOnlyList<string> matchedConcepts,
        IReadOnlyList<CheckOutcome> checks)
    {
        var items = new List<EvidenceItem>();

        if (targetDevice is not null)
        {
            items.Add(new EvidenceItem(
                targetDevice, null, CompatibilityResult.NotEvaluated, null, null, EvidenceRole.TargetDevice,
                "The device you own: the rules were checked against it."));
        }

        var countByRole = new Dictionary<EvidenceRole, int>();

        for (var index = 0; index < ranked.Count; index++)
        {
            var candidate = ranked[index];
            var rank = index + 1;

            // Stage 5 has already said these aren't what the shopper asked for (a phone battery for a drill).
            if (candidate.Product.Id == targetDevice?.Id || candidate.Signals.ConceptMatch == ConceptMatch.OutOfConcept)
            {
                continue;
            }

            var role = RoleFor(candidate.Compatibility.Status);
            var taken = countByRole.GetValueOrDefault(role);

            if (taken >= EvidenceLimits.For(role))
            {
                continue;
            }

            countByRole[role] = taken + 1;
            items.Add(new EvidenceItem(
                candidate.Product, null, candidate.Compatibility, candidate.Signals.ConceptMatch, rank, role,
                WhyIncluded(role, rank)));
        }

        var includedIds = items.Select(i => i.Product.Id).ToHashSet();
        var rules = checks
            .Where(check => includedIds.Contains(check.CandidateId))
            .GroupBy(check => check.Rule)
            .Select(group => new EvidenceRule(group.Key, [.. group.Select(check => check.Definition).Distinct()]))
            .ToList();

        return new EvidenceSet(items, ConceptsFor(matchedConcepts), rules);
    }

    private IReadOnlyList<EvidenceConcept> ConceptsFor(IReadOnlyList<string> matchedConcepts)
    {
        var concepts = new List<EvidenceConcept>();

        foreach (var notation in matchedConcepts.Distinct())
        {
            if (!ontology.TryGetConcept(notation, out var concept))
            {
                continue;
            }

            // English alternative labels only: the answer is written in English, and hidden labels are misspellings.
            var altLabels = ontology.Labels
                .Where(l => l.ConceptNotation == notation && l.IsTaxonomyConcept && l.Kind == LabelKind.Alternative && l.Language == "en")
                .Select(l => l.Label)
                .Distinct()
                .ToList();

            concepts.Add(new EvidenceConcept(
                notation,
                concept.PrefLabels.GetValueOrDefault("en") ?? notation,
                altLabels,
                concept.Definition));
        }

        return concepts;
    }

    private static EvidenceRole RoleFor(CompatibilityStatus status) => status switch
    {
        CompatibilityStatus.Compatible => EvidenceRole.Compatible,
        CompatibilityStatus.Incompatible => EvidenceRole.Incompatible,
        CompatibilityStatus.Unknown => EvidenceRole.Unknown,
        _ => EvidenceRole.NotChecked,
    };

    private static string WhyIncluded(EvidenceRole role, int rank) => role switch
    {
        EvidenceRole.Compatible => $"#{rank} in Stage 5's order, and every rule check passed: a product to recommend.",
        EvidenceRole.Incompatible => $"#{rank} in Stage 5's order, but a rule check failed: included with its reasons, so the answer can warn about it.",
        EvidenceRole.Unknown => $"#{rank} in Stage 5's order; a rule applies but couldn't be decided.",
        _ => $"#{rank} in Stage 5's order; no rule was checked for it.",
    };
}

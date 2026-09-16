using System.Text.RegularExpressions;
using PI.SearchApi.Pipeline.Rag;

namespace PI.SearchApi.Pipeline.Pedagogy;

// Stage 7, step 3 — Validate the finished explanation
//
// What:     Always checks citations against the evidence set. With pedagogy on, also checks the teaching
//           structure: all five headings in order; Decision recommends exactly one Compatible product; Near miss
//           uses an Incompatible product as the counter-example; and each bold concept is a word the ontology
//           or the rules actually use (a heuristic). The baseline has no structure, so those checks don't apply.
// Strength: A visible structure is a contract. The explanation streams as ordinary text, yet afterwards the
//           system can say whether it taught the right thing: the right product, the right counter-example.
// Failure:  It checks shape and citations, not understanding. An explanation can pass every check and still be
//           muddled, and the concept heuristic matches words, not meaning.
// Decision: docs/decisions/0017-pedagogy-engine.md
public static partial class ExplanationValidator
{
    public static ExplanationValidation Validate(string markdown, EvidenceSet evidence, bool applyPedagogy, bool answerFoundInsufficientEvidence)
    {
        var citations = CitationValidator.Validate(markdown, evidence.ProductIds);
        var warnings = citations.UnknownIds.Select(id => $"Cited {id}, which was not in the evidence.").ToList();
        var checks = new List<ValidationCheck>
        {
            new(
                "Citations are in the evidence",
                citations.UnknownIds.Count == 0,
                citations.UnknownIds.Count == 0
                    ? $"All {citations.Citations.Count} cited IDs were in the evidence."
                    : $"Not in the evidence: {string.Join(", ", citations.UnknownIds)}."),
        };

        if (!applyPedagogy)
        {
            checks.Add(new ValidationCheck(
                "Structure checks",
                Passed: true,
                "Not applied (baseline prompt): the baseline asks for free-form text, so there are no headings, decision or near miss to check."));

            return new ExplanationValidation(citations.Citations, citations.UnknownIds, warnings, checks, Structure: null);
        }

        var sections = ExplanationHeadingParser.Parse(markdown);

        Add(checks, warnings, HeadingsCheck(sections));
        Add(checks, warnings, DecisionCheck(sections, evidence, answerFoundInsufficientEvidence));
        Add(checks, warnings, NearMissCheck(sections, evidence));
        Add(checks, warnings, ConceptsCheck(sections, evidence));

        return new ExplanationValidation(citations.Citations, citations.UnknownIds, warnings, checks, ExplanationHeadingParser.ToStructure(sections));
    }

    private static void Add(List<ValidationCheck> checks, List<string> warnings, (ValidationCheck Check, IReadOnlyList<string> Warnings) result)
    {
        checks.Add(result.Check);
        warnings.AddRange(result.Warnings);
    }

    private static (ValidationCheck, IReadOnlyList<string>) HeadingsCheck(IReadOnlyList<ExplanationSection> sections)
    {
        var found = sections.Where(s => s.IsKnown).Select(s => s.Heading).Distinct().ToList();
        var missing = ExplanationHeadingParser.Headings.Except(found).ToList();
        var expectedOrder = ExplanationHeadingParser.Headings.Where(found.Contains).ToList();
        var isInOrder = found.SequenceEqual(expectedOrder);
        var warnings = new List<string>();

        if (missing.Count > 0)
        {
            warnings.Add($"Missing heading(s): {string.Join(", ", missing)}.");
        }

        if (!isInOrder)
        {
            warnings.Add($"Headings are out of order: {string.Join(" → ", found)} (expected {string.Join(" → ", ExplanationHeadingParser.Headings)}).");
        }

        return (new ValidationCheck(
            "All five headings, in order",
            warnings.Count == 0,
            warnings.Count == 0 ? string.Join(" → ", found) : string.Join(" ", warnings)), warnings);
    }

    private static (ValidationCheck, IReadOnlyList<string>) DecisionCheck(IReadOnlyList<ExplanationSection> sections, EvidenceSet evidence, bool answerFoundInsufficientEvidence)
    {
        const string name = "Decision recommends exactly one Compatible product";

        if (ExplanationHeadingParser.Body(sections, ExplanationHeadingParser.Decision) is not { } body)
        {
            return (new ValidationCheck(name, false, "There is no Decision section to check."), []);
        }

        // "for your laptop [PROD-0001]" cites the shopper's own device as context, not as a choice.
        var cited = CitedExceptTargetDevice(body, evidence);

        if (answerFoundInsufficientEvidence)
        {
            // The answer said the evidence didn't answer the question, so the right decision is "nothing suitable".
            return cited.Count == 0
                ? (new ValidationCheck(name, true, "The answer found insufficient evidence, and Decision recommends no product."), [])
                : (new ValidationCheck(name, false, $"Decision cites {string.Join(", ", cited)} although the answer found insufficient evidence."),
                    [$"Decision recommends {string.Join(", ", cited)} although the answer said the evidence was insufficient."]);
        }

        string? problem = cited.Count switch
        {
            0 => "Decision cites no product.",
            > 1 => $"Decision cites {cited.Count} products ({string.Join(", ", cited)}); it should choose exactly one.",
            _ when evidence.Find(cited[0]) is { Role: not EvidenceRole.Compatible } item =>
                $"Decision recommends {cited[0]}, which is {RoleText(item.Role)}, not Compatible.",
            _ => null,
        };

        return problem is null
            ? (new ValidationCheck(name, true, $"Decision recommends {cited[0]}, which is Compatible."), [])
            : (new ValidationCheck(name, false, problem), [problem]);
    }

    private static (ValidationCheck, IReadOnlyList<string>) NearMissCheck(IReadOnlyList<ExplanationSection> sections, EvidenceSet evidence)
    {
        const string name = "Near miss is an Incompatible product";

        if (ExplanationHeadingParser.Body(sections, ExplanationHeadingParser.NearMiss) is not { } body)
        {
            return (new ValidationCheck(name, false, "There is no Near miss section to check."), []);
        }

        var cited = CitedExceptTargetDevice(body, evidence);
        var hasIncompatibleEvidence = evidence.Items.Any(i => i.Role == EvidenceRole.Incompatible);

        if (!hasIncompatibleEvidence)
        {
            return cited.Count == 0
                ? (new ValidationCheck(name, true, "The evidence has no Incompatible product, and Near miss cites none."), [])
                : (new ValidationCheck(name, false, $"The evidence has no Incompatible product, but Near miss cites {string.Join(", ", cited)}."),
                    [$"Near miss cites {string.Join(", ", cited)}, but the evidence has no Incompatible product: it should say \"None\"."]);
        }

        var notIncompatible = cited.Where(id => evidence.Find(id)?.Role != EvidenceRole.Incompatible).ToList();
        string? problem = cited.Count == 0
            ? "Near miss cites no product, although the evidence has an Incompatible one to contrast with."
            : notIncompatible.Count > 0
                ? $"Near miss cites {string.Join(", ", notIncompatible)}, which is not Incompatible: the counter-example should be a product that doesn't fit."
                : null;

        return problem is null
            ? (new ValidationCheck(name, true, $"Near miss contrasts with {string.Join(", ", cited)}, which is Incompatible."), [])
            : (new ValidationCheck(name, false, problem), [problem]);
    }

    // A heuristic: each bold concept should share a word with a concept label, a rule definition or a spec term.
    private static (ValidationCheck, IReadOnlyList<string>) ConceptsCheck(IReadOnlyList<ExplanationSection> sections, EvidenceSet evidence)
    {
        const string name = "Concepts come from the ontology";
        var structure = ExplanationHeadingParser.ToStructure(sections);

        if (structure.Concepts.Count == 0)
        {
            const string problem = "Concepts has no bold terms, so the concepts it explains can't be checked.";
            return (new ValidationCheck(name, false, problem, IsHeuristic: true), [$"Heuristic: {problem}"]);
        }

        var vocabulary = OntologyWords(evidence);
        var unknown = structure.Concepts.Where(term => !Words(term).Any(vocabulary.Contains)).ToList();

        return unknown.Count == 0
            ? (new ValidationCheck(name, true, $"Every concept ({string.Join(", ", structure.Concepts)}) shares a word with a concept label, rule or spec term.", IsHeuristic: true), [])
            : (new ValidationCheck(name, false, $"Not found in the ontology's words: {string.Join(", ", unknown)}.", IsHeuristic: true),
                [.. unknown.Select(term => $"Heuristic: the concept \"{term}\" is not from the ontology (no matching label, rule or spec term).")]);
    }

    private static List<string> CitedExceptTargetDevice(string body, EvidenceSet evidence) =>
        [.. CitationValidator.Extract(body).Where(id => evidence.Find(id)?.Role != EvidenceRole.TargetDevice)];

    private static HashSet<string> OntologyWords(EvidenceSet evidence)
    {
        // The products' compatibility reasons quote the rule definitions too. They matter when there is no target
        // device: no check runs, so the evidence has no rules, but each Unknown reason still names the rule.
        var texts = evidence.Concepts.SelectMany(c => c.AltLabels.Append(c.PrefLabel))
            .Concat(evidence.Rules.SelectMany(r => r.Definitions))
            .Concat(evidence.Rules.SelectMany(r => r.SpecTerms).Select(PedagogyPromptBuilder.HumaniseSpecTerm))
            .Concat(evidence.Items.SelectMany(i => i.Compatibility.Reasons))
            // A product's own spec names (capacityAh, formFactor) are the catalogue's words, not invented ones.
            .Concat(evidence.Items.SelectMany(i => i.Product.Specs.Keys).Select(PedagogyPromptBuilder.HumaniseSpecTerm));

        return [.. texts.SelectMany(Words)];
    }

    // Lower-case words of three letters or more, with a simple plural folded: "Connectors" → "connector".
    private static IEnumerable<string> Words(string text) =>
        WordPattern().Matches(text.ToLowerInvariant())
            .Select(m => m.Value)
            .Where(w => w.Length >= 3)
            .Select(w => w.Length > 3 && w.EndsWith('s') ? w[..^1] : w);

    private static string RoleText(EvidenceRole role) => role switch
    {
        EvidenceRole.Incompatible => "Incompatible",
        EvidenceRole.Unknown => "Unknown",
        EvidenceRole.NotChecked => "not checked",
        EvidenceRole.TargetDevice => "the shopper's own device",
        _ => role.ToString(),
    };

    [GeneratedRegex(@"[a-z0-9]+")]
    private static partial Regex WordPattern();
}

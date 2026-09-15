using System.Text.RegularExpressions;

namespace PI.SearchApi.Pipeline.Rag;

// Stage 6, step 4 — Validate the finished answer
//
// What:     Runs after the last token, on the complete text: every [PROD-…] citation must be in the evidence set;
//           an answer must cite something unless its first line is the INSUFFICIENT_EVIDENCE sentinel; and a
//           sentence citing an Incompatible product should contain warning language (a heuristic).
// Strength: LLM output is untrusted. Checking it against the evidence turns "sounds right" into "can be checked",
//           and the failures stay visible as warnings instead of being silently stripped or regenerated.
// Failure:  It checks form, not truth: a correctly cited sentence can still misstate a spec, and the warning-
//           language heuristic can be fooled either way ("not only compatible…"). The trace labels it a heuristic.
// Decision: docs/adr/0016-rag-grounding-and-citations.md
public static partial class AnswerValidator
{
    /// <summary>The exact first line the prompt asks for when the evidence doesn't answer the question.</summary>
    public const string InsufficientEvidenceSentinel = "INSUFFICIENT_EVIDENCE";

    public static AnswerValidation Validate(string markdown, EvidenceSet evidence)
    {
        var insufficientEvidence = StartsWithSentinel(markdown);
        var citations = CitationValidator.Validate(markdown, evidence.ProductIds);
        var warnings = new List<string>();
        var checks = new List<ValidationCheck>
        {
            new(
                "Insufficient-evidence sentinel",
                Passed: true,
                insufficientEvidence
                    ? $"The first line is {InsufficientEvidenceSentinel}: the model says the evidence doesn't answer the question."
                    : "Not present: the model answered from the evidence."),
        };

        foreach (var id in citations.UnknownIds)
        {
            warnings.Add($"Cited {id}, which was not in the evidence.");
        }

        checks.Add(new ValidationCheck(
            "Citations are in the evidence",
            citations.UnknownIds.Count == 0,
            citations.UnknownIds.Count == 0
                ? $"All {citations.Citations.Count} cited IDs were in the evidence."
                : $"Not in the evidence: {string.Join(", ", citations.UnknownIds)}."));

        if (!insufficientEvidence)
        {
            var hasCitations = citations.Citations.Count > 0;
            if (!hasCitations)
            {
                warnings.Add("The answer cites no products, so none of its claims can be checked.");
            }

            checks.Add(new ValidationCheck(
                "Product claims are cited",
                hasCitations,
                hasCitations ? $"Cites {string.Join(", ", citations.Citations)}." : "No [PROD-…] citations found."));
        }

        var unwarned = SentencesRecommendingIncompatible(markdown, evidence);
        foreach (var (id, sentence) in unwarned)
        {
            warnings.Add($"Heuristic: a sentence cites {id}, which is Incompatible, without warning language: \"{sentence}\"");
        }

        checks.Add(new ValidationCheck(
            "Incompatible products are only mentioned as warnings",
            unwarned.Count == 0,
            unwarned.Count == 0
                ? "Every sentence citing an Incompatible product contains warning language (avoid, not, won't, wrong, …)."
                : $"{unwarned.Count} sentence(s) cite an Incompatible product without warning language.",
            IsHeuristic: true));

        return new AnswerValidation(citations.Citations, citations.UnknownIds, insufficientEvidence, warnings, checks);
    }

    public static bool StartsWithSentinel(string markdown) =>
        markdown.TrimStart().Split('\n', 2)[0].Trim().Trim('*', '_', '`').Trim() == InsufficientEvidenceSentinel;

    /// <summary>
    /// Sentences that cite an Incompatible product but contain none of the warning words. A heuristic: it reads
    /// wording, so "avoid" in a sentence is taken as a warning even when the model meant something else.
    /// </summary>
    public static IReadOnlyList<(string ProductId, string Sentence)> SentencesRecommendingIncompatible(string markdown, EvidenceSet evidence)
    {
        var found = new List<(string, string)>();

        foreach (var (sentence, isUnderWarningLeadIn) in Sentences(markdown))
        {
            foreach (var id in CitationValidator.Extract(sentence))
            {
                if (evidence.Find(id)?.Role == EvidenceRole.Incompatible && !isUnderWarningLeadIn && !WarningLanguage().IsMatch(sentence))
                {
                    found.Add((id, sentence));
                }
            }
        }

        return found;
    }

    // Lines first (bullets are sentences too), then sentence ends within a line. A bullet list introduced by a warning
    // line ("Avoid these incompatible options:") is a list of warnings, even when a bullet only states the reason.
    private static IEnumerable<(string Sentence, bool IsUnderWarningLeadIn)> Sentences(string markdown)
    {
        var isUnderWarningLeadIn = false;

        foreach (var rawLine in markdown.Split('\n'))
        {
            var line = rawLine.Trim();
            var isBullet = BulletMarker().IsMatch(line);

            if (!isBullet && line.Length > 0)
            {
                isUnderWarningLeadIn = line.EndsWith(':') && WarningLanguage().IsMatch(line);
            }

            var text = BulletMarker().Replace(line, "");

            foreach (var sentence in SentenceEnd().Split(text).Select(s => s.Trim()).Where(s => s.Length > 0))
            {
                yield return (sentence, isBullet && isUnderWarningLeadIn);
            }
        }
    }

    [GeneratedRegex(@"^([-*+]|\d+\.)\s+")]
    private static partial Regex BulletMarker();

    [GeneratedRegex(@"(?<=[.!?])\s+")]
    private static partial Regex SentenceEnd();

    // Plain warning words, plus the ways a model states a failed rule check ("fails", "insufficient", "below the required").
    [GeneratedRegex(@"\b(avoid|not|never|no longer|wrong|incorrect|incompatible|mismatch|fails?|insufficient|below|short of|lacks?|won't|wouldn't|doesn't|don't|isn't|can't|cannot|too little|too low|too weak|instead|unlike|skip|warning|beware)\b|n't\b", RegexOptions.IgnoreCase)]
    private static partial Regex WarningLanguage();
}

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using PI.SearchApi.Pipeline.Rag;

namespace PI.SearchApi.Pipeline.Pedagogy;

// Stage 7, step 1 — Choose the prompt and the words
//
// What:     Picks the system prompt by options.applyPedagogy: pedagogy-system.md (teaching principles, five fixed
//           headings and the active audience's guidance) or pedagogy-baseline.md (a fair, plain prompt naming the
//           audience). Renders one shared user message, pedagogy-user.md, with the question, the checked answer,
//           the evidence and each concept's words for the audience, taken from the ontology's SKOS labels.
// Strength: A fair experiment changes one thing. Same facts, same audience and same words either way; only the
//           teaching design differs, and the trace shows both messages in full.
// Failure:  The words come from the labels the ontology has. A concept with no everyday altLabel gives a novice
//           only its preferred label, and the model may still reach for jargon.
// Decision: docs/adr/0017-pedagogy-engine.md
public sealed partial class PedagogyPromptBuilder(PromptLibrary prompts)
{
    public const string SystemPromptFile = "pedagogy-system.md";
    public const string BaselinePromptFile = "pedagogy-baseline.md";
    public const string UserPromptFile = "pedagogy-user.md";
    public const string AudiencesFile = "pedagogy-audiences.md";

    public PedagogyPrompt Build(string question, string audience, bool applyPedagogy, EvidenceSet evidence, string answerMarkdown)
    {
        var wordsOffered = WordsFor(evidence, audience);
        string? audienceGuidance = null;
        string systemPromptFile;
        string systemPrompt;

        if (applyPedagogy)
        {
            systemPromptFile = SystemPromptFile;
            audienceGuidance = AudienceGuidance(audience);
            systemPrompt = PromptTemplate.Render(prompts.Get(SystemPromptFile), new Dictionary<string, string>
            {
                ["audience"] = audience,
                ["audienceGuidance"] = audienceGuidance,
            });
        }
        else
        {
            systemPromptFile = BaselinePromptFile;
            systemPrompt = PromptTemplate.Render(prompts.Get(BaselinePromptFile), new Dictionary<string, string> { ["audience"] = audience });
        }

        // The user message doesn't depend on applyPedagogy: that is what makes the comparison fair.
        var userPrompt = PromptTemplate.Render(prompts.Get(UserPromptFile), new Dictionary<string, string>
        {
            ["question"] = question,
            ["audience"] = audience,
            ["answer"] = answerMarkdown.Trim(),
            ["targetDevice"] = EvidenceFormatter.FormatTargetDevice(evidence),
            ["products"] = EvidenceFormatter.FormatProducts(evidence),
            ["concepts"] = FormatConcepts(evidence, wordsOffered, audience),
            ["rules"] = EvidenceFormatter.FormatRules(evidence),
        });

        return new PedagogyPrompt(applyPedagogy, audience, systemPromptFile, systemPrompt, userPrompt, audienceGuidance, wordsOffered);
    }

    /// <summary>
    /// The words each concept is offered for an audience (ADR-0017): a novice gets the everyday altLabels first, then the
    /// preferred label to give once; an enthusiast the preferred label; an expert the preferred label. Hidden labels
    /// (misspellings) are never in the evidence set, so they can't be offered.
    /// </summary>
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> WordsFor(EvidenceSet evidence, string audience) =>
        evidence.Concepts.ToDictionary(
            concept => concept.Notation,
            concept => (IReadOnlyList<string>)(audience == "novice"
                ? [.. concept.AltLabels, concept.PrefLabel]
                : [concept.PrefLabel]));

    /// <summary>A spec name as words: "minChargerWattageW" → "min charger wattage", "chargingPort" → "charging port".</summary>
    public static string HumaniseSpecTerm(string specName)
    {
        var withoutUnit = UnitSuffix().Replace(specName, "");
        return CamelCaseBoundary().Replace(withoutUnit, " ").ToLowerInvariant();
    }

    /// <summary>The active audience's section of <c>pedagogy-audiences.md</c> (a <c>## audience</c> heading and its text).</summary>
    public string AudienceGuidance(string audience)
    {
        var file = prompts.Get(AudiencesFile);
        var match = Regex.Match(file, $@"^##\s+{Regex.Escape(audience)}\s*\n(?<text>.*?)(?=^##\s|\z)", RegexOptions.Multiline | RegexOptions.Singleline);

        return match.Success
            ? match.Groups["text"].Value.Trim()
            : throw new InvalidOperationException($"{AudiencesFile} has no '## {audience}' section.");
    }

    private static string FormatConcepts(EvidenceSet evidence, IReadOnlyDictionary<string, IReadOnlyList<string>> wordsOffered, string audience)
    {
        if (evidence.Concepts.Count == 0)
        {
            return "None: the question didn't match a concept in the ontology.";
        }

        var text = new StringBuilder();

        foreach (var concept in evidence.Concepts)
        {
            var words = wordsOffered[concept.Notation];
            var line = audience == "novice" && concept.AltLabels.Count > 0
                ? $"- {concept.PrefLabel}: everyday words: {string.Join(", ", concept.AltLabels)}; proper name to give once: {concept.PrefLabel}"
                : $"- {string.Join(", ", words)}";

            text.Append(line);

            if (concept.Definition is { } definition)
            {
                text.Append(CultureInfo.InvariantCulture, $". Definition: {definition}");
            }

            text.Append('\n');
        }

        if (audience == "expert")
        {
            var specTerms = evidence.Rules.SelectMany(r => r.SpecTerms).Select(HumaniseSpecTerm).Distinct().ToList();

            if (specTerms.Count > 0)
            {
                text.Append(CultureInfo.InvariantCulture, $"- Spec terms from the rules: {string.Join(", ", specTerms)}\n");
            }
        }

        return text.ToString().TrimEnd();
    }

    [GeneratedRegex("(?<=[a-z0-9])(W|V|Ah|Gb|Mah|In|Kg)$")]
    private static partial Regex UnitSuffix();

    [GeneratedRegex("(?<=[a-z0-9])(?=[A-Z])")]
    private static partial Regex CamelCaseBoundary();
}

using System.Text.RegularExpressions;
using PI.SearchApi.Contracts;
using PI.SearchApi.Pipeline.Rag;

namespace PI.SearchApi.Pipeline.Pedagogy;

/// <summary>
/// Splits an explanation into sections at its headings and reads the five fixed ones into an <see cref="ExplanationStructure"/>
/// (ADR-0017). Tolerant about markup, because small models vary it: <c>## Decision</c>, <c>### Decision</c> and a line that
/// is only <c>**Decision**</c> all count. Whether the headings are all there, and in order, is the validator's job.
/// </summary>
public static partial class ExplanationHeadingParser
{
    public const string Decision = "Decision";
    public const string Concepts = "Concepts";
    public const string NearMiss = "Near miss";
    public const string RuleOfThumb = "Rule of thumb";
    public const string NextStep = "Next step";

    /// <summary>The five headings, in the order the prompt asks for them.</summary>
    public static readonly IReadOnlyList<string> Headings = [Decision, Concepts, NearMiss, RuleOfThumb, NextStep];

    /// <summary>The sections in the order they appear. Text before the first heading is a section with an empty heading.</summary>
    public static IReadOnlyList<ExplanationSection> Parse(string markdown)
    {
        var sections = new List<ExplanationSection>();
        string? heading = null;
        var isKnown = false;
        var body = new List<string>();

        void Close()
        {
            var text = string.Join('\n', body).Trim();

            if (heading is not null || text.Length > 0)
            {
                sections.Add(new ExplanationSection(heading ?? "", isKnown, text));
            }

            body.Clear();
        }

        foreach (var line in markdown.ReplaceLineEndings("\n").Split('\n'))
        {
            var match = HeadingLine().Match(line);
            var name = match.Success ? Normalise(match.Groups["heading"].Value) : null;
            var canonical = name is null ? null : Headings.FirstOrDefault(h => string.Equals(h, name, StringComparison.OrdinalIgnoreCase));

            // A bold-only line is a heading only when it names one of the five; any "#" line is a heading.
            if (match.Success && (canonical is not null || match.Groups["hashes"].Success))
            {
                Close();
                heading = canonical ?? name;
                isKnown = canonical is not null;
            }
            else
            {
                body.Add(line);
            }
        }

        Close();
        return sections;
    }

    /// <summary>The body of a canonical section, or null when the explanation doesn't have it.</summary>
    public static string? Body(IReadOnlyList<ExplanationSection> sections, string heading) =>
        sections.FirstOrDefault(s => s.IsKnown && s.Heading == heading)?.Body;

    public static ExplanationStructure ToStructure(IReadOnlyList<ExplanationSection> sections)
    {
        // A concept is the bold term that starts a line or bullet ("- **Connector**: …"). Bold words later in the sentence
        // ("uses a **USB-C** port") are emphasis, not concepts.
        var concepts = Body(sections, Concepts) is { } conceptsBody
            ? LeadingBoldTerm().Matches(conceptsBody).Select(m => m.Groups["term"].Value.Trim().TrimEnd(':').Trim()).Where(t => t.Length > 0).Distinct().ToList()
            : [];

        return new ExplanationStructure
        {
            Decision = new ExplanationProduct(FirstCitation(Body(sections, Decision))),
            Concepts = concepts,
            NearMiss = new ExplanationProduct(FirstCitation(Body(sections, NearMiss))),
            RuleOfThumb = NullIfEmpty(Body(sections, RuleOfThumb)),
            NextStep = NullIfEmpty(Body(sections, NextStep)),
        };
    }

    private static string? FirstCitation(string? body) => body is null ? null : CitationValidator.Extract(body).FirstOrDefault();

    private static string? NullIfEmpty(string? text) => string.IsNullOrWhiteSpace(text) ? null : text;

    private static string Normalise(string heading) => WhiteSpace().Replace(heading.Trim().TrimEnd(':').Trim(), " ");

    [GeneratedRegex(@"^\s*(?:(?<hashes>#{1,6})\s*(?<heading>.+?)\s*#*|\*\*(?<heading>[^*]+?)\*\*:?)\s*$")]
    private static partial Regex HeadingLine();

    [GeneratedRegex(@"^\s*(?:[-*+]|\d+\.)?\s*\*\*(?<term>[^*]+?)\*\*", RegexOptions.Multiline)]
    private static partial Regex LeadingBoldTerm();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhiteSpace();
}

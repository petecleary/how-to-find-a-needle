using System.Text.RegularExpressions;

namespace PI.SearchApi.Pipeline.Rag;

/// <summary>
/// Finds <c>[PROD-nnnn]</c> citations and checks each one against the evidence set (ADR-0016). "Cite or it didn't
/// happen": a cited ID the model wasn't given means it invented or remembered a product, and that must be visible.
/// Shared by Stage 6's answer and Stage 7's explanation.
/// </summary>
public static partial class CitationValidator
{
    /// <summary>
    /// Every product ID inside square brackets, once each, in order of first appearance. Models sometimes group
    /// citations ("[PROD-0012, PROD-0011]"), so every ID inside a bracket counts.
    /// </summary>
    public static IReadOnlyList<string> Extract(string text) =>
    [
        .. Bracketed().Matches(text)
            .SelectMany(bracket => ProductId().Matches(bracket.Value).Select(id => id.Value))
            .Distinct(),
    ];

    public static CitationCheck Validate(string text, IReadOnlyCollection<string> evidenceIds)
    {
        var citations = Extract(text);
        var unknown = citations.Where(id => !evidenceIds.Contains(id)).ToList();

        return new CitationCheck(citations, unknown);
    }

    [GeneratedRegex(@"\[[^\[\]]*\]")]
    private static partial Regex Bracketed();

    [GeneratedRegex(@"PROD-\d{4}")]
    private static partial Regex ProductId();
}

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace PI.SearchApi.Pipeline.Ontology;

/// <summary>
/// Splits text into tokens and folds each one for matching against ontology labels (ADR-0013):
/// lower case, accents removed, simple English plurals removed. The query and every label go through
/// the same folding, so "Portátiles", "portatil" and "PORTÁTIL" all meet in the middle.
/// </summary>
public static partial class TextNormaliser
{
    /// <summary>Tokens with their original spelling (for display) and folded form (for matching).</summary>
    public static IReadOnlyList<Token> Tokenise(string text) =>
        [.. WordPattern().Matches(text).Select(m => new Token(m.Value, Fold(m.Value)))];

    /// <summary>The folded tokens joined by single spaces: the key a label is matched on.</summary>
    public static string FoldPhrase(string text) => string.Join(' ', Tokenise(text).Select(t => t.Folded));

    /// <summary>
    /// Lower-cases, strips accents and folds a simple English plural: "Batteries" → "battery",
    /// "SSDs" → "ssd", "Chargers" → "charger". Words ending in "ss" ("cordless") are left alone.
    /// </summary>
    /// <remarks>
    /// This is deliberately crude — no stemmer, no dictionary — so every rule is visible here. It
    /// sometimes over-folds ("lens" → "len"), but it does so identically on both sides of the match.
    /// </remarks>
    public static string Fold(string word)
    {
        var lower = RemoveAccents(word.ToLowerInvariant());

        if (lower.Length > 3 && lower.EndsWith("ies", StringComparison.Ordinal))
        {
            return lower[..^3] + "y";
        }

        if (lower.Length > 3 && lower.EndsWith('s') && !lower.EndsWith("ss", StringComparison.Ordinal))
        {
            return lower[..^1];
        }

        return lower;
    }

    private static string RemoveAccents(string text)
    {
        // Decompose "á" into "a" + a combining accent, then drop the combining marks.
        var decomposed = text.Normalize(NormalizationForm.FormD);
        var kept = decomposed.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark);

        return new string([.. kept]).Normalize(NormalizationForm.FormC);
    }

    // A word is letters or digits, optionally joined by "-" or "." ("usb-c", "5.5mm", "so-dimm").
    [GeneratedRegex(@"[\p{L}\p{N}]+(?:[.\-][\p{L}\p{N}]+)*")]
    private static partial Regex WordPattern();
}

/// <summary>One word of text: as typed, and folded for matching.</summary>
public sealed record Token(string Original, string Folded);

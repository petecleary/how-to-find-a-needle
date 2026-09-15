using System.Globalization;
using System.Text;
using System.Text.Json;
using PI.SearchApi.Contracts;

namespace PI.SearchApi.Pipeline.Rag;

/// <summary>
/// Renders the evidence set as the compact, labelled text blocks the user prompt contains (ADR-0016). Every product
/// line starts with its <c>[PROD-…]</c> ID in the exact form the model is asked to cite, so copying it is the easy path.
/// </summary>
public static class EvidenceFormatter
{
    public const int MaxDescriptionLength = 300;

    private static readonly CultureInfo BritishCulture = CultureInfo.GetCultureInfo("en-GB");

    /// <summary>Cuts a description at a word boundary near <see cref="MaxDescriptionLength"/>, marking the cut with "…".</summary>
    public static string Truncate(string text)
    {
        if (text.Length <= MaxDescriptionLength)
        {
            return text;
        }

        var cut = text.LastIndexOf(' ', MaxDescriptionLength);
        return text[..(cut > 0 ? cut : MaxDescriptionLength)].TrimEnd(',', ';', ' ') + "…";
    }

    public static string FormatTargetDevice(EvidenceSet evidence) =>
        evidence.TargetDevice is { } device
            ? FormatProduct(device)
            : "None: the shopper hasn't said which device they own, so no product can be confirmed as compatible.";

    public static string FormatProducts(EvidenceSet evidence)
    {
        var products = evidence.Items.Where(i => i.Role != EvidenceRole.TargetDevice).ToList();

        return products.Count == 0
            ? "None: no products were retrieved for this query."
            : string.Join("\n\n", products.Select(FormatProduct));
    }

    public static string FormatConcepts(EvidenceSet evidence) =>
        evidence.Concepts.Count == 0
            ? "None: the query didn't match a concept in the ontology."
            : string.Join("\n", evidence.Concepts.Select(concept =>
            {
                var line = new StringBuilder($"- {concept.PrefLabel} ({concept.Notation})");

                if (concept.AltLabels.Count > 0)
                {
                    line.Append(CultureInfo.InvariantCulture, $". Also called: {string.Join(", ", concept.AltLabels)}");
                }

                if (concept.Definition is { } definition)
                {
                    line.Append(CultureInfo.InvariantCulture, $". Definition: {definition}");
                }

                return line.ToString();
            }));

    public static string FormatRules(EvidenceSet evidence) =>
        evidence.Rules.Count == 0
            ? "None: no compatibility rule was checked for these products."
            : string.Join("\n", evidence.Rules.Select(rule => $"- {rule.Name}: {string.Join(" ", rule.Definitions)}"));

    public static string FormatProduct(EvidenceItem item)
    {
        var product = item.Product;
        var specs = string.Join(", ", product.Specs.Select(spec => $"{spec.Key}: {SpecValue(spec.Value)}"));
        var text = new StringBuilder()
            .Append(CultureInfo.InvariantCulture, $"[{product.Id}] {product.Name} — {product.Brand} — {product.Price.ToString("C", BritishCulture)}")
            .Append(CultureInfo.InvariantCulture, $" — specs: {specs}");

        if (item.Role != EvidenceRole.TargetDevice)
        {
            text.Append(CultureInfo.InvariantCulture, $"\nCompatibility: {StatusText(item.Compatibility.Status)}");

            foreach (var reason in item.Compatibility.Reasons)
            {
                text.Append(CultureInfo.InvariantCulture, $"\n  - {ReasonText(reason)}");
            }
        }

        if (item.Description is { } description)
        {
            text.Append(CultureInfo.InvariantCulture, $"\nDescription: {description}");
        }

        return text.ToString();
    }

    private static string StatusText(CompatibilityStatus status) => status switch
    {
        CompatibilityStatus.Compatible => "Compatible",
        CompatibilityStatus.Incompatible => "Incompatible — do not recommend",
        CompatibilityStatus.Unknown => "Unknown — can't be confirmed",
        _ => "Not checked — no rule applies",
    };

    // Stage 5's reasons start with ✓, ✗ or ?, which read well on screen but are easy for a model to misread.
    private static string ReasonText(string reason) => reason switch
    {
        _ when reason.StartsWith("✓ ", StringComparison.Ordinal) => "Passed: " + reason[2..],
        _ when reason.StartsWith("✗ ", StringComparison.Ordinal) => "Failed: " + reason[2..],
        _ when reason.StartsWith("? ", StringComparison.Ordinal) => "Unknown: " + reason[2..],
        _ => reason,
    };

    private static string SpecValue(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => value.GetString() ?? "",
        JsonValueKind.True => "yes",
        JsonValueKind.False => "no",
        JsonValueKind.Array => string.Join(" or ", value.EnumerateArray().Select(SpecValue)),
        _ => value.GetRawText(),
    };
}

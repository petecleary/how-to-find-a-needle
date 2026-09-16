using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace PI.SearchApi.Pipeline.Ontology;

// Stage 5, step 1 — Requirements stated in the query
//
// What:     Turns what the shopper typed into requirements the domain rules can check, without a target
//           device. A matched value concept ("USB-C") becomes connector = usb-c; a number with a unit ("65W")
//           becomes wattageW ≥ 65, using the operator of the rule check that owns that spec.
// Strength: The rules work before anyone picks a device. "65W USB-C charger" flags a barrel charger with a
//           reason, because the knowledge is in the ontology, not in a chosen product.
// Failure:  It only understands values the rules already compare. "fast charger" or "enough for my laptop"
//           state nothing checkable, and a unit must be typed with its number ("65W" or "65 W").
// Decision: docs/decisions/0013-domain-ontology-and-compatibility.md
public sealed partial class QueryRequirementExtractor(IOntology ontology)
{
    public QueryRequirements Extract(QueryUnderstanding understanding)
    {
        // Only the rules for what the shopper wants: "USB-C" in a charger query constrains chargers, nothing else.
        var checks = RelevantChecks(understanding.WantedConcepts);
        var requirements = new List<QueryRequirement>();
        var unusedPhrases = new List<string>();
        var claimed = new bool[understanding.Tokens.Count];

        // 1. Value concepts the label matcher already found: "USB-C" → usb-c in the connectors scheme.
        foreach (var match in understanding.Matches)
        {
            var valueLabels = match.Labels.Where(l => !l.IsTaxonomyConcept).ToList();

            if (valueLabels.Count == 0)
            {
                continue;
            }

            // "Brakk 18V" is one value; its "18V" must not be read again as a number below.
            claimed.AsSpan(match.TokenStart, match.TokenCount).Fill(true);

            var used = false;

            foreach (var label in valueLabels)
            {
                foreach (var check in checks.Where(c => c.ValueSchemeNotation == label.SchemeNotation))
                {
                    // The shopper names one value, so the accessory must be that value, whatever the device-side operator.
                    AddOnce(requirements, new QueryRequirement(
                        check.AccessorySpec, "equals", JsonSerializer.SerializeToElement(label.ConceptNotation), match.Phrase, check.ValueSchemeNotation));
                    used = true;
                }
            }

            if (!used)
            {
                unusedPhrases.Add(match.Phrase);
            }
        }

        // 2. Numbers with a unit: "65W" is one token, "65 W" is two.
        var tokens = understanding.Tokens;

        for (var i = 0; i < tokens.Count; i++)
        {
            if (claimed[i] || !TryReadQuantity(tokens, claimed, i, out var number, out var unit, out var phrase, out var tokenCount))
            {
                continue;
            }

            i += tokenCount - 1;

            // A check with no value scheme compares raw numbers; its accessory spec's unit says what the number measures.
            var numericChecks = checks
                .Where(c => c.ValueSchemeNotation is null && string.Equals(SpecUnit.Of(c.AccessorySpec), unit, StringComparison.OrdinalIgnoreCase))
                .ToList();

            var used = false;

            foreach (var check in numericChecks)
            {
                AddOnce(requirements, new QueryRequirement(
                    check.AccessorySpec, check.Operator, JsonSerializer.SerializeToElement(number), phrase, null));
                used = true;
            }

            if (!used)
            {
                unusedPhrases.Add(phrase); // e.g. "18V": no rule compares voltage, so it stays search text only
            }
        }

        return new QueryRequirements(requirements, unusedPhrases);
    }

    // A rule is relevant when its accessory type is a wanted concept, or broader or narrower than one:
    // "laptop chargers" wants the "chargers" rule, and "chargers" also covers a rule stated for a narrower type.
    private List<RuleCheck> RelevantChecks(IReadOnlyList<string> wantedConcepts) =>
    [
        .. ontology.Rules
            .Where(rule => wantedConcepts.Any(wanted =>
                ontology.IsNarrowerOrSelf(wanted, rule.AccessoryTypeNotation)
                || ontology.IsNarrowerOrSelf(rule.AccessoryTypeNotation, wanted)))
            .SelectMany(rule => rule.Checks),
    ];

    // "USB-C" can reach the same connector check through two labels ("USB-C", "USB Type-C"); keep one requirement.
    private static void AddOnce(List<QueryRequirement> requirements, QueryRequirement requirement)
    {
        var duplicate = requirements.Any(r =>
            r.AccessorySpec == requirement.AccessorySpec
            && r.Operator == requirement.Operator
            && r.Value.GetRawText() == requirement.Value.GetRawText());

        if (!duplicate)
        {
            requirements.Add(requirement);
        }
    }

    private static bool TryReadQuantity(
        IReadOnlyList<Token> tokens, bool[] claimed, int index,
        out double number, out string unit, out string phrase, out int tokenCount)
    {
        var token = tokens[index];
        var joined = NumberWithUnit().Match(token.Folded);

        if (joined.Success && SpecUnit.IsKnown(joined.Groups["unit"].Value))
        {
            number = double.Parse(joined.Groups["number"].Value, CultureInfo.InvariantCulture);
            unit = joined.Groups["unit"].Value;
            phrase = token.Original;
            tokenCount = 1;
            return true;
        }

        if (NumberOnly().IsMatch(token.Folded)
            && index + 1 < tokens.Count
            && !claimed[index + 1]
            && SpecUnit.IsKnown(tokens[index + 1].Folded))
        {
            number = double.Parse(token.Folded, CultureInfo.InvariantCulture);
            unit = tokens[index + 1].Folded;
            phrase = $"{token.Original} {tokens[index + 1].Original}";
            tokenCount = 2;
            return true;
        }

        (number, unit, phrase, tokenCount) = (0, "", "", 0);
        return false;
    }

    [GeneratedRegex(@"^(?<number>\d+(?:\.\d+)?)(?<unit>[a-z]+)$")]
    private static partial Regex NumberWithUnit();

    [GeneratedRegex(@"^\d+(?:\.\d+)?$")]
    private static partial Regex NumberOnly();
}

/// <summary>
/// One requirement stated in the query: the accessory's <paramref name="AccessorySpec"/> must satisfy
/// <paramref name="Operator"/> against <paramref name="Value"/>, e.g. connector equals usb-c, or wattageW
/// greaterOrEqual 65.
/// </summary>
/// <param name="Phrase">The words as typed, e.g. "USB-C" or "65W".</param>
/// <param name="ValueSchemeNotation">The vocabulary the value comes from, or null for a number.</param>
public sealed record QueryRequirement(
    string AccessorySpec,
    string Operator,
    JsonElement Value,
    string Phrase,
    string? ValueSchemeNotation);

/// <summary>The requirements a query states, and the value phrases no relevant rule could use.</summary>
public sealed record QueryRequirements(IReadOnlyList<QueryRequirement> Requirements, IReadOnlyList<string> UnusedPhrases)
{
    public static QueryRequirements None { get; } = new([], []);

    public bool Any => Requirements.Count > 0;
}

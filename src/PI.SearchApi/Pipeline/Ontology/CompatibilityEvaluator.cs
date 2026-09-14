using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using PI.SearchApi.Contracts;

namespace PI.SearchApi.Pipeline.Ontology;

// Stage 6, step 5 — Constrain: class-level domain rules vs the target device
//
// What:     Finds the rules for (candidate's type, device's type) in the ontology, then runs
//           each check — one of four operators — over the candidate's and the device's specs.
//           Vocabulary-backed values are compared as concepts, so "Type-C" equals "usb-c".
// Strength: Similarity is a guess; a rule is knowledge. The 45W barrel charger is rejected with a
//           reason quoting the rule and both values, and the C# never names a specific rule.
// Failure:  The rule language is tiny (equals, ≥, ≤, in). Anything richer — "this USB-C PD profile
//           supports 20V" — needs a richer model: that's where SHACL or a knowledge graph begins.
// Decision: docs/adr/0013-domain-ontology-and-compatibility.md
public sealed partial class CompatibilityEvaluator(IOntology ontology)
{
    public CompatibilityEvaluation Evaluate(ProductSummary candidate, ProductSummary? device)
    {
        if (device is null)
        {
            // Rules exist for this kind of product, but there's nothing to check them against.
            var applicable = ontology.Rules
                .Where(rule => candidate.Categories.Any(c => ontology.IsNarrowerOrSelf(c, rule.AccessoryTypeNotation)))
                .ToList();

            return applicable.Count == 0
                ? new CompatibilityEvaluation(CompatibilityResult.NotEvaluated, [])
                : new CompatibilityEvaluation(
                    new CompatibilityResult(CompatibilityStatus.Unknown,
                        [.. applicable.SelectMany(r => r.Checks).Select(c => $"? No target device: choose one, or name it in the query, to check that {LowerFirst(c.Definition)}")]),
                    []);
        }

        var rules = ontology.RulesFor(candidate.Categories, device.Categories);

        if (rules.Count == 0)
        {
            return new CompatibilityEvaluation(CompatibilityResult.NotEvaluated, []);
        }

        var outcomes = new List<CheckOutcome>();

        foreach (var rule in rules)
        {
            foreach (var check in rule.Checks)
            {
                outcomes.Add(RunCheck($"{rule.AccessoryTypeNotation} → {rule.DeviceTypeNotation}", check, candidate, device));
            }
        }

        // Any failure decides it; otherwise any undecidable check leaves it Unknown.
        var status = outcomes.Any(o => o.Result == CheckResult.Fail) ? CompatibilityStatus.Incompatible
            : outcomes.Any(o => o.Result == CheckResult.Unknown) ? CompatibilityStatus.Unknown
            : CompatibilityStatus.Compatible;

        // Reasons: the checks that decided the status (failures for Incompatible), or every check when it passed.
        var reasons = status switch
        {
            CompatibilityStatus.Incompatible => outcomes.Where(o => o.Result == CheckResult.Fail),
            CompatibilityStatus.Unknown => outcomes.Where(o => o.Result == CheckResult.Unknown),
            _ => outcomes,
        };

        return new CompatibilityEvaluation(
            new CompatibilityResult(status, [.. reasons.Select(o => o.Reason)]),
            outcomes);
    }

    private CheckOutcome RunCheck(string ruleName, RuleCheck check, ProductSummary candidate, ProductSummary device)
    {
        var hasAccessoryValue = candidate.Specs.TryGetValue(check.AccessorySpec, out var accessoryValue);
        var hasDeviceValue = device.Specs.TryGetValue(check.DeviceSpec, out var deviceValue);

        if (!hasAccessoryValue || !hasDeviceValue)
        {
            var (missingProduct, missingSpec) = !hasAccessoryValue ? (candidate.Name, check.AccessorySpec) : (device.Name, check.DeviceSpec);

            return new CheckOutcome(
                candidate.Id, ruleName, check.Definition,
                check.AccessorySpec, hasAccessoryValue ? Display(accessoryValue, check.AccessorySpec, check.ValueSchemeNotation) : null,
                check.Operator,
                check.DeviceSpec, hasDeviceValue ? Display(deviceValue, check.DeviceSpec, check.ValueSchemeNotation) : null,
                CheckResult.Unknown,
                $"? {check.Definition} {missingProduct} has no '{missingSpec}' spec, so this can't be checked.");
        }

        var result = check.Operator switch
        {
            "equals" => AreEqual(accessoryValue, deviceValue, check.ValueSchemeNotation),
            "greaterOrEqual" => CompareNumbers(accessoryValue, deviceValue, (a, d) => a >= d),
            "lessOrEqual" => CompareNumbers(accessoryValue, deviceValue, (a, d) => a <= d),
            "in" => IsIn(accessoryValue, deviceValue, check.ValueSchemeNotation),
            _ => CheckResult.Unknown, // an operator this evaluator doesn't know: never guess
        };

        var accessoryDisplay = Display(accessoryValue, check.AccessorySpec, check.ValueSchemeNotation);
        var deviceDisplay = Display(deviceValue, check.DeviceSpec, check.ValueSchemeNotation);
        var symbol = result switch { CheckResult.Pass => "✓", CheckResult.Fail => "✗", _ => "?" };

        // Quote the rule and both values, e.g. "✗ The charger's plug must fit the laptop's charging port.
        // Voltline 45W Barrel Charger has 5.5mm barrel; Blackbird Aerobook 14 needs USB-C."
        var reason = $"{symbol} {check.Definition} {candidate.Name} has {accessoryDisplay}; {device.Name} {Requirement(check.Operator)} {deviceDisplay}.";

        return new CheckOutcome(
            candidate.Id, ruleName, check.Definition,
            check.AccessorySpec, accessoryDisplay, check.Operator, check.DeviceSpec, deviceDisplay,
            result, reason);
    }

    // equals: vocabulary-backed values compare as concepts; other values compare as JSON values.
    private CheckResult AreEqual(JsonElement accessory, JsonElement device, string? scheme)
    {
        if (scheme is not null)
        {
            if (accessory.ValueKind != JsonValueKind.String || device.ValueKind != JsonValueKind.String
                || !ontology.TryResolveVocabularyValue(scheme, accessory.GetString()!, out var accessoryConcept)
                || !ontology.TryResolveVocabularyValue(scheme, device.GetString()!, out var deviceConcept))
            {
                return CheckResult.Unknown; // a value the vocabulary doesn't know can't be compared as a concept
            }

            return accessoryConcept == deviceConcept ? CheckResult.Pass : CheckResult.Fail;
        }

        return (accessory.ValueKind, device.ValueKind) switch
        {
            (JsonValueKind.Number, JsonValueKind.Number) => accessory.GetDouble() == device.GetDouble() ? CheckResult.Pass : CheckResult.Fail,
            (JsonValueKind.String, JsonValueKind.String) =>
                string.Equals(accessory.GetString(), device.GetString(), StringComparison.OrdinalIgnoreCase) ? CheckResult.Pass : CheckResult.Fail,
            _ => accessory.GetRawText() == device.GetRawText() ? CheckResult.Pass : CheckResult.Fail,
        };
    }

    private static CheckResult CompareNumbers(JsonElement accessory, JsonElement device, Func<double, double, bool> comparison) =>
        accessory.ValueKind == JsonValueKind.Number && device.ValueKind == JsonValueKind.Number
            ? comparison(accessory.GetDouble(), device.GetDouble()) ? CheckResult.Pass : CheckResult.Fail
            : CheckResult.Unknown;

    // in: the device lists the values it accepts; the accessory's value must be one of them.
    private CheckResult IsIn(JsonElement accessory, JsonElement device, string? scheme)
    {
        if (device.ValueKind != JsonValueKind.Array)
        {
            return CheckResult.Unknown;
        }

        var results = device.EnumerateArray().Select(allowed => AreEqual(accessory, allowed, scheme)).ToList();

        return results.Contains(CheckResult.Pass) ? CheckResult.Pass
            : results.Contains(CheckResult.Unknown) ? CheckResult.Unknown
            : CheckResult.Fail;
    }

    private static string Requirement(string @operator) => @operator switch
    {
        "greaterOrEqual" => "needs at least",
        "lessOrEqual" => "accepts at most",
        "in" => "accepts one of",
        _ => "needs",
    };

    // How a value reads in a reason: a vocabulary concept's preferred label ("USB-C"), a number with the
    // unit from its spec name ("wattageW": 45 → "45W"), or the raw value.
    private string Display(JsonElement value, string specName, string? scheme)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.String:
                var text = value.GetString()!;
                if (scheme is not null && ontology.TryResolveVocabularyValue(scheme, text, out var notation))
                {
                    return ontology.Labels.FirstOrDefault(l =>
                        l.ConceptNotation == notation && l.SchemeNotation == scheme && l.Kind == LabelKind.Preferred && l.Language == "en")?.Label
                        ?? notation;
                }

                return text;

            case JsonValueKind.Number:
                var unit = UnitSuffix().Match(specName);
                return value.GetDouble().ToString(CultureInfo.InvariantCulture) + (unit.Success ? unit.Value : "");

            case JsonValueKind.Array:
                return string.Join(", ", value.EnumerateArray().Select(v => Display(v, specName, scheme)));

            case JsonValueKind.True:
                return "yes";

            case JsonValueKind.False:
                return "no";

            default:
                return value.GetRawText();
        }
    }

    private static string LowerFirst(string text) => text.Length == 0 ? text : char.ToLowerInvariant(text[0]) + text[1..];

    // Spec names carry their unit (ADR-0005): wattageW, voltageV, capacityAh.
    [GeneratedRegex("(W|V|Ah|Gb|Mah|In|Kg)$")]
    private static partial Regex UnitSuffix();
}

/// <summary>A candidate's compatibility plus every check that decided it (for the trace).</summary>
public sealed record CompatibilityEvaluation(CompatibilityResult Result, IReadOnlyList<CheckOutcome> Checks);

public enum CheckResult
{
    Pass,
    Fail,
    Unknown,
}

/// <summary>One rule check on one candidate, with the values compared.</summary>
public sealed record CheckOutcome(
    string CandidateId,
    string Rule,
    string Definition,
    string AccessorySpec,
    string? AccessoryValue,
    string Operator,
    string DeviceSpec,
    string? DeviceValue,
    CheckResult Result,
    string Reason);

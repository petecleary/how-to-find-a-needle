using System.Globalization;
using System.Text.Json;
using PI.SearchApi.Contracts;

namespace PI.SearchApi.Pipeline.Ontology;

// Stage 5, step 5 — Constrain: class-level domain rules vs the target device, or vs the query
//
// What:     Finds the rules for (candidate's type, device's type) in the ontology, then runs each check — one
//           of four operators — over the candidate's and the device's specs. With no device, the same checks
//           run against what the shopper stated ("65W USB-C charger"). Vocabulary-backed values are compared
//           as concepts, so "Type-C" equals "usb-c".
// Strength: Similarity is a guess; a rule is knowledge. The 45W barrel charger is rejected with a reason
//           quoting the rule and both values, and the C# never names a specific rule.
// Failure:  The rule language is tiny (equals, ≥, ≤, in). Anything richer — "this USB-C PD profile
//           supports 20V" — needs a richer model: that's where SHACL or a knowledge graph begins.
// Decision: docs/decisions/0013-domain-ontology-and-compatibility.md
public sealed class CompatibilityEvaluator(IOntology ontology)
{
    public CompatibilityEvaluation Evaluate(ProductSummary candidate, ProductSummary? device)
    {
        if (device is null)
        {
            // Rules exist for this kind of product, but there's nothing to check them against.
            var applicable = RulesForAccessory(candidate);

            return applicable.Count == 0
                ? new CompatibilityEvaluation(CompatibilityResult.NotEvaluated, [])
                : new CompatibilityEvaluation(
                    new CompatibilityResult(CompatibilityStatus.Unknown,
                        [.. applicable.SelectMany(r => r.Checks).Select(NoDeviceReason)]),
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
                outcomes.Add(RunCheck(RuleName(rule), check, candidate, device));
            }
        }

        return Decide(outcomes, CompatibilitySource.Device);
    }

    /// <summary>
    /// With no target device: checks the candidate against the requirements stated in the query. Each requirement
    /// stands in for the device's value in the check it belongs to; a check with no stated value stays Unknown.
    /// </summary>
    public CompatibilityEvaluation EvaluateAgainstRequirements(ProductSummary candidate, IReadOnlyList<QueryRequirement> requirements)
    {
        var rules = RulesForAccessory(candidate);

        if (rules.Count == 0)
        {
            return new CompatibilityEvaluation(CompatibilityResult.NotEvaluated, []);
        }

        var outcomes = new List<CheckOutcome>();

        foreach (var rule in rules)
        {
            foreach (var check in rule.Checks)
            {
                var requirement = requirements.FirstOrDefault(r =>
                    r.AccessorySpec == check.AccessorySpec && r.ValueSchemeNotation == check.ValueSchemeNotation);

                outcomes.Add(requirement is null
                    ? new CheckOutcome(candidate.Id, RuleName(rule), check.Definition, check.AccessorySpec, null, check.Operator,
                        check.DeviceSpec, null, CheckResult.Unknown, NoDeviceReason(check)) { Source = CompatibilitySource.Query }
                    : RunRequirementCheck(RuleName(rule), check, candidate, requirement));
            }
        }

        return Decide(outcomes, CompatibilitySource.Query);
    }

    /// <summary>
    /// Stated requirements that the target device contradicts, e.g. "45W" for a laptop that needs at least 65W.
    /// The device decides; these are reported in the trace, not applied.
    /// </summary>
    public IReadOnlyList<string> ConflictsWithDevice(IReadOnlyList<QueryRequirement> requirements, ProductSummary device)
    {
        var conflicts = new List<string>();

        var deviceChecks = ontology.Rules
            .Where(rule => device.Categories.Any(c => ontology.IsNarrowerOrSelf(c, rule.DeviceTypeNotation)))
            .SelectMany(rule => rule.Checks);

        foreach (var check in deviceChecks)
        {
            var requirement = requirements.FirstOrDefault(r =>
                r.AccessorySpec == check.AccessorySpec && r.ValueSchemeNotation == check.ValueSchemeNotation);

            if (requirement is null || !device.Specs.TryGetValue(check.DeviceSpec, out var deviceValue))
            {
                continue;
            }

            // Would a product that exactly meets the requirement pass against the device? If not, they disagree.
            if (Compare(check.Operator, requirement.Value, deviceValue, check.ValueSchemeNotation) == CheckResult.Fail)
            {
                conflicts.Add(
                    $"You asked for {RequirementText(requirement, check)}, but {device.Name} {Requirement(check.Operator)} " +
                    $"{Display(deviceValue, check.DeviceSpec, check.ValueSchemeNotation)}. The target device decides.");
            }
        }

        return conflicts;
    }

    /// <summary>The rules whose accessory type this product belongs to, whatever the device.</summary>
    public IReadOnlyList<CompatibilityRule> RulesForAccessory(ProductSummary candidate) =>
    [
        .. ontology.Rules.Where(rule => candidate.Categories.Any(c => ontology.IsNarrowerOrSelf(c, rule.AccessoryTypeNotation))),
    ];

    private static CompatibilityEvaluation Decide(List<CheckOutcome> outcomes, CompatibilitySource source)
    {
        // Any failure decides it; otherwise any undecidable check leaves it Unknown.
        var status = outcomes.Any(o => o.Result == CheckResult.Fail) ? CompatibilityStatus.Incompatible
            : outcomes.Any(o => o.Result == CheckResult.Unknown) ? CompatibilityStatus.Unknown
            : CompatibilityStatus.Compatible;

        // Reasons: the checks that decided the status (failures for Incompatible), or every check when it passed.
        // Against the query, an Unknown also shows what *did* pass: "✓ you asked for USB-C" is worth seeing.
        var reasons = status switch
        {
            CompatibilityStatus.Incompatible => outcomes.Where(o => o.Result == CheckResult.Fail),
            CompatibilityStatus.Unknown when source == CompatibilitySource.Device => outcomes.Where(o => o.Result == CheckResult.Unknown),
            _ => outcomes,
        };

        return new CompatibilityEvaluation(
            new CompatibilityResult(status, [.. reasons.Select(o => o.Reason)]) { Source = source },
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

        var result = Compare(check.Operator, accessoryValue, deviceValue, check.ValueSchemeNotation);
        var accessoryDisplay = Display(accessoryValue, check.AccessorySpec, check.ValueSchemeNotation);
        var deviceDisplay = Display(deviceValue, check.DeviceSpec, check.ValueSchemeNotation);

        // Quote the rule and both values, e.g. "✗ The charger's plug must fit the laptop's charging port.
        // Voltline 45W Barrel Charger has 5.5mm barrel; Blackbird Aerobook 14 needs USB-C."
        var reason = $"{Symbol(result)} {check.Definition} {candidate.Name} has {accessoryDisplay}; {device.Name} {Requirement(check.Operator)} {deviceDisplay}.";

        return new CheckOutcome(
            candidate.Id, ruleName, check.Definition,
            check.AccessorySpec, accessoryDisplay, check.Operator, check.DeviceSpec, deviceDisplay,
            result, reason);
    }

    private CheckOutcome RunRequirementCheck(string ruleName, RuleCheck check, ProductSummary candidate, QueryRequirement requirement)
    {
        // The outcome records the bare value ("65W") next to its operator, like a device's value; the reason reads
        // it as a sentence ("at least 65W").
        var requiredValue = Display(requirement.Value, check.AccessorySpec, check.ValueSchemeNotation);
        var requirementDisplay = RequirementText(requirement, check);

        if (!candidate.Specs.TryGetValue(check.AccessorySpec, out var accessoryValue))
        {
            return new CheckOutcome(
                candidate.Id, ruleName, check.Definition, check.AccessorySpec, null, requirement.Operator,
                check.DeviceSpec, requiredValue, CheckResult.Unknown,
                $"? {check.Definition} {candidate.Name} has no '{check.AccessorySpec}' spec, so this can't be checked.")
            { Source = CompatibilitySource.Query };
        }

        // The stated value takes the device's place in the comparison: "65W" stands in for minChargerWattageW.
        var result = Compare(requirement.Operator, accessoryValue, requirement.Value, check.ValueSchemeNotation);
        var accessoryDisplay = Display(accessoryValue, check.AccessorySpec, check.ValueSchemeNotation);

        // "✗ The charger's plug must fit the laptop's charging port. Voltline 45W Barrel Charger has 5.5mm barrel;
        // you asked for USB-C."
        var reason = $"{Symbol(result)} {check.Definition} {candidate.Name} has {accessoryDisplay}; you asked for {requirementDisplay}.";

        return new CheckOutcome(
            candidate.Id, ruleName, check.Definition, check.AccessorySpec, accessoryDisplay, requirement.Operator,
            check.DeviceSpec, requiredValue, result, reason)
        { Source = CompatibilitySource.Query };
    }

    private CheckResult Compare(string @operator, JsonElement accessory, JsonElement required, string? scheme) => @operator switch
    {
        "equals" => AreEqual(accessory, required, scheme),
        "greaterOrEqual" => CompareNumbers(accessory, required, (a, d) => a >= d),
        "lessOrEqual" => CompareNumbers(accessory, required, (a, d) => a <= d),
        "in" => IsIn(accessory, required, scheme),
        _ => CheckResult.Unknown, // an operator this evaluator doesn't know: never guess
    };

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

    private static string Symbol(CheckResult result) => result switch { CheckResult.Pass => "✓", CheckResult.Fail => "✗", _ => "?" };

    private static string RuleName(CompatibilityRule rule) => $"{rule.AccessoryTypeNotation} → {rule.DeviceTypeNotation}";

    private static string NoDeviceReason(RuleCheck check) =>
        $"? No target device: choose one, or name it in the query, to check that {LowerFirst(check.Definition)}";

    private static string Requirement(string @operator) => @operator switch
    {
        "greaterOrEqual" => "needs at least",
        "lessOrEqual" => "accepts at most",
        "in" => "accepts one of",
        _ => "needs",
    };

    // How a stated requirement reads: "USB-C", "at least 65W", "at most 20V".
    private string RequirementText(QueryRequirement requirement, RuleCheck check)
    {
        var value = Display(requirement.Value, check.AccessorySpec, check.ValueSchemeNotation);

        return requirement.Operator switch
        {
            "greaterOrEqual" => $"at least {value}",
            "lessOrEqual" => $"at most {value}",
            _ => value,
        };
    }

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
                return value.GetDouble().ToString(CultureInfo.InvariantCulture) + (SpecUnit.Of(specName) ?? "");

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
/// <remarks>
/// When <see cref="Source"/> is <see cref="CompatibilitySource.Query"/>, <see cref="DeviceValue"/> is the value the
/// shopper stated ("65W", compared with <see cref="Operator"/>), standing in for the device's <see cref="DeviceSpec"/>.
/// </remarks>
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
    string Reason)
{
    public CompatibilitySource Source { get; init; } = CompatibilitySource.Device;
}

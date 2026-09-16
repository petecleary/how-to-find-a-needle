using System.Text.Json;
using PI.SearchApi.Contracts;
using PI.SearchApi.Pipeline;
using PI.SearchApi.Pipeline.Ontology;
using Xunit;

namespace PI.SearchApi.Tests.Pipeline.Ontology;

public sealed class CompatibilityEvaluatorTests
{
    private static readonly DomainOntology Ontology = new(Path.Combine(AppContext.BaseDirectory, "assets", "data"));
    private static readonly CompatibilityEvaluator Evaluator = new(Ontology);

    private static ProductSummary Product(string id, string name, string category, string specsJson) =>
        new(id, name, "Test", [category], 10m, JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(specsJson)!);

    private static readonly ProductSummary Aerobook = Product(
        "PROD-0001", "Blackbird Aerobook 14", "laptops",
        """{ "chargingPort": "usb-c", "minChargerWattageW": 65, "m2SlotInterface": "nvme", "memoryType": "ddr5-sodimm" }""");

    [Fact]
    public void Evaluate_MatchingConnectorAndEnoughWatts_IsCompatible()
    {
        var charger = Product("C1", "Voltline 65W USB-C GaN Charger", "laptop-chargers", """{ "connector": "usb-c", "wattageW": 65 }""");

        var evaluation = Evaluator.Evaluate(charger, Aerobook);

        Assert.Equal(CompatibilityStatus.Compatible, evaluation.Result.Status);
        Assert.Equal(2, evaluation.Checks.Count);
        Assert.All(evaluation.Checks, c => Assert.Equal(CheckResult.Pass, c.Result));
    }

    [Fact]
    public void Evaluate_BarrelCharger_IsIncompatibleWithBothReasonsQuotingRuleAndValues()
    {
        // GQ-01's near miss: wrong plug (5.5mm barrel ≠ USB-C) and too little power (45 < 65).
        var charger = Product("PROD-0014", "Voltline 45W Barrel Charger", "laptop-chargers", """{ "connector": "barrel-5.5mm", "wattageW": 45 }""");

        var evaluation = Evaluator.Evaluate(charger, Aerobook);

        Assert.Equal(CompatibilityStatus.Incompatible, evaluation.Result.Status);
        Assert.Equal(2, evaluation.Result.Reasons.Count);
        Assert.Contains(evaluation.Result.Reasons, r =>
            r.Contains("The charger's plug must fit the laptop's charging port.") && r.Contains("5.5mm barrel") && r.Contains("needs USB-C"));
        Assert.Contains(evaluation.Result.Reasons, r => r.Contains("45W") && r.Contains("needs at least 65W"));
    }

    [Fact]
    public void Evaluate_VocabularySynonym_EqualsTheNotation()
    {
        // "Type-C" is an altLabel of the usb-c concept, so it equals the laptop's "usb-c".
        var charger = Product("C2", "Generic Type-C Charger", "laptop-chargers", """{ "connector": "Type-C", "wattageW": 100 }""");

        Assert.Equal(CompatibilityStatus.Compatible, Evaluator.Evaluate(charger, Aerobook).Result.Status);
    }

    [Theory]
    [InlineData(64, CompatibilityStatus.Incompatible)]
    [InlineData(65, CompatibilityStatus.Compatible)]
    [InlineData(100, CompatibilityStatus.Compatible)]
    public void Evaluate_GreaterOrEqual_ComparesNumbers(int wattage, CompatibilityStatus expected)
    {
        var charger = Product("C3", "Charger", "laptop-chargers", $$"""{ "connector": "usb-c", "wattageW": {{wattage}} }""");

        Assert.Equal(expected, Evaluator.Evaluate(charger, Aerobook).Result.Status);
    }

    [Fact]
    public void Evaluate_MissingAccessorySpec_IsUnknownAndSaysWhichSpec()
    {
        var charger = Product("C4", "Mystery Charger", "laptop-chargers", """{ "connector": "usb-c" }""");

        var evaluation = Evaluator.Evaluate(charger, Aerobook);

        Assert.Equal(CompatibilityStatus.Unknown, evaluation.Result.Status);
        Assert.Contains(evaluation.Result.Reasons, r => r.Contains("'wattageW'"));
    }

    [Fact]
    public void Evaluate_FailureAndMissingSpec_FailureWins()
    {
        var charger = Product("C5", "Barrel Charger", "laptop-chargers", """{ "connector": "barrel-5.5mm" }""");

        Assert.Equal(CompatibilityStatus.Incompatible, Evaluator.Evaluate(charger, Aerobook).Result.Status);
    }

    [Fact]
    public void Evaluate_SataSsdInNvmeLaptop_IsIncompatibleThroughNarrowerCategory()
    {
        // GQ-06: the rule is stated for "ssds"; the product is filed under the narrower "sata-ssds".
        var ssd = Product("PROD-0019", "Kestrel 1TB SATA M.2 2280 SSD", "sata-ssds", """{ "interface": "sata" }""");

        var evaluation = Evaluator.Evaluate(ssd, Aerobook);

        Assert.Equal(CompatibilityStatus.Incompatible, evaluation.Result.Status);
        Assert.Equal("ssds → laptops", Assert.Single(evaluation.Checks).Rule);
    }

    [Fact]
    public void Evaluate_BatteryPlatformMismatch_IsIncompatible()
    {
        // GQ-05: same numbers on the box, different platform.
        var drill = Product("PROD-0006", "Brakk 18V Combi Drill", "drills", """{ "batteryPlatform": "brakk-18v" }""");
        var battery = Product("PROD-0028", "Tornio 20V MAX 4.0Ah Battery", "batteries", """{ "platform": "tornio-20v-max" }""");

        Assert.Equal(CompatibilityStatus.Incompatible, Evaluator.Evaluate(battery, drill).Result.Status);
    }

    [Fact]
    public void Evaluate_NoRuleForThisPair_IsNotEvaluated()
    {
        var sleeve = Product("PROD-0037", "Laptop Sleeve", "bags", "{}");

        Assert.Equal(CompatibilityStatus.NotEvaluated, Evaluator.Evaluate(sleeve, Aerobook).Result.Status);
    }

    [Fact]
    public void Evaluate_RuleAppliesButNoTargetDevice_IsUnknown()
    {
        var charger = Product("C6", "Charger", "laptop-chargers", """{ "connector": "usb-c", "wattageW": 65 }""");

        var evaluation = Evaluator.Evaluate(charger, device: null);

        Assert.Equal(CompatibilityStatus.Unknown, evaluation.Result.Status);
        Assert.Contains(evaluation.Result.Reasons, r => r.Contains("No target device"));
    }

    [Fact]
    public void Evaluate_NoRuleAndNoTargetDevice_IsNotEvaluated()
    {
        Assert.Equal(CompatibilityStatus.NotEvaluated, Evaluator.Evaluate(Product("X", "Speaker", "audio", "{}"), device: null).Result.Status);
    }

    [Fact]
    public void Evaluate_WithTargetDevice_SourceIsDevice()
    {
        var charger = Product("C7", "Charger", "laptop-chargers", """{ "connector": "usb-c", "wattageW": 65 }""");

        Assert.Equal(CompatibilitySource.Device, Evaluator.Evaluate(charger, Aerobook).Result.Source);
    }

    // --- Requirements stated in the query (no target device) ---------------------------------------------------

    private static readonly QueryRequirement UsbC = new("connector", "equals", JsonSerializer.SerializeToElement("usb-c"), "USB-C", "connectors");
    private static readonly QueryRequirement AtLeast65W = new("wattageW", "greaterOrEqual", JsonSerializer.SerializeToElement(65), "65W", null);

    [Fact]
    public void EvaluateAgainstRequirements_BarrelChargerForUsbC_IsIncompatibleAndSaysWhatWasAskedFor()
    {
        var charger = Product("PROD-0014", "Voltline 45W Barrel Charger", "laptop-chargers", """{ "connector": "barrel-5.5mm", "wattageW": 45 }""");

        var evaluation = Evaluator.EvaluateAgainstRequirements(charger, [UsbC, AtLeast65W]);

        Assert.Equal(CompatibilityStatus.Incompatible, evaluation.Result.Status);
        Assert.Equal(CompatibilitySource.Query, evaluation.Result.Source);
        Assert.Contains(evaluation.Result.Reasons, r => r.Contains("has 5.5mm barrel; you asked for USB-C."));
        Assert.Contains(evaluation.Result.Reasons, r => r.Contains("has 45W; you asked for at least 65W."));
        Assert.All(evaluation.Checks, c => Assert.Equal(CompatibilitySource.Query, c.Source));
    }

    [Theory]
    [InlineData(65, CompatibilityStatus.Compatible)]
    [InlineData(100, CompatibilityStatus.Compatible)]
    [InlineData(60, CompatibilityStatus.Incompatible)]
    public void EvaluateAgainstRequirements_StatedWattage_IsAMinimum(int wattage, CompatibilityStatus expected)
    {
        // "65W" stands in for the laptop's minChargerWattageW, so the rule's ≥ applies: a 100W charger meets it.
        var charger = Product("C8", "Charger", "laptop-chargers", $$"""{ "connector": "usb-c", "wattageW": {{wattage}} }""");

        Assert.Equal(expected, Evaluator.EvaluateAgainstRequirements(charger, [UsbC, AtLeast65W]).Result.Status);
    }

    [Fact]
    public void EvaluateAgainstRequirements_OnlyConnectorStated_IsUnknownButShowsWhatPassed()
    {
        // No one said how much power is needed, so a USB-C charger can't be confirmed; the passed check is still shown.
        var charger = Product("C9", "45W USB-C Charger", "laptop-chargers", """{ "connector": "usb-c", "wattageW": 45 }""");

        var evaluation = Evaluator.EvaluateAgainstRequirements(charger, [UsbC]);

        Assert.Equal(CompatibilityStatus.Unknown, evaluation.Result.Status);
        Assert.Contains(evaluation.Result.Reasons, r => r.StartsWith("✓", StringComparison.Ordinal) && r.Contains("you asked for USB-C"));
        Assert.Contains(evaluation.Result.Reasons, r => r.StartsWith("?", StringComparison.Ordinal) && r.Contains("No target device"));
    }

    [Fact]
    public void EvaluateAgainstRequirements_ProductWithNoRules_IsNotEvaluated()
    {
        var sleeve = Product("PROD-0037", "Laptop Sleeve", "bags", "{}");

        Assert.Equal(CompatibilityStatus.NotEvaluated, Evaluator.EvaluateAgainstRequirements(sleeve, [UsbC]).Result.Status);
    }

    [Fact]
    public void ConflictsWithDevice_TooFewWattsForTheLaptop_IsReported()
    {
        var fortyFive = AtLeast65W with { Value = JsonSerializer.SerializeToElement(45), Phrase = "45W" };

        var conflict = Assert.Single(Evaluator.ConflictsWithDevice([UsbC, fortyFive], Aerobook));

        Assert.Equal("You asked for at least 45W, but Blackbird Aerobook 14 needs at least 65W. The target device decides.", conflict);
    }
}

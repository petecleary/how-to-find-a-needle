using PI.SearchApi.Pipeline.Ontology;
using Xunit;

namespace PI.SearchApi.Tests.Pipeline.Ontology;

public sealed class DomainOntologyTests
{
    private static DomainOntology Load() =>
        new(Path.Combine(AppContext.BaseDirectory, "assets", "data"));

    [Fact]
    public void Concepts_LaptopChargers_HasBroaderChargers()
    {
        var ontology = Load();

        Assert.True(ontology.TryGetConcept("laptop-chargers", out var concept));
        Assert.Equal("chargers", concept.BroaderNotation);
        Assert.Equal("Laptop chargers", concept.PrefLabels["en"]);
    }

    [Fact]
    public void Concepts_Laptops_IsDeviceTypeWithSpanishLabel()
    {
        var ontology = Load();

        Assert.True(ontology.TryGetConcept("laptops", out var concept));
        Assert.True(concept.IsDeviceType);
        Assert.Equal("Portátiles", concept.PrefLabels["es"]);
    }

    [Fact]
    public void Concepts_Chargers_IsNotADeviceType()
    {
        var ontology = Load();

        Assert.True(ontology.TryGetConcept("chargers", out var concept));
        Assert.False(concept.IsDeviceType);
    }

    [Fact]
    public void IsNarrowerOrSelf_LaptopChargersUnderChargers_IsTrue()
    {
        var ontology = Load();

        Assert.True(ontology.IsNarrowerOrSelf("laptop-chargers", "chargers"));
        Assert.True(ontology.IsNarrowerOrSelf("chargers", "chargers")); // reflexive
        Assert.False(ontology.IsNarrowerOrSelf("chargers", "laptop-chargers")); // not symmetric
    }

    [Fact]
    public void IsNarrowerOrSelf_NvmeSsdsUnderSsds_IsTrue()
    {
        var ontology = Load();

        Assert.True(ontology.IsNarrowerOrSelf("nvme-ssds", "ssds"));
        Assert.True(ontology.IsNarrowerOrSelf("sata-ssds", "ssds"));
    }

    [Fact]
    public void VocabularyValues_Connectors_IncludesUsbCSynonym()
    {
        var ontology = Load();

        var connectors = ontology.VocabularyValues("connectors");
        var usbC = Assert.Single(connectors, v => v.Notation == "usb-c");
        Assert.Contains("Type-C", usbC.Labels);
        Assert.Contains("USB-C", usbC.Labels);
    }

    [Fact]
    public void VocabularyValues_UnknownScheme_ReturnsEmpty()
    {
        var ontology = Load();

        Assert.Empty(ontology.VocabularyValues("not-a-scheme"));
    }

    [Fact]
    public void Rules_ChargerFitsLaptop_HasTwoChecksWithConnectorScheme()
    {
        var ontology = Load();

        var rule = Assert.Single(ontology.Rules, r =>
            r.AccessoryTypeNotation == "laptop-chargers" && r.DeviceTypeNotation == "laptops");

        Assert.Equal(2, rule.Checks.Count);

        var connectorCheck = Assert.Single(rule.Checks, c => c.AccessorySpec == "connector");
        Assert.Equal("equals", connectorCheck.Operator);
        Assert.Equal("chargingPort", connectorCheck.DeviceSpec);
        Assert.Equal("connectors", connectorCheck.ValueSchemeNotation);

        var wattageCheck = Assert.Single(rule.Checks, c => c.AccessorySpec == "wattageW");
        Assert.Equal("greaterOrEqual", wattageCheck.Operator);
        Assert.Null(wattageCheck.ValueSchemeNotation); // numeric check, no vocabulary
    }

    [Fact]
    public void Rules_BatteryFitsTool_MatchesBatteriesAndDrills()
    {
        var ontology = Load();

        var rule = Assert.Single(ontology.Rules, r =>
            r.AccessoryTypeNotation == "batteries" && r.DeviceTypeNotation == "drills");
        var check = Assert.Single(rule.Checks);
        Assert.Equal("platform", check.AccessorySpec);
        Assert.Equal("battery-platforms", check.ValueSchemeNotation);
    }

    [Fact]
    public void Rules_Count_MatchesTheFourDocumentedRules()
    {
        var ontology = Load();

        Assert.Equal(4, ontology.Rules.Count);
    }
}

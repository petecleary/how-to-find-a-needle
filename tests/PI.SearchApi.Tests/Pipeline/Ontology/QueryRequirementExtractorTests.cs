using PI.SearchApi.Pipeline.Ontology;
using Xunit;

namespace PI.SearchApi.Tests.Pipeline.Ontology;

public sealed class QueryRequirementExtractorTests
{
    private static readonly DomainOntology Ontology = new(Path.Combine(AppContext.BaseDirectory, "assets", "data"));
    private static readonly LabelMatcher Matcher = new(Ontology);
    private static readonly QueryRequirementExtractor Extractor = new(Ontology);

    private static QueryRequirements Extract(string query) => Extractor.Extract(Matcher.Understand(query));

    [Fact]
    public void Extract_65WUsbCCharger_ConnectorEqualsAndWattageAtLeast()
    {
        // "USB-C" is a value concept in the connectors vocabulary; "65W" carries the unit of the rule's wattageW spec.
        var requirements = Extract("65W USB-C charger");

        Assert.Collection(
            requirements.Requirements.OrderBy(r => r.AccessorySpec),
            connector =>
            {
                Assert.Equal(("connector", "equals", "usb-c", "USB-C"), (connector.AccessorySpec, connector.Operator, connector.Value.GetString(), connector.Phrase));
                Assert.Equal("connectors", connector.ValueSchemeNotation);
            },
            wattage =>
            {
                Assert.Equal(("wattageW", "greaterOrEqual", 65d, "65W"), (wattage.AccessorySpec, wattage.Operator, wattage.Value.GetDouble(), wattage.Phrase));
                Assert.Null(wattage.ValueSchemeNotation);
            });
        Assert.Empty(requirements.UnusedPhrases);
    }

    [Fact]
    public void Extract_NumberAndUnitAsTwoWords_IsOneRequirement()
    {
        var requirement = Assert.Single(Extract("65 W laptop charger").Requirements);

        Assert.Equal(("wattageW", 65d, "65 W"), (requirement.AccessorySpec, requirement.Value.GetDouble(), requirement.Phrase));
    }

    [Fact]
    public void Extract_Synonym_ResolvesToTheNotation()
    {
        // "Type-C" is an altLabel of usb-c.
        var requirement = Assert.Single(Extract("Type-C charger").Requirements);

        Assert.Equal("usb-c", requirement.Value.GetString());
    }

    [Fact]
    public void Extract_SpanishQuery_FindsTheConnector()
    {
        // GQ-07: "cargador" wants Chargers, so "USB-C" constrains the charger rule.
        var requirement = Assert.Single(Extract("cargador USB-C para portátil").Requirements);

        Assert.Equal(("connector", "usb-c"), (requirement.AccessorySpec, requirement.Value.GetString()));
    }

    [Fact]
    public void Extract_NoWantedCategory_StatesNothingAndListsThePhrasesAsUnused()
    {
        // Without a wanted category there's no rule to scope the values to.
        var requirements = Extract("USB-C 65W");

        Assert.Empty(requirements.Requirements);
        Assert.Equal(["USB-C", "65W"], requirements.UnusedPhrases);
    }

    [Fact]
    public void Extract_ValueNoRelevantRuleUses_IsUnused()
    {
        // NVMe is a storage interface; nothing in the charger rule compares one.
        var requirements = Extract("NVMe charger");

        Assert.Empty(requirements.Requirements);
        Assert.Equal(["NVMe"], requirements.UnusedPhrases);
    }

    [Fact]
    public void Extract_VoltageForBatteries_IsUnusedBecauseTheRuleComparesPlatforms()
    {
        // The battery rule compares platforms, not volts: "18V" alone can't tell Brakk 18V from Tornio 20V MAX.
        var requirements = Extract("18V battery");

        Assert.Empty(requirements.Requirements);
        Assert.Equal(["18V"], requirements.UnusedPhrases);
    }

    [Fact]
    public void Extract_NumberInsideAValueLabel_IsNotReadAgain()
    {
        // "Brakk 18V" is the platform; its "18V" must not also appear as an unused quantity.
        var requirements = Extract("Brakk 18V battery");

        var platform = Assert.Single(requirements.Requirements);
        Assert.Equal(("platform", "brakk-18v"), (platform.AccessorySpec, platform.Value.GetString()));
        Assert.Empty(requirements.UnusedPhrases);
    }
}

using PI.SearchApi.Contracts;
using PI.SearchApi.Pipeline.Ontology;
using Xunit;

namespace PI.SearchApi.Tests.Pipeline.Ontology;

public sealed class ValueVocabularyBuilderTests
{
    private static readonly DomainOntology Ontology = new(Path.Combine(AppContext.BaseDirectory, "assets", "data"));

    private static IReadOnlyList<ValueVocabulary> Build() =>
        ValueVocabularyBuilder.Build(Ontology.Vocabularies, Ontology.Rules);

    [Fact]
    public void Build_ReturnsEveryValueVocabulary_ButNotTheTaxonomy()
    {
        var notations = Build().Select(v => v.Notation);

        Assert.Equal(new[] { "battery-platforms", "connectors", "memory-types", "storage-interfaces" }, notations);
    }

    [Fact]
    public void Build_Connectors_HasNameValuesSynonymsAndBothSpecKeys()
    {
        var connectors = Assert.Single(Build(), v => v.Notation == "connectors");

        Assert.Equal("Connectors", connectors.Label);
        Assert.Equal(new[] { "chargingPort", "connector" }, connectors.Specs);

        var usbC = Assert.Single(connectors.Values, v => v.Notation == "usb-c");
        Assert.Equal("USB-C", usbC.Label);
        Assert.Contains("Type-C", usbC.AltLabels);
    }

    [Fact]
    public void Build_MemoryTypes_SameSpecKeyOnBothSides_IsListedOnce()
    {
        var memoryTypes = Assert.Single(Build(), v => v.Notation == "memory-types");

        Assert.Equal(new[] { "memoryType" }, memoryTypes.Specs);
    }

    [Fact]
    public void Build_VocabularyNoRuleUses_HasNoSpecKeys()
    {
        var connectors = new OntologyVocabulary("connectors", "Connectors", []);

        var result = Assert.Single(ValueVocabularyBuilder.Build([connectors], []));

        Assert.Empty(result.Specs);
    }

    [Fact]
    public void Build_EveryValue_ResolvesToItselfInTheOntology()
    {
        // The filter sends a notation; the rule evaluator and catalog validation must agree it's known.
        foreach (var vocabulary in Build())
        {
            foreach (var value in vocabulary.Values)
            {
                Assert.True(Ontology.TryResolveVocabularyValue(vocabulary.Notation, value.Notation, out var resolved));
                Assert.Equal(value.Notation, resolved);
            }
        }
    }
}

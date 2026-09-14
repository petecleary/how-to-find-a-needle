using PI.SearchApi.Contracts;
using PI.SearchApi.Pipeline.Ontology;
using Xunit;

namespace PI.SearchApi.Tests.Pipeline.Ontology;

public sealed class ConceptClassifierTests
{
    private static readonly ConceptClassifier Classifier = new(new DomainOntology(Path.Combine(AppContext.BaseDirectory, "assets", "data")));

    [Fact]
    public void Classify_NarrowerCategory_IsInConceptWithBroaderChain()
    {
        var result = Classifier.Classify(["laptop-chargers", "usb-c-pd-chargers"], ["chargers"]);

        Assert.Equal(ConceptMatch.InConcept, result.Match);
        Assert.Equal("chargers", result.MatchedConcept);
        Assert.Equal(["laptop-chargers", "chargers"], result.Chain);
    }

    [Fact]
    public void Classify_SameCategory_IsInConceptWithOneStepChain()
    {
        var result = Classifier.Classify(["batteries"], ["drills", "batteries"]);

        Assert.Equal(ConceptMatch.InConcept, result.Match);
        Assert.Equal(["batteries"], result.Chain);
    }

    [Fact]
    public void Classify_PhoneBatteryForDrillBatteryQuery_IsOutOfConcept()
    {
        // GQ-03: phone-batteries sits under Telephony, not under Power tools › Batteries.
        var result = Classifier.Classify(["phone-batteries"], ["drills", "batteries"]);

        Assert.Equal(ConceptMatch.OutOfConcept, result.Match);
        Assert.Empty(result.Chain);
    }

    [Fact]
    public void Classify_NoMatchedConcepts_IsNoConcept()
    {
        Assert.Equal(ConceptMatch.NoConcept, Classifier.Classify(["laptops"], []).Match);
    }

    [Fact]
    public void Classify_BroaderCategoryThanMatchedConcept_IsOutOfConcept()
    {
        // A product filed under "chargers" isn't necessarily a laptop charger: subsumption only goes downwards.
        Assert.Equal(ConceptMatch.OutOfConcept, Classifier.Classify(["chargers"], ["laptop-chargers"]).Match);
    }
}

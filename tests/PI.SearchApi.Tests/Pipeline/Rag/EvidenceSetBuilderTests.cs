using PI.SearchApi.Contracts;
using PI.SearchApi.Pipeline;
using PI.SearchApi.Pipeline.Ontology;
using PI.SearchApi.Pipeline.Rag;
using Xunit;
using static PI.SearchApi.Tests.Pipeline.Rag.RagTestData;

namespace PI.SearchApi.Tests.Pipeline.Rag;

public sealed class EvidenceSetBuilderTests
{
    private static readonly EvidenceSetBuilder Builder = new(DomainModel);

    [Fact]
    public void Build_ManyOfEachStatus_KeepsEachRoleWithinItsLimit()
    {
        // 7 Compatible, 5 Incompatible, 4 Unknown and 7 NotEvaluated → 5, 3, 2 and 5.
        var ranked = Enumerable.Range(1, 7).Select(i => Candidate($"PROD-01{i:00}", CompatibilityStatus.Compatible))
            .Concat(Enumerable.Range(1, 5).Select(i => Candidate($"PROD-02{i:00}", CompatibilityStatus.Incompatible)))
            .Concat(Enumerable.Range(1, 4).Select(i => Candidate($"PROD-03{i:00}", CompatibilityStatus.Unknown)))
            .Concat(Enumerable.Range(1, 7).Select(i => Candidate($"PROD-04{i:00}", CompatibilityStatus.NotEvaluated)))
            .ToList();

        var evidence = Builder.Build(ranked, null, [], []);

        Assert.Equal(5, evidence.Items.Count(i => i.Role == EvidenceRole.Compatible));
        Assert.Equal(3, evidence.Items.Count(i => i.Role == EvidenceRole.Incompatible));
        Assert.Equal(2, evidence.Items.Count(i => i.Role == EvidenceRole.Unknown));
        Assert.Equal(5, evidence.Items.Count(i => i.Role == EvidenceRole.NotChecked));
    }

    [Fact]
    public void Build_KeepsStageFiveOrderAndRecordsEachRank()
    {
        List<Candidate> ranked =
        [
            Candidate("PROD-0012", CompatibilityStatus.Compatible),
            Candidate("PROD-0014", CompatibilityStatus.Incompatible, reasons: "✗ wrong plug"),
            Candidate("PROD-0011", CompatibilityStatus.Compatible),
        ];

        var evidence = Builder.Build(ranked, null, [], []);

        Assert.Equal(["PROD-0012", "PROD-0014", "PROD-0011"], evidence.ProductIds);
        Assert.Equal([1, 2, 3], evidence.Items.Select(i => i.Rank));
        Assert.Equal(["✗ wrong plug"], evidence.Find("PROD-0014")!.Compatibility.Reasons);
    }

    [Fact]
    public void Build_TargetDeviceInResults_ComesFirstAndOnlyOnce()
    {
        var laptop = Product("PROD-0001", "Blackbird Aerobook 14", ["laptops"]);
        List<Candidate> ranked =
        [
            Candidate("PROD-0012", CompatibilityStatus.Compatible),
            new(laptop, 0.4, new CandidateSignals(), CompatibilityResult.NotEvaluated),
        ];

        var evidence = Builder.Build(ranked, laptop, [], []);

        Assert.Equal(["PROD-0001", "PROD-0012"], evidence.ProductIds);
        Assert.Equal(EvidenceRole.TargetDevice, evidence.Items[0].Role);
    }

    [Fact]
    public void Build_OutOfConceptProducts_AreLeftOut()
    {
        // GQ-01's DDR4 modules are Incompatible, but they aren't chargers: not what the shopper asked for.
        List<Candidate> ranked =
        [
            Candidate("PROD-0012", CompatibilityStatus.Compatible),
            Candidate("PROD-0022", CompatibilityStatus.Incompatible, ConceptMatch.OutOfConcept),
        ];

        var evidence = Builder.Build(ranked, null, [], []);

        Assert.Equal(["PROD-0012"], evidence.ProductIds);
    }

    [Fact]
    public void Build_MatchedConcept_HasEnglishLabelsAndDefinitionButNoHiddenLabels()
    {
        var evidence = Builder.Build([], null, ["laptop-chargers"], []);

        var concept = Assert.Single(evidence.Concepts);
        Assert.True(DomainModel.TryGetConcept("laptop-chargers", out var expected));
        Assert.Equal(expected.PrefLabels["en"], concept.PrefLabel);
        Assert.Equal(expected.Definition, concept.Definition);

        var hiddenLabels = DomainModel.Labels
            .Where(l => l.ConceptNotation == "laptop-chargers" && l.Kind == LabelKind.Hidden)
            .Select(l => l.Label);
        Assert.Empty(concept.AltLabels.Intersect(hiddenLabels));
        Assert.All(concept.AltLabels, label => Assert.Contains(DomainModel.Labels, l =>
            l.ConceptNotation == "laptop-chargers" && l.Label == label && l.Kind == LabelKind.Alternative && l.Language == "en"));
    }

    [Fact]
    public void Build_Rules_OnlyForProductsInTheEvidence()
    {
        List<Candidate> ranked = [Candidate("PROD-0012", CompatibilityStatus.Compatible)];
        List<CheckOutcome> checks =
        [
            Check("PROD-0012", "chargers → laptops", "The charger's plug must fit the laptop's charging port."),
            Check("PROD-0012", "chargers → laptops", "The charger must supply at least the power the laptop needs."),
            Check("PROD-0099", "ssds → laptops", "The SSD's interface must match."),
        ];

        var evidence = Builder.Build(ranked, null, [], checks);

        var rule = Assert.Single(evidence.Rules);
        Assert.Equal("chargers → laptops", rule.Name);
        Assert.Equal(2, rule.Definitions.Count);
    }

    private static CheckOutcome Check(string candidateId, string rule, string definition) =>
        new(candidateId, rule, definition, "connector", "USB-C", "equals", "chargingPort", "USB-C", CheckResult.Pass, "✓");
}

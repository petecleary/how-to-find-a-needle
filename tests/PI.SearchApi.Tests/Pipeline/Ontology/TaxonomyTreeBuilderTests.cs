using PI.SearchApi.Contracts;
using PI.SearchApi.Pipeline.Ontology;
using Xunit;

namespace PI.SearchApi.Tests.Pipeline.Ontology;

public sealed class TaxonomyTreeBuilderTests
{
    private static IReadOnlyList<TaxonomyNode> BuildTree() =>
        TaxonomyTreeBuilder.Build(new DomainOntology(Path.Combine(AppContext.BaseDirectory, "assets", "data")).Concepts);

    [Fact]
    public void Build_Roots_AreTopConceptsOnly()
    {
        var roots = BuildTree();

        Assert.Contains(roots, r => r.Notation == "power");
        Assert.Contains(roots, r => r.Notation == "computers");
        Assert.DoesNotContain(roots, r => r.Notation == "chargers");
    }

    [Fact]
    public void Build_Chargers_HasNarrowerLabelsSynonymsAndDefinition()
    {
        var power = Assert.Single(BuildTree(), r => r.Notation == "power");

        var chargers = Assert.Single(power.Narrower, n => n.Notation == "chargers");

        Assert.Equal("Chargers", chargers.Label);
        Assert.Equal("Cargadores", chargers.Labels["es"]);
        Assert.Contains("power brick", chargers.AltLabels);
        Assert.StartsWith("A device that supplies", chargers.Definition);
        Assert.Contains(chargers.Narrower, n => n.Notation == "laptop-chargers");
    }

    [Fact]
    public void Build_Laptops_IsMarkedAsDeviceType()
    {
        var computers = Assert.Single(BuildTree(), r => r.Notation == "computers");

        var laptops = Assert.Single(computers.Narrower, n => n.Notation == "laptops");

        Assert.True(laptops.IsDeviceType);
        Assert.Empty(laptops.Narrower);
    }
}

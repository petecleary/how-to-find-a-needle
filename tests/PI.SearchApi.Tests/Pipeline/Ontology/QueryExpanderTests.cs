using PI.SearchApi.Pipeline.Ontology;
using Xunit;

namespace PI.SearchApi.Tests.Pipeline.Ontology;

public sealed class QueryExpanderTests
{
    private static readonly DomainOntology Ontology = new(Path.Combine(AppContext.BaseDirectory, "assets", "data"));
    private static readonly LabelMatcher Matcher = new(Ontology);
    private static readonly QueryExpander Expander = new(Ontology);

    [Fact]
    public void Expand_PowerBrick_StartsWithThePhraseThenEnglishSynonymsAndNarrowerConcepts()
    {
        var expansion = Expander.Expand(Matcher.Understand("power brick for laptop"));

        var chargers = expansion.Keyword.OrGroups[0];
        Assert.Equal("power brick", chargers[0]);
        Assert.Contains("Chargers", chargers);
        Assert.Contains("AC adapter", chargers);
        Assert.Contains("Laptop chargers", chargers); // a narrower concept's label
        Assert.DoesNotContain("chager", chargers); // hidden labels aren't expanded
    }

    [Fact]
    public void Expand_EveryGroup_IsCappedAtTenTermsPerConcept()
    {
        var expansion = Expander.Expand(Matcher.Understand("power brick for laptop"));

        Assert.All(expansion.Keyword.OrGroups, group => Assert.True(group.Count <= QueryExpander.MaxTermsPerConcept));
        Assert.Equal(QueryExpander.MaxTermsPerConcept, expansion.Keyword.OrGroups[0].Count); // Chargers has more than 10 labels
    }

    [Fact]
    public void Expand_Terms_AreDistinctIgnoringCase()
    {
        var expansion = Expander.Expand(Matcher.Understand("power brick for laptop"));

        Assert.All(expansion.Keyword.OrGroups, group =>
            Assert.Equal(group.Count, group.Distinct(StringComparer.OrdinalIgnoreCase).Count()));
    }

    [Fact]
    public void Expand_OneOrGroupPerCategoryPhrase_WithTheRestOfTheQueryKept()
    {
        var expansion = Expander.Expand(Matcher.Understand("power brick for laptop"));

        Assert.Equal(2, expansion.Keyword.OrGroups.Count);
        Assert.Equal("laptop", expansion.Keyword.OrGroups[1][0]);
        Assert.Equal("for", expansion.Keyword.RemainingText);
    }

    [Fact]
    public void Expand_EmbeddingText_AppendsConceptAndNarrowerNames()
    {
        var expansion = Expander.Expand(Matcher.Understand("power brick for laptop"));

        Assert.StartsWith("power brick for laptop (chargers, ", expansion.EmbeddingText);
        Assert.Contains("laptop chargers", expansion.EmbeddingText);
        Assert.EndsWith("laptops)", expansion.EmbeddingText);
    }

    [Fact]
    public void Expand_SpanishQuery_ExpandsToEnglishTerms()
    {
        // GQ-07: the Spanish phrase expands to English labels the catalog actually uses.
        var expansion = Expander.Expand(Matcher.Understand("cargador USB-C para portátil"));

        Assert.Equal("cargador", expansion.Keyword.OrGroups[0][0]);
        Assert.Contains("Chargers", expansion.Keyword.OrGroups[0]);
        Assert.Contains("Laptops", expansion.Keyword.OrGroups[1]);
    }
}

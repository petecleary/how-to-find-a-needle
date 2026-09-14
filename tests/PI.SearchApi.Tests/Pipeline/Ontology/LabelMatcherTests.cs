using PI.SearchApi.Pipeline.Ontology;
using Xunit;

namespace PI.SearchApi.Tests.Pipeline.Ontology;

public sealed class LabelMatcherTests
{
    private static readonly DomainOntology Ontology = new(Path.Combine(AppContext.BaseDirectory, "assets", "data"));
    private static readonly LabelMatcher Matcher = new(Ontology);

    [Fact]
    public void Understand_PowerBrickForLaptop_MatchesChargersAndLaptops()
    {
        // ADR-0013's example: "power brick" is an altLabel of Chargers; "laptop" folds to match "Laptops".
        var understanding = Matcher.Understand("power brick for laptop");

        Assert.Equal(["chargers", "laptops"], understanding.TaxonomyConcepts);
        Assert.Equal(["power brick", "laptop"], understanding.Matches.Select(m => m.Phrase));
        Assert.Equal("for", understanding.RemainingText);
    }

    [Fact]
    public void Understand_LongestMatchFirst_ClaimsWordsBeforeShorterLabels()
    {
        // "cordless phone battery" (Phone batteries) must win over "battery" (Batteries).
        var understanding = Matcher.Understand("cordless phone battery");

        var match = Assert.Single(understanding.Matches);
        Assert.Equal("cordless phone battery", match.Phrase);
        Assert.Equal(["phone-batteries"], understanding.TaxonomyConcepts);
    }

    [Fact]
    public void Understand_CordlessDrillBattery_MatchesDrillsAndBatteriesNotPhoneBatteries()
    {
        var understanding = Matcher.Understand("cordless drill battery");

        Assert.Equal(["drills", "batteries"], understanding.TaxonomyConcepts);
        Assert.Equal("cordless", understanding.RemainingText);
    }

    [Fact]
    public void Understand_SpanishQuery_MatchesSpanishLabelsWithAccentsFolded()
    {
        // GQ-07: "cargador" is a Spanish altLabel of Chargers; "portátil" of Laptops. "USB-C" is a value concept.
        var understanding = Matcher.Understand("cargador USB-C para portátil");

        Assert.Equal(["chargers", "laptops"], understanding.TaxonomyConcepts);
        var usbC = Assert.Single(understanding.Matches, m => m.Phrase == "USB-C");
        Assert.False(usbC.IsTaxonomyMatch);
        Assert.Equal("usb-c", usbC.Labels[0].ConceptNotation);

        // Value phrases stay in the remaining text; only category phrases are removed (ADR-0013).
        Assert.Equal("USB-C para", understanding.RemainingText);
    }

    [Fact]
    public void Understand_HiddenLabelMisspelling_StillMatches()
    {
        var understanding = Matcher.Understand("chager");

        Assert.Equal(["chargers"], understanding.TaxonomyConcepts);
        Assert.Equal(LabelKind.Hidden, Assert.Single(understanding.Matches).Labels[0].Kind);
    }

    [Theory]
    [InlineData("chargers", "chargers")]
    [InlineData("CHARGER", "chargers")]
    [InlineData("batteries", "batteries")]
    [InlineData("ssd", "ssds")]
    [InlineData("Portátiles", "laptops")]
    public void Understand_PluralsCaseAndAccents_FoldToTheSameConcept(string query, string expectedConcept)
    {
        Assert.Equal([expectedConcept], Matcher.Understand(query).TaxonomyConcepts);
    }

    [Fact]
    public void Understand_PartialPhrase_DoesNotMatch()
    {
        // Lexical matching is brittle, and the trace says so: "brick" alone isn't a label.
        var understanding = Matcher.Understand("brick");

        Assert.Empty(understanding.Matches);
        Assert.Equal("brick", understanding.RemainingText);
    }

    [Fact]
    public void Understand_NoLabels_ReturnsNoConceptsAndTheWholeQuery()
    {
        var understanding = Matcher.Understand("charger for my Blackbird Aerobook 14");

        Assert.Equal(["chargers"], understanding.TaxonomyConcepts);
        Assert.Equal("for my Blackbird Aerobook 14", understanding.RemainingText);
    }
}

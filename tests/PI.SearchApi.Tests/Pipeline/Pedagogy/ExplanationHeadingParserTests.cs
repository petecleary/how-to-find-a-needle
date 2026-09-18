using PI.SearchApi.Pipeline.Pedagogy;
using Xunit;

namespace PI.SearchApi.Tests.Pipeline.Pedagogy;

public sealed class ExplanationHeadingParserTests
{
    private const string Complete = """
        ## Decision
        Get the Voltline 65W USB-C GaN Charger [PROD-0012].

        ## Concepts
        - **Connector**: the plug must fit your laptop's port.
        - **Wattage:** the charger must supply at least 65W.

        ## Near miss
        The Voltline 45W Barrel Charger [PROD-0014] looks the same, but has the wrong plug.

        ## Rule of thumb
        Match the plug, then match or beat the wattage.

        ## Next step
        Check the W rating on your current charger.
        """;

    [Fact]
    public void Parse_CompleteExplanation_FindsTheFiveHeadingsInOrder()
    {
        var sections = ExplanationHeadingParser.Parse(Complete);

        Assert.Equal(ExplanationHeadingParser.Headings, sections.Select(s => s.Heading));
        Assert.All(sections, s => Assert.True(s.IsKnown));
    }

    [Fact]
    public void ToStructure_CompleteExplanation_ReadsDecisionConceptsNearMissAndSentences()
    {
        var structure = ExplanationHeadingParser.ToStructure(ExplanationHeadingParser.Parse(Complete));

        Assert.Equal("PROD-0012", structure.Decision.ProductId);
        Assert.Equal(["Connector", "Wattage"], structure.Concepts);
        Assert.Equal("PROD-0014", structure.NearMiss.ProductId);
        Assert.Equal("Match the plug, then match or beat the wattage.", structure.RuleOfThumb);
        Assert.Equal("Check the W rating on your current charger.", structure.NextStep);
    }

    [Fact]
    public void ToStructure_BoldWordsLaterInABullet_AreEmphasisNotConcepts()
    {
        // Real qwen3.6:35b output for GQ-03, novice (2026-09-15): **USB-C** and **65W** emphasise values mid-sentence.
        const string markdown = """
            ## Concepts
            *   **Connector**: This is the plug shape. Your Blackbird Aerobook 14 uses a **USB-C** port.
            *   **Wattage (Power)**: Your laptop needs at least **65W**.
            """;

        var structure = ExplanationHeadingParser.ToStructure(ExplanationHeadingParser.Parse(markdown));

        Assert.Equal(["Connector", "Wattage (Power)"], structure.Concepts);
    }

    [Fact]
    public void Parse_MissingHeading_LeavesItOut()
    {
        var sections = ExplanationHeadingParser.Parse(Complete.Replace("## Rule of thumb\nMatch the plug, then match or beat the wattage.\n", ""));

        Assert.DoesNotContain(sections, s => s.Heading == ExplanationHeadingParser.RuleOfThumb);
        Assert.Null(ExplanationHeadingParser.ToStructure(sections).RuleOfThumb);
    }

    [Fact]
    public void Parse_OutOfOrderHeadings_KeepsTheOrderWritten()
    {
        const string markdown = "## Near miss\nNone.\n\n## Decision\nGet [PROD-0012].";

        var sections = ExplanationHeadingParser.Parse(markdown);

        Assert.Equal([ExplanationHeadingParser.NearMiss, ExplanationHeadingParser.Decision], sections.Select(s => s.Heading));
    }

    [Theory]
    [InlineData("### decision")]
    [InlineData("**Decision**")]
    [InlineData("**Decision:**")]
    [InlineData("# Decision:")]
    public void Parse_HeadingMarkupVariants_AreRecognised(string headingLine)
    {
        var sections = ExplanationHeadingParser.Parse($"{headingLine}\nGet [PROD-0012].");

        var section = Assert.Single(sections);
        Assert.Equal(ExplanationHeadingParser.Decision, section.Heading);
        Assert.Equal("Get [PROD-0012].", section.Body);
    }

    [Fact]
    public void Parse_BoldTermInsideABullet_IsNotAHeading()
    {
        var sections = ExplanationHeadingParser.Parse("## Concepts\n**Connector**: the plug.\n**Wattage**");

        var section = Assert.Single(sections);
        Assert.Equal(ExplanationHeadingParser.Concepts, section.Heading);
        Assert.Contains("**Wattage**", section.Body);
    }
}

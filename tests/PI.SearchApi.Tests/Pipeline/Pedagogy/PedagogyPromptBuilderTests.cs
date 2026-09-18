using System.Text.RegularExpressions;
using PI.SearchApi.Pipeline;
using PI.SearchApi.Pipeline.Ontology;
using PI.SearchApi.Pipeline.Pedagogy;
using PI.SearchApi.Pipeline.Rag;
using Xunit;
using static PI.SearchApi.Tests.Pipeline.Rag.RagTestData;

namespace PI.SearchApi.Tests.Pipeline.Pedagogy;

public sealed class PedagogyPromptBuilderTests
{
    private const string Answer = "The Voltline 65W USB-C GaN Charger [PROD-0012] fits.";

    private static readonly PedagogyPromptBuilder Builder = new(new PromptLibrary(Path.Combine(AppContext.BaseDirectory, "assets", "prompts")));

    [Fact]
    public void Build_ApplyPedagogyToggle_ChangesOnlyTheSystemPrompt()
    {
        // A fair demo changes one thing: the facts, audience and words in the user message are identical.
        var pedagogy = Builder.Build("power adapter for my laptop", "novice", applyPedagogy: true, Gq03Evidence(), Answer);
        var baseline = Builder.Build("power adapter for my laptop", "novice", applyPedagogy: false, Gq03Evidence(), Answer);

        Assert.Equal(pedagogy.UserPrompt, baseline.UserPrompt);
        Assert.NotEqual(pedagogy.SystemPrompt, baseline.SystemPrompt);
        Assert.Equal(PedagogyPromptBuilder.SystemPromptFile, pedagogy.SystemPromptFile);
        Assert.Equal(PedagogyPromptBuilder.BaselinePromptFile, baseline.SystemPromptFile);
    }

    [Fact]
    public void Build_PedagogyOn_HasTheFiveHeadingsAndTheActiveAudienceGuidance()
    {
        var prompt = Builder.Build("q", "expert", applyPedagogy: true, Gq03Evidence(), Answer);

        foreach (var heading in ExplanationHeadingParser.Headings)
        {
            Assert.Contains($"## {heading}", prompt.SystemPrompt);
        }

        Assert.Contains("spec-first", prompt.SystemPrompt);
        Assert.DoesNotContain("One short analogy is allowed", prompt.SystemPrompt);
        Assert.Equal(Builder.AudienceGuidance("expert"), prompt.AudienceGuidance);
    }

    [Fact]
    public void Build_Baseline_NamesTheAudienceWithoutHeadingsOrGuidance()
    {
        var prompt = Builder.Build("q", "novice", applyPedagogy: false, Gq03Evidence(), Answer);

        Assert.Contains("novice", prompt.SystemPrompt);
        Assert.DoesNotContain("## Decision", prompt.SystemPrompt);
        Assert.Null(prompt.AudienceGuidance);
        Assert.DoesNotContain("{{", prompt.SystemPrompt + prompt.UserPrompt);
    }

    [Fact]
    public void Build_Novice_OffersEverydayAltLabelsAndTheProperName()
    {
        var prompt = Builder.Build("q", "novice", applyPedagogy: true, Gq03Evidence(), Answer);

        Assert.Contains("- Laptop chargers: everyday words: power adapter, power brick; proper name to give once: Laptop chargers", prompt.UserPrompt);
        Assert.Equal(["power adapter", "power brick", "Laptop chargers"], prompt.WordsOffered["laptop-chargers"]);
    }

    [Fact]
    public void Build_Expert_OffersPreferredLabelAndSpecTermsButNoAltLabels()
    {
        var prompt = Builder.Build("q", "expert", applyPedagogy: true, Gq03Evidence(), Answer);

        Assert.DoesNotContain("power brick", prompt.UserPrompt);
        Assert.Contains("- Spec terms from the rules: connector, charging port, wattage, min charger wattage", prompt.UserPrompt);
        Assert.Equal(["Laptop chargers"], prompt.WordsOffered["laptop-chargers"]);
    }

    [Theory]
    [InlineData("novice", true)]
    [InlineData("enthusiast", true)]
    [InlineData("expert", true)]
    [InlineData("novice", false)]
    public void Build_RealOntologyConcepts_NeverIncludeHiddenLabels(string audience, bool applyPedagogy)
    {
        string[] concepts = ["chargers", "laptop-chargers", "laptops", "batteries", "ssds"];
        var evidence = new EvidenceSetBuilder(DomainModel).Build([], null, concepts, []);

        var prompt = Builder.Build("q", audience, applyPedagogy, evidence, Answer);

        var hiddenLabels = DomainModel.Labels
            .Where(l => concepts.Contains(l.ConceptNotation) && l.Kind == LabelKind.Hidden)
            .Select(l => l.Label)
            .ToList();
        Assert.NotEmpty(hiddenLabels);
        Assert.All(hiddenLabels, hidden =>
            Assert.DoesNotMatch($@"(?i)\b{Regex.Escape(hidden)}\b", prompt.SystemPrompt + prompt.UserPrompt));
    }

    [Theory]
    [InlineData("minChargerWattageW", "min charger wattage")]
    [InlineData("chargingPort", "charging port")]
    [InlineData("connector", "connector")]
    [InlineData("capacityAh", "capacity")]
    public void HumaniseSpecTerm_SpecName_ReadsAsWords(string specName, string expected)
    {
        Assert.Equal(expected, PedagogyPromptBuilder.HumaniseSpecTerm(specName));
    }
}

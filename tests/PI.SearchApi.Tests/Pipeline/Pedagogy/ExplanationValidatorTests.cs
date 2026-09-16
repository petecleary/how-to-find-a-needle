using PI.SearchApi.Contracts;
using PI.SearchApi.Pipeline.Pedagogy;
using PI.SearchApi.Pipeline.Rag;
using Xunit;
using static PI.SearchApi.Tests.Pipeline.Rag.RagTestData;

namespace PI.SearchApi.Tests.Pipeline.Pedagogy;

public sealed class ExplanationValidatorTests
{
    private static string Explanation(
        string decision = "Get the Voltline 65W USB-C GaN Charger [PROD-0012].",
        string concepts = "- **Connector**: the plug must fit the charging port.\n- **Wattage**: at least 65W.",
        string nearMiss = "The Voltline 45W Barrel Charger [PROD-0014] looks the same, but the plug is wrong.") => $"""
        ## Decision
        {decision}

        ## Concepts
        {concepts}

        ## Near miss
        {nearMiss}

        ## Rule of thumb
        Match the plug, then the wattage.

        ## Next step
        Check your current charger's W rating.
        """;

    [Fact]
    public void Validate_WellFormedExplanation_PassesEveryCheck()
    {
        var validation = ExplanationValidator.Validate(Explanation(), Gq01Evidence(), applyPedagogy: true, answerFoundInsufficientEvidence: false);

        Assert.Empty(validation.Warnings);
        Assert.All(validation.Checks, check => Assert.True(check.Passed, check.Detail));
        Assert.Equal("PROD-0012", validation.Structure!.Decision.ProductId);
    }

    [Fact]
    public void Validate_DecisionRecommendsTheNearMiss_Warns()
    {
        var validation = ExplanationValidator.Validate(
            Explanation(decision: "Get the Voltline 45W Barrel Charger [PROD-0014]."), Gq01Evidence(), applyPedagogy: true, answerFoundInsufficientEvidence: false);

        Assert.Contains(validation.Warnings, w => w.Contains("Decision recommends PROD-0014, which is Incompatible"));
    }

    [Fact]
    public void Validate_DecisionCitesTwoProducts_Warns()
    {
        var validation = ExplanationValidator.Validate(
            Explanation(decision: "Get [PROD-0012] or [PROD-0014]."), Gq01Evidence(), applyPedagogy: true, answerFoundInsufficientEvidence: false);

        Assert.Contains(validation.Warnings, w => w.Contains("it should choose exactly one"));
    }

    [Fact]
    public void Validate_DecisionAndNearMissAlsoCiteTheTargetDevice_StillPass()
    {
        // Bake-off finding (2026-09-15): "for your Aerobook [PROD-0001]" is context, not a second choice.
        var validation = ExplanationValidator.Validate(
            Explanation(
                decision: "For your laptop [PROD-0001], get the Voltline 65W USB-C GaN Charger [PROD-0012].",
                nearMiss: "The Voltline 45W Barrel Charger [PROD-0014] won't fit your laptop [PROD-0001]."),
            Gq01Evidence(), applyPedagogy: true, answerFoundInsufficientEvidence: false);

        Assert.Empty(validation.Warnings);
    }

    [Fact]
    public void Validate_InsufficientEvidenceAndDecisionCitesNothing_Passes()
    {
        var validation = ExplanationValidator.Validate(
            Explanation(decision: "No suitable product was found."), Gq01Evidence(), applyPedagogy: true, answerFoundInsufficientEvidence: true);

        Assert.DoesNotContain(validation.Warnings, w => w.Contains("Decision"));
    }

    [Fact]
    public void Validate_NearMissCitesACompatibleProduct_Warns()
    {
        var validation = ExplanationValidator.Validate(
            Explanation(nearMiss: "The Voltline 65W [PROD-0012] is similar."), Gq01Evidence(), applyPedagogy: true, answerFoundInsufficientEvidence: false);

        Assert.Contains(validation.Warnings, w => w.Contains("Near miss cites PROD-0012, which is not Incompatible"));
    }

    [Fact]
    public void Validate_MissingAndOutOfOrderHeadings_Warn()
    {
        const string markdown = "## Concepts\n- **Connector**: fits.\n\n## Decision\nGet [PROD-0012].";

        var validation = ExplanationValidator.Validate(markdown, Gq01Evidence(), applyPedagogy: true, answerFoundInsufficientEvidence: false);

        Assert.Contains(validation.Warnings, w => w.StartsWith("Missing heading(s): Near miss, Rule of thumb, Next step", StringComparison.Ordinal));
        Assert.Contains(validation.Warnings, w => w.StartsWith("Headings are out of order", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_ConceptNotFromTheOntology_WarnsAsAHeuristic()
    {
        var validation = ExplanationValidator.Validate(
            Explanation(concepts: "- **Connector**: fits.\n- **Brand loyalty**: stick with one maker."), Gq01Evidence(), applyPedagogy: true, answerFoundInsufficientEvidence: false);

        var warning = Assert.Single(validation.Warnings);
        Assert.StartsWith("Heuristic: the concept \"Brand loyalty\"", warning);
        Assert.Contains(validation.Checks, c => c.IsHeuristic && !c.Passed);
    }

    [Fact]
    public void Validate_NoTargetDevice_ConceptHeuristicStillKnowsTheRuleWords()
    {
        // Bake-off finding (2026-09-15), GQ-03: with no device no rule is checked, so the evidence has no rules, but the
        // Unknown reason still quotes the rule. "Battery platform" must not be flagged as "not from the ontology".
        var evidence = new EvidenceSet(
            [
                new EvidenceItem(
                    Product("PROD-0026", "Brakk 18V 4Ah Battery", ["batteries"]),
                    null,
                    new CompatibilityResult(CompatibilityStatus.Unknown,
                        ["? No target device: choose one, or name it in the query, to check that the battery must be built for the same cordless platform as the tool, whatever the voltage printed on the box."]),
                    ConceptMatch.InConcept, 1, EvidenceRole.Unknown, "#1"),
            ],
            [],
            []);

        var validation = ExplanationValidator.Validate(
            Explanation(decision: "No suitable product was found.", concepts: "- **Battery platform**: brands don't mix.\n- **Voltage**: printed on the box.", nearMiss: "None"),
            evidence, applyPedagogy: true, answerFoundInsufficientEvidence: true);

        Assert.DoesNotContain(validation.Warnings, w => w.StartsWith("Heuristic:", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_ConceptNamedAfterAProductSpec_IsNotFlagged()
    {
        // Bake-off finding (2026-09-15): "Capacity (Ah)" and "Form Factor" are spec names from the catalogue the model
        // was given, so explaining them is grounded, even though they aren't taxonomy concepts.
        var evidence = new EvidenceSet(
            [
                new EvidenceItem(
                    Product("PROD-0026", "Brakk 18V 4Ah Battery", ["batteries"], """{"capacityAh":4,"formFactor":"slide"}"""),
                    null, new CompatibilityResult(CompatibilityStatus.NotEvaluated, []),
                    ConceptMatch.InConcept, 1, EvidenceRole.NotChecked, "#1"),
            ],
            [],
            []);

        var validation = ExplanationValidator.Validate(
            Explanation(
                decision: "No suitable product was found.",
                concepts: "- **Capacity (Ah)**: how long it runs.\n- **Form Factor**: how it clips on.",
                nearMiss: "None"),
            evidence, applyPedagogy: true, answerFoundInsufficientEvidence: true);

        Assert.DoesNotContain(validation.Warnings, w => w.StartsWith("Heuristic:", StringComparison.Ordinal));
    }

    [Fact]
    public void Validate_Baseline_ChecksOnlyCitationsAndHasNoStructure()
    {
        const string freeForm = "The charger you want is the Voltline 65W [PROD-0012]. It has the right plug, and [PROD-0099] too.";

        var validation = ExplanationValidator.Validate(freeForm, Gq01Evidence(), applyPedagogy: false, answerFoundInsufficientEvidence: false);

        Assert.Null(validation.Structure);
        Assert.Equal(["Cited PROD-0099, which was not in the evidence."], validation.Warnings);
        Assert.Contains(validation.Checks, c => c.Detail.StartsWith("Not applied (baseline prompt)", StringComparison.Ordinal));
    }
}

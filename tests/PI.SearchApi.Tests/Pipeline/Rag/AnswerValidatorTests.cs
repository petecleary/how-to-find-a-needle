using PI.SearchApi.Pipeline.Rag;
using Xunit;
using static PI.SearchApi.Tests.Pipeline.Rag.RagTestData;

namespace PI.SearchApi.Tests.Pipeline.Rag;

public sealed class AnswerValidatorTests
{
    [Fact]
    public void Validate_GroundedAnswerThatWarnsAboutTheNearMiss_HasNoWarnings()
    {
        const string answer = "The Voltline 65W USB-C GaN Charger [PROD-0012] fits your laptop.\n\nAvoid the Voltline 45W Barrel Charger [PROD-0014]: wrong plug and too little power.";

        var validation = AnswerValidator.Validate(answer, Gq01Evidence());

        Assert.Empty(validation.Warnings);
        Assert.Equal(["PROD-0012", "PROD-0014"], validation.Citations);
        Assert.False(validation.InsufficientEvidence);
    }

    [Fact]
    public void Validate_CitesAProductNotInTheEvidence_WarnsAndListsItAsInvalid()
    {
        var validation = AnswerValidator.Validate("Try the Voltline 90W [PROD-0099].", Gq01Evidence());

        Assert.Equal(["PROD-0099"], validation.InvalidCitations);
        Assert.Contains("Cited PROD-0099, which was not in the evidence.", validation.Warnings);
    }

    [Fact]
    public void Validate_NoCitations_Warns()
    {
        var validation = AnswerValidator.Validate("Any 65W USB-C charger will work.", Gq01Evidence());

        Assert.Contains(validation.Warnings, w => w.Contains("cites no products"));
    }

    [Theory]
    [InlineData("INSUFFICIENT_EVIDENCE\nThe evidence has no chargers for this laptop.")]
    [InlineData("  **INSUFFICIENT_EVIDENCE**\r\nNothing matches.")]
    public void Validate_SentinelFirstLine_IsInsufficientEvidenceWithoutAMissingCitationWarning(string answer)
    {
        var validation = AnswerValidator.Validate(answer.ReplaceLineEndings("\n"), Gq01Evidence());

        Assert.True(validation.InsufficientEvidence);
        Assert.Empty(validation.Warnings);
    }

    [Fact]
    public void Validate_BulletsUnderAWarningLeadIn_AreTreatedAsWarnings()
    {
        // Real qwen3.6:35b output for GQ-01 (2026-09-15): the bullets only state reasons, but the list is introduced as warnings.
        const string answer = """
            Your Blackbird Aerobook 14 [PROD-0001] charges from the **Voltline 65W USB-C GaN Charger** [PROD-0012].

            Avoid these incompatible options:
            *   **Voltline 45W Barrel Charger** [PROD-0014] has a barrel plug and supplies 45W, where 65W is required.
            """;

        var validation = AnswerValidator.Validate(answer, Gq01Evidence());

        Assert.Empty(validation.Warnings);
    }

    [Fact]
    public void Validate_BulletsUnderANeutralLeadIn_AreStillChecked()
    {
        const string answer = """
            Good choices:
            - [PROD-0012] is compact.
            - [PROD-0014] is cheap and quiet.
            """;

        var validation = AnswerValidator.Validate(answer, Gq01Evidence());

        Assert.Contains(validation.Warnings, w => w.Contains("PROD-0014"));
    }

    [Theory]
    [InlineData("The Voltline 45W Barrel Charger [PROD-0014] fails because it uses a barrel plug and supplies insufficient power.", false)]
    [InlineData("Avoid the Voltline 45W Barrel Charger [PROD-0014].", false)]
    [InlineData("The 45W barrel charger [PROD-0014] won't fit: it has the wrong plug.", false)]
    [InlineData("- [PROD-0014] is not compatible with your laptop.", false)]
    [InlineData("The Voltline 45W Barrel Charger [PROD-0014] is a great budget choice.", true)]
    public void Validate_SentenceCitingIncompatibleProduct_WarnsOnlyWithoutWarningLanguage(string answer, bool expectWarning)
    {
        // Talk moment: the model must warn about the near miss, never recommend it. A heuristic, labelled as one.
        var validation = AnswerValidator.Validate("[PROD-0012] fits. " + answer, Gq01Evidence());

        Assert.Equal(expectWarning, validation.Warnings.Any(w => w.StartsWith("Heuristic:", StringComparison.Ordinal)));
        Assert.Contains(validation.Checks, c => c.IsHeuristic);
    }
}

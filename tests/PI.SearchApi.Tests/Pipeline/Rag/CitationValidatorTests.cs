using PI.SearchApi.Pipeline.Rag;
using Xunit;

namespace PI.SearchApi.Tests.Pipeline.Rag;

public sealed class CitationValidatorTests
{
    [Fact]
    public void Validate_AllCitationsInEvidence_HasNoUnknownIds()
    {
        var check = CitationValidator.Validate("Get the Voltline [PROD-0012]; avoid the barrel charger [PROD-0014].", ["PROD-0012", "PROD-0014"]);

        Assert.Equal(["PROD-0012", "PROD-0014"], check.Citations);
        Assert.Empty(check.UnknownIds);
    }

    [Fact]
    public void Validate_CitesAProductNotInEvidence_ReportsIt()
    {
        var check = CitationValidator.Validate("Try [PROD-0099].", ["PROD-0012"]);

        Assert.Equal(["PROD-0099"], check.UnknownIds);
    }

    [Fact]
    public void Validate_NoCitations_ReturnsEmptyLists()
    {
        var check = CitationValidator.Validate("Any USB-C charger will do.", ["PROD-0012"]);

        Assert.Empty(check.Citations);
        Assert.Empty(check.UnknownIds);
    }

    [Fact]
    public void Extract_GroupedAndRepeatedCitations_ReturnsEachIdOnceInOrder()
    {
        var ids = CitationValidator.Extract("Both fit [PROD-0012, PROD-0011]. The first [PROD-0012] is smaller.");

        Assert.Equal(["PROD-0012", "PROD-0011"], ids);
    }

    [Fact]
    public void Extract_UnbracketedId_IsNotACitation()
    {
        Assert.Empty(CitationValidator.Extract("PROD-0012 fits."));
    }
}

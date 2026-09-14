using Xunit;

namespace PI.SearchApi.IntegrationTests.GoldenQueries;

[Collection(AppHostCollection.Name)]
public sealed class KeywordStageGoldenQueryTests(AppHostFixture fixture)
{
    [Fact]
    public async Task GQ01_Keyword_FindsTheOfficialChargerByItsExactWords()
    {
        // Talk moment: keyword search is great when the catalog uses the same words as the shopper.
        await GoldenQueryRunner.RunAsync(fixture, "GQ-01", "keyword");
    }

    [Fact]
    public async Task GQ02_Keyword_MissesChargersBecauseNobodySaysPowerBrick()
    {
        // Talk moment: the synonym miss — "power brick" appears nowhere in the catalog, so the chargers never match.
        var response = await GoldenQueryRunner.RunAsync(fixture, "GQ-02", "keyword");

        var step = Assert.Single(response.DebugTrace.Steps);
        Assert.Contains("brick", step.Details!["tsquery"].GetString());
    }

    [Fact]
    public async Task GQ03_Keyword_RanksTheCordlessPhoneBatteryHighly()
    {
        // Talk moment: the keyword trap — a cordless phone battery shares every word with "cordless drill battery".
        var response = await GoldenQueryRunner.RunAsync(fixture, "GQ-03", "keyword");

        var step = Assert.Single(response.DebugTrace.Steps);
        Assert.Contains("'batteri'", step.Details!["tsquery"].GetString()); // stemming is visible in the trace
    }

    [Fact]
    public async Task GQ07_Keyword_FindsNothingForASpanishQuery()
    {
        // Talk moment: English stemming over an English catalog shares no words with "cargador USB-C para portátil".
        await GoldenQueryRunner.RunAsync(fixture, "GQ-07", "keyword");
    }
}

using Xunit;

namespace PI.SearchApi.IntegrationTests.GoldenQueries;

[Collection(AppHostCollection.Name)]
public sealed class HybridStageGoldenQueryTests(AppHostFixture fixture)
{
    [Fact]
    public async Task GQ01_Hybrid_StillRanksTheIncompatibleChargerHighly()
    {
        // Talk moment: fusion improves relevance, not correctness — the 45W barrel charger is still near the top.
        RepositoryPaths.SkipUnlessNomicModelIsPresent();

        var response = await GoldenQueryRunner.RunAsync(fixture, "GQ-01", "hybrid");

        // The trace shows the whole pipeline: keyword, then vector (embed + search), then fusion.
        Assert.Equal(["keyword", "vector", "vector", "hybrid"], response.DebugTrace.Steps.Select(s => s.Stage));
        Assert.Contains("1/(60+", response.DebugTrace.Steps[^1].Details!["formulas"].ToString());
    }

    [Fact]
    public async Task GQ02_Hybrid_KeepsTheVectorHitForPowerBrick()
    {
        // Talk moment: keyword found nothing, but fusion keeps vector search's laptop chargers.
        RepositoryPaths.SkipUnlessNomicModelIsPresent();

        await GoldenQueryRunner.RunAsync(fixture, "GQ-02", "hybrid");
    }

    [Fact]
    public async Task GQ03_Hybrid_PutsTheDrillBatteryAboveTheKeywordTrap()
    {
        // Talk moment: fusion lifts the drill battery above the phone battery, but a keyword #1 survives RRF
        // in the top 3 — removing it is the ontology's job.
        RepositoryPaths.SkipUnlessNomicModelIsPresent();

        await GoldenQueryRunner.RunAsync(fixture, "GQ-03", "hybrid");
    }
}

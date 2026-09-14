using Xunit;

namespace PI.SearchApi.IntegrationTests.GoldenQueries;

[Collection(AppHostCollection.Name)]
public sealed class VectorStageGoldenQueryTests(AppHostFixture fixture)
{
    [Fact]
    public async Task GQ01_Vector_RanksTheIncompatibleBarrelChargerHighly()
    {
        // Talk moment: vector search ranks the 45W barrel charger highly — similarity ≠ compatibility.
        RepositoryPaths.SkipUnlessNomicModelIsPresent();

        var response = await GoldenQueryRunner.RunAsync(fixture, "GQ-01", "vector");

        Assert.Equal(["vector", "vector"], response.DebugTrace.Steps.Select(s => s.Stage));
        Assert.Contains("<=>", response.DebugTrace.Steps[1].Sql);
    }

    [Fact]
    public async Task GQ02_Vector_FindsLaptopChargersForPowerBrick()
    {
        // Talk moment: the synonym miss is rescued by meaning — "power brick" lands near "laptop power adapter".
        RepositoryPaths.SkipUnlessNomicModelIsPresent();

        await GoldenQueryRunner.RunAsync(fixture, "GQ-02", "vector");
    }

    [Fact]
    public async Task GQ03_Vector_PutsTheDrillBatteryAboveThePhoneBattery()
    {
        // Talk moment: a cordless phone battery shares the words but not the meaning of "cordless drill battery".
        RepositoryPaths.SkipUnlessNomicModelIsPresent();

        await GoldenQueryRunner.RunAsync(fixture, "GQ-03", "vector");
    }

    [Fact]
    public async Task GQ05_Vector_RanksTheOtherPlatformsBatteryHighly()
    {
        // Talk moment: a Tornio 20V MAX battery reads almost exactly like a Brakk 18V one.
        RepositoryPaths.SkipUnlessNomicModelIsPresent();

        await GoldenQueryRunner.RunAsync(fixture, "GQ-05", "vector");
    }

    [Fact]
    public async Task GQ06_Vector_RanksTheSataSsdHighly()
    {
        // Talk moment: a SATA M.2 2280 SSD reads almost identically to the NVMe drive the laptop needs.
        RepositoryPaths.SkipUnlessNomicModelIsPresent();

        await GoldenQueryRunner.RunAsync(fixture, "GQ-06", "vector");
    }
}

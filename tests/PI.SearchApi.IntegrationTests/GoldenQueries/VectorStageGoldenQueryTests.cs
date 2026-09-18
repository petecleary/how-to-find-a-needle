using Xunit;

namespace PI.SearchApi.IntegrationTests.GoldenQueries;

[Collection(AppHostCollection.Name)]
public sealed class VectorStageGoldenQueryTests(AppHostFixture fixture)
{
    [Fact]
    public async Task GQ03_Vector_RanksTheIncompatibleBarrelChargerHighly()
    {
        // Talk moment: vector search ranks the 45W barrel charger highly — similarity ≠ compatibility.
        RepositoryPaths.SkipUnlessNomicModelIsPresent();

        var response = await GoldenQueryRunner.RunAsync(fixture, "GQ-03", "vector");

        Assert.Equal(["vector", "vector"], response.DebugTrace.Steps.Select(s => s.Stage));
        Assert.Contains("<=>", response.DebugTrace.Steps[1].Sql);
    }

    [Fact]
    public async Task GQ02_Vector_FindsTheChargerButRanksPowerBanksAbove()
    {
        // Talk moment: meaning finds the charger that keyword search missed, but "power brick" lands nearer "power bank".
        RepositoryPaths.SkipUnlessNomicModelIsPresent();

        await GoldenQueryRunner.RunAsync(fixture, "GQ-02", "vector");
    }

    [Fact]
    public async Task GQ04_Vector_PutsTheDrillBatteryAboveThePhoneBattery()
    {
        // Talk moment: a cordless phone battery shares the words but not the meaning of "cordless drill battery".
        RepositoryPaths.SkipUnlessNomicModelIsPresent();

        await GoldenQueryRunner.RunAsync(fixture, "GQ-04", "vector");
    }

    [Fact]
    public async Task GQ05_Vector_RanksTheOtherPlatformsBatteryHighly()
    {
        // Talk moment: a Tornio 20V MAX battery reads almost exactly like a Brakk 18V one.
        RepositoryPaths.SkipUnlessNomicModelIsPresent();

        await GoldenQueryRunner.RunAsync(fixture, "GQ-05", "vector");
    }

    [Fact]
    public async Task GQ08_Vector_RanksTheNamedLaptopAndBrandAccessoriesAboveTheChargers()
    {
        // Talk moment: a device name is context, not intent — but vector search can't tell, so Blackbird things win.
        RepositoryPaths.SkipUnlessNomicModelIsPresent();

        await GoldenQueryRunner.RunAsync(fixture, "GQ-08", "vector");
    }

    [Fact]
    public async Task GQ06_Vector_RanksTheSataSsdHighly()
    {
        // Talk moment: a SATA M.2 2280 SSD reads almost identically to the NVMe drive the laptop needs.
        RepositoryPaths.SkipUnlessNomicModelIsPresent();

        await GoldenQueryRunner.RunAsync(fixture, "GQ-06", "vector");
    }
}

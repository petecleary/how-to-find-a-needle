using System.Text.Json.Nodes;
using PI.SearchApi.IntegrationTests.SearchApi;
using Xunit;

namespace PI.SearchApi.IntegrationTests.GoldenQueries;

[Collection(AppHostCollection.Name)]
public sealed class OntologyStageGoldenQueryTests(AppHostFixture fixture)
{
    [Fact]
    public async Task GQ01_Ontology_FlagsNearMissWithReason()
    {
        // Talk moment: the 45W barrel charger is flagged Incompatible on connector and wattage — similarity ≠ compatibility.
        RepositoryPaths.SkipUnlessNomicModelIsPresent();

        var response = await GoldenQueryRunner.RunAsync(fixture, "GQ-01", "ontology");

        var nearMiss = Assert.Single(response.Results, r => r.Id == "PROD-0014");
        Assert.Contains(nearMiss.Compatibility.Reasons, r => r.Contains("plug"));
        Assert.Contains(nearMiss.Compatibility.Reasons, r => r.Contains("power"));
    }

    [Fact]
    public async Task GQ02_Ontology_ExpansionRescuesTheKeywordSide()
    {
        // Talk moment: "power brick" expands to "power adapter" and "laptop chargers", so keyword search finds the charger too.
        RepositoryPaths.SkipUnlessNomicModelIsPresent();

        await GoldenQueryRunner.RunAsync(fixture, "GQ-02", "ontology");
    }

    [Fact]
    public async Task GQ03_Ontology_MarksThePhoneBatteryOutOfConcept()
    {
        // Talk moment: the cordless phone battery is under Telephony, not Power tools › Batteries.
        RepositoryPaths.SkipUnlessNomicModelIsPresent();

        await GoldenQueryRunner.RunAsync(fixture, "GQ-03", "ontology");
    }

    [Fact]
    public async Task GQ05_Ontology_RejectsTheOtherPlatformsBattery()
    {
        // Talk moment: a Tornio 20V MAX battery doesn't fit a Brakk 18V drill, whatever the numbers say.
        RepositoryPaths.SkipUnlessNomicModelIsPresent();

        await GoldenQueryRunner.RunAsync(fixture, "GQ-05", "ontology");
    }

    [Fact]
    public async Task GQ06_Ontology_FlagsTheSataSsdForAnNvmeSlot()
    {
        // Talk moment: same M.2 2280 shape, different interface.
        RepositoryPaths.SkipUnlessNomicModelIsPresent();

        await GoldenQueryRunner.RunAsync(fixture, "GQ-06", "ontology");
    }

    [Fact]
    public async Task GQ07_Ontology_SpanishLabelsFindTheChargers()
    {
        // Talk moment: "cargador" is a Spanish label of Chargers, so the query expands to English terms and finds the charger.
        RepositoryPaths.SkipUnlessNomicModelIsPresent();

        await GoldenQueryRunner.RunAsync(fixture, "GQ-07", "ontology");
    }

    [Fact]
    public async Task GQ08_Ontology_TreatsTheDeviceNameAsContext()
    {
        // Talk moment: Stage 6 recognises "Blackbird Aerobook 14" as the device you own, removes it from the
        // search text, checks the chargers against it, and moves the laptop itself below the chargers.
        RepositoryPaths.SkipUnlessNomicModelIsPresent();

        var response = await GoldenQueryRunner.RunAsync(fixture, "GQ-08", "ontology");

        var understand = response.DebugTrace.Steps[0];
        Assert.Equal("Blackbird Aerobook 14", understand.Details!["deviceMention"].GetString());
        Assert.Equal("charger for my", understand.Details["queryWithoutDevice"].GetString());
        Assert.Equal("PROD-0001", understand.Details["targetDevice"].GetProperty("id").GetString());

        var ownDevice = Assert.Single(response.Results, r => r.Id == "PROD-0001");
        Assert.Contains(ownDevice.Compatibility.Reasons, r => r.Contains("your target device"));
    }

    [Fact]
    public async Task Ontology_TogglesOff_NothingIsDemotedOrEvaluated_ButEveryStepIsStillTraced()
    {
        // The presenter's before/after: Stage 6 with both toggles off behaves like hybrid search on the typed query.
        RepositoryPaths.SkipUnlessNomicModelIsPresent();

        var request = GoldenQueryCase.Load("GQ-01").Request;
        request["options"] = new JsonObject { ["expandSynonyms"] = false, ["applyConstraints"] = false };

        using var client = fixture.CreateSearchApiClient();
        var response = await SearchApiClient.SearchAsync(client, "ontology", request, TestContext.Current.CancellationToken);

        Assert.All(response.Results, r => Assert.Equal("NotEvaluated", r.Compatibility.Status));
        Assert.Equal(
            response.Results.Select(r => r.Signals.FusedRank),
            response.Results.Select((_, i) => (int?)(i + 1))); // fused order, untouched
        Assert.Equal(
            ["ontology", "ontology", "keyword", "vector", "vector", "hybrid", "ontology", "ontology"],
            response.DebugTrace.Steps.Select(s => s.Stage));
    }
}

using System.Text.Json.Nodes;
using PI.SearchApi.IntegrationTests.SearchApi;
using Xunit;

namespace PI.SearchApi.IntegrationTests.GoldenQueries;

[Collection(AppHostCollection.Name)]
public sealed class OntologyStageGoldenQueryTests(AppHostFixture fixture)
{
    [Fact]
    public async Task GQ03_Ontology_FlagsNearMissWithReason()
    {
        // Talk moment: the 45W barrel charger is flagged Incompatible on connector and wattage — similarity ≠ compatibility.
        RepositoryPaths.SkipUnlessNomicModelIsPresent();

        var response = await GoldenQueryRunner.RunAsync(fixture, "GQ-03", "ontology");

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
    public async Task GQ04_Ontology_MarksThePhoneBatteryOutOfConcept()
    {
        // Talk moment: the cordless phone battery is under Telephony, not Power tools › Batteries.
        RepositoryPaths.SkipUnlessNomicModelIsPresent();

        await GoldenQueryRunner.RunAsync(fixture, "GQ-04", "ontology");
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
        // Talk moment: "cargador" is a Spanish label of Chargers, so the query expands to English terms and finds the charger;
        // "USB-C" is a stated requirement, so the barrel charger is flagged even with no device.
        RepositoryPaths.SkipUnlessNomicModelIsPresent();

        var response = await GoldenQueryRunner.RunAsync(fixture, "GQ-07", "ontology");

        var barrel = Assert.Single(response.Results, r => r.Id == "PROD-0014");
        Assert.Equal("Query", barrel.Compatibility.Source);
        Assert.Contains(barrel.Compatibility.Reasons, r => r.Contains("you asked for USB-C"));
    }

    [Fact]
    public async Task GQ09_Ontology_ChecksStatedRequirementsWithoutADevice()
    {
        // Talk moment: no device picked, but "65W USB-C" is enough to check every charger, and each one says which laptops it fits.
        RepositoryPaths.SkipUnlessNomicModelIsPresent();

        var response = await GoldenQueryRunner.RunAsync(fixture, "GQ-09", "ontology");

        var understand = response.DebugTrace.Steps[0];
        Assert.Equal(2, understand.Details!["requirements"].GetArrayLength());
        Assert.True(understand.Details["requirementsApplied"].GetBoolean());
        Assert.Equal("query", response.DebugTrace.Steps[^1].Details!["checkedAgainst"].GetString());

        var compatible = Assert.Single(response.Results, r => r.Id == "PROD-0012");
        Assert.Equal("Query", compatible.Compatibility.Source);
        var laptops = Assert.Single(compatible.Compatibility.Fits!, f => f.DeviceType == "laptops");
        Assert.Contains(laptops.Devices, d => d.Id == "PROD-0001");
        Assert.True(laptops.Total > laptops.Devices.Count, "a 65W charger shouldn't fit every laptop (some need 100W or a barrel plug)");

        var barrel = Assert.Single(response.Results, r => r.Id == "PROD-0014");
        Assert.Contains(barrel.Compatibility.Reasons, r => r.Contains("you asked for USB-C"));
        Assert.Contains(barrel.Compatibility.Reasons, r => r.Contains("you asked for at least 65W"));
    }

    [Fact]
    public async Task Ontology_TargetDeviceAndContradictingRequirement_DeviceDecidesAndTheConflictIsTraced()
    {
        // "45W" contradicts the Aerobook 14's 65W minimum: the device's specs decide, and the trace says so.
        RepositoryPaths.SkipUnlessNomicModelIsPresent();

        var request = new JsonObject { ["query"] = "45W charger", ["context"] = new JsonObject { ["targetProductId"] = "PROD-0001" } };
        using var client = fixture.CreateSearchApiClient();

        var response = await SearchApiClient.SearchAsync(client, "ontology", request, TestContext.Current.CancellationToken);

        var understand = response.DebugTrace.Steps[0];
        Assert.False(understand.Details!["requirementsApplied"].GetBoolean());
        Assert.Contains("needs at least 65W", understand.Details["requirementConflicts"][0].GetString());
        Assert.All(response.Results.Where(r => r.Compatibility.Status is "Compatible" or "Incompatible"),
            r => Assert.Equal("Device", r.Compatibility.Source));
        Assert.All(response.Results, r => Assert.Null(r.Compatibility.Fits));
    }

    [Fact]
    public async Task GQ08_Ontology_TreatsTheDeviceNameAsContext()
    {
        // Talk moment: Stage 5 recognises "Blackbird Aerobook 14" as the device you own, removes it from the
        // search text, checks the chargers against it, and keeps the laptop itself below the chargers.
        RepositoryPaths.SkipUnlessNomicModelIsPresent();

        var response = await GoldenQueryRunner.RunAsync(fixture, "GQ-08", "ontology");

        var understand = response.DebugTrace.Steps[0];
        Assert.Equal("Blackbird Aerobook 14", understand.Details!["deviceMention"].GetString());
        Assert.Equal("charger for my", understand.Details["queryWithoutDevice"].GetString());
        Assert.Equal("PROD-0001", understand.Details["targetDevice"].GetProperty("id").GetString());

        // Once its name is removed, "charger for my" doesn't retrieve the laptop from the 300-product catalog (in
        // the 60-product core it always did). When it is retrieved, it must say why it sits below the chargers.
        var ownDevice = response.Results.SingleOrDefault(r => r.Id == "PROD-0001");
        if (ownDevice is not null)
        {
            Assert.Contains(ownDevice.Compatibility.Reasons, r => r.Contains("your target device"));
        }
    }

    [Fact]
    public async Task Ontology_TogglesOff_NothingIsDemotedOrEvaluated_ButEveryStepIsStillTraced()
    {
        // The presenter's before/after: Stage 5 with both toggles off behaves like hybrid search on the typed query.
        RepositoryPaths.SkipUnlessNomicModelIsPresent();

        var request = GoldenQueryCase.Load("GQ-03").Request;
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

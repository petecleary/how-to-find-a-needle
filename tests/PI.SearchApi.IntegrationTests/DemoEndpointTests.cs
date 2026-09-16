using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace PI.SearchApi.IntegrationTests;

[Collection(AppHostCollection.Name)]
public sealed class DemoEndpointTests(AppHostFixture fixture)
{
    [Fact]
    public async Task Queries_ReturnsEveryGoldenQuery()
    {
        using var client = fixture.CreateSearchApiClient();

        var queries = await client.GetFromJsonAsync<JsonArray>("/api/demo/queries", TestContext.Current.CancellationToken);

        Assert.Equal(9, queries!.Count);
        Assert.Equal("GQ-01", queries[0]!["id"]!.GetValue<string>());
    }

    [Fact]
    public async Task Devices_ReturnsLaptopsAndDrillsButNotAccessories()
    {
        using var client = fixture.CreateSearchApiClient();

        var devices = await client.GetFromJsonAsync<JsonArray>("/api/demo/devices", TestContext.Current.CancellationToken);
        var ids = devices!.Select(d => d!["id"]!.GetValue<string>()).ToList();

        Assert.Contains("PROD-0001", ids); // Blackbird Aerobook 14 (laptops)
        Assert.Contains("PROD-0006", ids); // Brakk 18V Combi Drill (drills)
        Assert.DoesNotContain("PROD-0011", ids); // a charger is an accessory, not a device
    }

    [Fact]
    public async Task Taxonomy_ReturnsTreeWithMultilingualLabels()
    {
        using var client = fixture.CreateSearchApiClient();

        var tree = await client.GetFromJsonAsync<JsonArray>("/api/taxonomy", TestContext.Current.CancellationToken);

        var power = tree!.Single(n => n!["notation"]!.GetValue<string>() == "power")!;
        var chargers = power["narrower"]!.AsArray().Single(n => n!["notation"]!.GetValue<string>() == "chargers")!;
        Assert.Equal("Cargadores", chargers["labels"]!["es"]!.GetValue<string>());
        Assert.Contains("power brick", chargers["altLabels"]!.AsArray().Select(l => l!.GetValue<string>()));
    }

    [Fact]
    public async Task Vocabularies_ReturnsConnectorsWithSpecKeysAndSynonyms()
    {
        using var client = fixture.CreateSearchApiClient();

        var vocabularies = await client.GetFromJsonAsync<JsonArray>("/api/vocabularies", TestContext.Current.CancellationToken);

        var connectors = vocabularies!.Single(v => v!["notation"]!.GetValue<string>() == "connectors")!;
        Assert.Equal(new[] { "chargingPort", "connector" }, connectors["specs"]!.AsArray().Select(s => s!.GetValue<string>()));
        var usbC = connectors["values"]!.AsArray().Single(v => v!["notation"]!.GetValue<string>() == "usb-c")!;
        Assert.Contains("Type-C", usbC["altLabels"]!.AsArray().Select(l => l!.GetValue<string>()));
    }

    [Fact]
    public async Task Brands_ReturnsEachCatalogueBrandOnceInOrder()
    {
        using var client = fixture.CreateSearchApiClient();

        var brands = (await client.GetFromJsonAsync<List<string>>("/api/brands", TestContext.Current.CancellationToken))!;

        Assert.Contains("Brakk", brands); // GQ-04 filters on it
        Assert.Contains("Voltline", brands);
        Assert.Equal(brands.Distinct().Order(StringComparer.Ordinal), brands);
    }

    [Fact]
    public async Task Search_InvalidRequest_ReturnsProblemDetails400()
    {
        using var client = fixture.CreateSearchApiClient();

        using var response = await client.PostAsJsonAsync(
            "/api/search/structured",
            new { pageSize = 0, filters = new { categories = new[] { "chargerz" } } },
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>(TestContext.Current.CancellationToken);
        Assert.Contains("chargerz", body.ToString());
    }
}

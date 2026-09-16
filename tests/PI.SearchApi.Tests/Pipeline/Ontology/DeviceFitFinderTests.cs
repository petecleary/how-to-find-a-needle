using System.Text.Json;
using PI.SearchApi.Pipeline;
using PI.SearchApi.Pipeline.Ontology;
using Xunit;

namespace PI.SearchApi.Tests.Pipeline.Ontology;

public sealed class DeviceFitFinderTests
{
    private static readonly DomainOntology Ontology = new(Path.Combine(AppContext.BaseDirectory, "assets", "data"));
    private static readonly DeviceFitFinder Finder = new(Ontology, new CompatibilityEvaluator(Ontology));

    private static ProductSummary Product(string id, string name, string category, string specsJson) =>
        new(id, name, "Test", [category], 10m, JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(specsJson)!);

    private static readonly ProductSummary[] Devices =
    [
        Product("L1", "Aerobook 14", "laptops", """{ "chargingPort": "usb-c", "minChargerWattageW": 65 }"""),
        Product("L2", "Aerobook 16", "laptops", """{ "chargingPort": "usb-c", "minChargerWattageW": 100 }"""),
        Product("L3", "Workmate 15", "laptops", """{ "chargingPort": "barrel-5.5mm", "minChargerWattageW": 45 }"""),
        Product("L4", "Mystery Laptop", "laptops", """{ "chargingPort": "usb-c" }"""),
        Product("D1", "Brakk 18V Drill", "drills", """{ "batteryPlatform": "brakk-18v" }"""),
    ];

    [Fact]
    public void FitsFor_65WUsbCCharger_FitsOnlyTheLaptopsEveryCheckPassesFor()
    {
        // L2 needs 100W, L3 has a barrel socket, and L4 has no wattage spec (Unknown, so not a fit). Drills aren't checked.
        var charger = Product("C1", "65W USB-C Charger", "laptop-chargers", """{ "connector": "usb-c", "wattageW": 65 }""");

        var fit = Assert.Single(Finder.FitsFor(charger, Devices)!);

        Assert.Equal(("laptops", "Laptops", 4), (fit.DeviceType, fit.DeviceTypeLabel, fit.Total));
        Assert.Equal(["L1"], fit.Devices.Select(d => d.Id));
    }

    [Fact]
    public void FitsFor_ChargerThatFitsNothing_ListsNoneButKeepsTheTotal()
    {
        var charger = Product("C2", "20W Phone Charger", "phone-chargers", """{ "connector": "usb-c", "wattageW": 20 }""");

        var fit = Assert.Single(Finder.FitsFor(charger, Devices)!);

        Assert.Empty(fit.Devices);
        Assert.Equal(4, fit.Total);
    }

    [Fact]
    public void FitsFor_ProductWithNoRules_IsNull()
    {
        var sleeve = Product("B1", "Laptop Sleeve", "bags", "{}");

        Assert.Null(Finder.FitsFor(sleeve, Devices));
    }
}

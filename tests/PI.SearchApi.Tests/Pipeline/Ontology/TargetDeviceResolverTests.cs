using PI.SearchApi.Pipeline;
using PI.SearchApi.Pipeline.Ontology;
using Xunit;

namespace PI.SearchApi.Tests.Pipeline.Ontology;

public sealed class TargetDeviceResolverTests
{
    private static ProductSummary Device(string id, string name) => new(id, name, "Test", ["laptops"], 10m, new Dictionary<string, System.Text.Json.JsonElement>());

    private static readonly ProductSummary[] Devices =
    [
        Device("PROD-0001", "Blackbird Aerobook 14"),
        Device("PROD-0002", "Blackbird Aerobook 16"),
        Device("PROD-0004", "Corvid Slate 15"),
        Device("PROD-0005", "Corvid Slate 15 Pro"),
    ];

    [Fact]
    public void FindMention_DeviceNameInQuery_ReturnsDeviceAndTokenSpan()
    {
        var tokens = TextNormaliser.Tokenise("charger for my Blackbird Aerobook 14");

        var mention = TargetDeviceResolver.FindMention(tokens, Devices);

        Assert.NotNull(mention);
        Assert.Equal("PROD-0001", mention.Device.Id);
        Assert.Equal(new TokenSpan(3, 3), mention.Span); // "Blackbird Aerobook 14" is tokens 3–5
    }

    [Fact]
    public void FindMention_LongestNameWins()
    {
        var mention = TargetDeviceResolver.FindMention(TextNormaliser.Tokenise("charger for a corvid slate 15 pro"), Devices);

        Assert.Equal("PROD-0005", mention!.Device.Id);
    }

    [Fact]
    public void FindMention_IgnoresCase()
    {
        Assert.Equal("PROD-0002", TargetDeviceResolver.FindMention(TextNormaliser.Tokenise("BLACKBIRD aerobook 16 ssd"), Devices)!.Device.Id);
    }

    [Theory]
    [InlineData("charger for my Aerobook")] // partial names don't match: exact on purpose, and a known limit
    [InlineData("charger for my Blackbird Aerobook")]
    [InlineData("power adapter for my laptop")]
    public void FindMention_NoWholeDeviceName_ReturnsNull(string query)
    {
        Assert.Null(TargetDeviceResolver.FindMention(TextNormaliser.Tokenise(query), Devices));
    }
}

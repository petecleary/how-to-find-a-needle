using Xunit;

namespace PI.SearchApi.IntegrationTests.GoldenQueries;

[Collection(AppHostCollection.Name)]
public sealed class StructuredStageGoldenQueryTests(AppHostFixture fixture)
{
    [Fact]
    public async Task GQ01_Structured_ReturnsExactlyTheBrakk18VItemsUnder100()
    {
        // Talk moment: the user already knows the attributes — an exact filter needs no ranking at all.
        var response = await GoldenQueryRunner.RunAsync(fixture, "GQ-01", "structured");

        Assert.Equal(6, response.TotalResults);
        Assert.All(response.Results, r => Assert.Null(r.Score));
        Assert.All(response.Results, r => Assert.True(r.Signals.StructuredMatch));

        var step = Assert.Single(response.DebugTrace.Steps);
        Assert.Equal("structured", step.Stage);
        Assert.Contains("specs @> @specs::jsonb", step.Sql);
        Assert.DoesNotContain("Brakk", step.Sql); // values are parameters, never interpolated
        Assert.True(step.Parameters!.ContainsKey("brand"));
    }
}

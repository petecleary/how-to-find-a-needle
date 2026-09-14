using System.Net;
using Xunit;

namespace PI.SearchApi.IntegrationTests;

[Collection(AppHostCollection.Name)]
public sealed class HealthCheckTests(AppHostFixture fixture)
{
    [Fact]
    public async Task Health_WhenAppHostStarts_ReturnsOk()
    {
        using var client = fixture.CreateSearchApiClient();

        using var response = await client.GetAsync("/health", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}

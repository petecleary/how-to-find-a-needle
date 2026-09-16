using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using Xunit;

namespace PI.SearchApi.IntegrationTests;

/// <summary>
/// The UI generates its TypeScript types from <c>/openapi/v1.json</c> (ADR-0014), so a gap in the
/// document becomes a gap in the UI's types.
/// </summary>
[Collection(AppHostCollection.Name)]
public sealed class OpenApiDocumentTests(AppHostFixture fixture)
{
    [Fact]
    public async Task Document_EveryOperation_HasSummary()
    {
        var document = await GetDocumentAsync();

        var operations = document["paths"]!.AsObject()
            .SelectMany(path => path.Value!.AsObject().Select(operation => (
                Name: $"{operation.Key.ToUpperInvariant()} {path.Key}",
                Summary: operation.Value!["summary"]?.GetValue<string>())))
            .ToList();

        Assert.NotEmpty(operations);
        Assert.All(operations, operation =>
            Assert.False(string.IsNullOrWhiteSpace(operation.Summary), $"{operation.Name} has no summary"));
    }

    [Theory]
    [InlineData("/api/search/structured", "400")]
    [InlineData("/api/search/keyword", "400")]
    [InlineData("/api/search/vector", "400")]
    [InlineData("/api/search/vector", "503")]
    [InlineData("/api/search/hybrid", "400")]
    [InlineData("/api/search/hybrid", "503")]
    [InlineData("/api/search/ontology", "400")]
    [InlineData("/api/search/ontology", "503")]
    public async Task Document_SearchOperation_DescribesProblemResponse(string path, string statusCode)
    {
        var document = await GetDocumentAsync();

        var response = document["paths"]![path]!["post"]!["responses"]![statusCode];

        Assert.NotNull(response);
        Assert.NotNull(response["content"]!["application/problem+json"]);
    }

    [Fact]
    public async Task ValidationProblem_RealResponse_HasEveryDocumentedField()
    {
        var document = await GetDocumentAsync();
        var documentedFields = document["components"]!["schemas"]!["ValidationProblem"]!["properties"]!.AsObject()
            .Select(property => property.Key)
            .ToList();
        using var client = fixture.CreateSearchApiClient();

        // pageSize is limited to 1–50 (ADR-0003), so this request fails validation.
        using var response = await client.PostAsJsonAsync(
            "/api/search/keyword",
            new { query = "charger", pageSize = 999 },
            TestContext.Current.CancellationToken);
        var body = await response.Content.ReadFromJsonAsync<JsonObject>(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.All(documentedFields, field => Assert.True(body!.ContainsKey(field), $"The 400 body has no '{field}'"));
        Assert.Equal("pageSize", body!["errors"]![0]!["name"]!.GetValue<string>());
    }

    private async Task<JsonObject> GetDocumentAsync()
    {
        using var client = fixture.CreateSearchApiClient();
        var document = await client.GetFromJsonAsync<JsonObject>("/openapi/v1.json", TestContext.Current.CancellationToken);
        return document!;
    }
}

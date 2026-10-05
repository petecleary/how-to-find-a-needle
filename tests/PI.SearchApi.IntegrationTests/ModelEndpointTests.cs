using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using PI.SearchApi.IntegrationTests.GoldenQueries;
using PI.SearchApi.IntegrationTests.SearchApi;
using Xunit;

namespace PI.SearchApi.IntegrationTests;

// Bring your own model (ADR-0019): the provider endpoints, and a Stage 6 request that names its model.
[Collection(AppHostCollection.Name)]
public sealed class ModelEndpointTests(AppHostFixture fixture)
{
    // The default in appsettings.json; naming it explicitly proves the options.model path without needing a second model.
    private const string DefaultOllamaModel = "ollama/qwen3.6:35b";

    [Fact]
    public async Task Providers_ListsEveryProviderWithKeySourceButNoKey()
    {
        using var client = fixture.CreateSearchApiClient();

        var providers = (await client.GetFromJsonAsync<JsonArray>("/api/providers", TestContext.Current.CancellationToken))!;

        Assert.Equal(
            ["ollama", "openai", "anthropic", "google", "azure", "compat"],
            providers.Select(p => p!["id"]!.GetValue<string>()));
        Assert.All(providers, p =>
        {
            Assert.NotNull(p!["keySource"]);
            Assert.Null(p["key"]);
            Assert.Null(p["apiKey"]);
        });
    }

    [Fact]
    public async Task Models_AlwaysOffersTheDefaultModel()
    {
        using var client = fixture.CreateSearchApiClient();

        var catalogue = await client.GetFromJsonAsync<JsonObject>("/api/models", TestContext.Current.CancellationToken);

        Assert.Equal(DefaultOllamaModel, catalogue!["default"]!.GetValue<string>());
        var ollama = catalogue["providers"]!.AsArray().Single(p => p!["id"]!.GetValue<string>() == "ollama");
        Assert.Contains(ollama!["models"]!.AsArray(), m => m!["ref"]!.GetValue<string>() == DefaultOllamaModel);
    }

    [Fact]
    public async Task UpdateProvider_InvalidBaseUrl_Returns400()
    {
        using var client = fixture.CreateSearchApiClient();

        using var response = await client.PutAsJsonAsync(
            "/api/providers/ollama", new { baseUrl = "not a url" }, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task UnknownProvider_Returns404()
    {
        using var client = fixture.CreateSearchApiClient();

        using var response = await client.PostAsync("/api/providers/litellm/test", null, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Answer_MalformedModel_Returns400()
    {
        using var client = fixture.CreateSearchApiClient();
        var request = WithModel(GoldenQueryCase.Load("GQ-03").Request, "qwen3.6:35b"); // no provider

        using var response = await client.PostAsJsonAsync("/api/search/rag/answer", request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Answer_ProviderThatNeedsABaseUrl_Returns503WithTheFix()
    {
        // Azure has no default address, and the tests' settings folder is empty, so this can't reach any network.
        RepositoryPaths.SkipUnlessNomicModelIsPresent();
        using var client = fixture.CreateSearchApiClient();
        var request = WithModel(GoldenQueryCase.Load("GQ-03").Request, "azure/my-deployment");

        using var message = new HttpRequestMessage(HttpMethod.Post, "/api/search/rag/answer") { Content = JsonContent.Create(request) };
        message.Headers.Accept.ParseAdd("application/json");
        using var response = await client.SendAsync(message, TestContext.Current.CancellationToken);
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.Contains("needs a base URL", body);
        Assert.Contains("Models and API keys", body);
    }

    [Fact]
    public async Task GQ03_Rag_NamedModel_AnswersWithThatModel()
    {
        RepositoryPaths.SkipUnlessNomicModelIsPresent();
        using var client = fixture.CreateSearchApiClient();
        var request = WithModel(GoldenQueryCase.Load("GQ-03").Request, DefaultOllamaModel);

        var answer = await AnswerApiClient.AnswerAsync(client, "rag", request, TestContext.Current.CancellationToken);

        Assert.Equal("ollama", answer.Provider);
        Assert.Equal("qwen3.6:35b", answer.Model);
    }

    private static JsonObject WithModel(JsonObject request, string model)
    {
        var copy = request.DeepClone().AsObject();
        if (copy["options"] is not JsonObject options)
        {
            options = [];
            copy["options"] = options;
        }

        options["model"] = model;
        return copy;
    }
}

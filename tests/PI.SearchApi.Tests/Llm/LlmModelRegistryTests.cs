using System.Net;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting.Internal;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using PI.SearchApi.Llm;
using Xunit;

namespace PI.SearchApi.Tests.Llm;

// The registry with in-memory configuration, a temporary settings folder and a stub HTTP handler: no network, no Ollama.
public sealed class LlmModelRegistryTests : IDisposable
{
    private readonly string home = Path.Combine(Path.GetTempPath(), "needle-registry-" + Guid.NewGuid().ToString("N"));
    private readonly StubHandler http = new();

    public void Dispose()
    {
        if (Directory.Exists(home))
        {
            Directory.Delete(home, recursive: true);
        }
    }

    private (LlmModelRegistry Registry, ISecretStore Secrets, LlmSettingsStore Settings) Create(Dictionary<string, string?>? values = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { [LlmSettingsStore.HomeSetting] = home })
            .AddInMemoryCollection(values ?? [])
            .Build();
        var defaults = new LlmOptions { Provider = LlmProviders.Ollama, Model = "qwen3.6:35b", Endpoint = "http://localhost:11434" };
        var settings = new LlmSettingsStore(configuration, NullLogger<LlmSettingsStore>.Instance);
        var secrets = new ConfigurationAndSessionSecretStore(configuration);
        var registry = new LlmModelRegistry(
            Options.Create(defaults), settings, secrets, new StubHttpClientFactory(http), NullLoggerFactory.Instance,
            new HostingEnvironment { EnvironmentName = "Production" });

        return (registry, secrets, settings);
    }

    [Fact]
    public void Resolve_NoModel_UsesTheConfiguredDefault()
    {
        var (registry, _, _) = Create();

        var call = registry.Resolve(model: null);

        Assert.Equal(LlmProviders.Ollama, call.Options.Provider);
        Assert.Equal("qwen3.6:35b", call.Options.Model);
        Assert.Equal("http://localhost:11434", call.Options.Endpoint);
    }

    [Fact]
    public void Resolve_NamedModel_UsesItsProvidersAddressKeyAndOptions()
    {
        var (registry, secrets, _) = Create();
        secrets.SetForSession(LlmProviders.Anthropic, "sk-ant-session");

        var call = registry.Resolve("anthropic/claude-sonnet-5");

        Assert.Equal(LlmProviders.Anthropic, call.Options.Provider);
        Assert.Equal("claude-sonnet-5", call.Options.Model);
        Assert.Equal("https://api.anthropic.com", call.Options.Endpoint);
        Assert.Null(call.ChatOptions.Temperature); // Anthropic's options, not the default provider's
    }

    [Fact]
    public void Resolve_HostedModelWithoutKey_ThrowsUnavailable()
    {
        var (registry, _, _) = Create();

        Assert.Throws<LlmUnavailableException>(() => registry.Resolve("openai/gpt-5"));
    }

    [Fact]
    public void Resolve_SavedBaseUrl_WinsOverTheDefault()
    {
        var (registry, _, settings) = Create();
        settings.Update(s => s with
        {
            Providers = new Dictionary<string, LlmProviderSettings> { [LlmProviders.Ollama] = new() { BaseUrl = "http://gpu-box:11434/" } },
        });

        Assert.Equal("http://gpu-box:11434", registry.Resolve("ollama/llama3").Options.Endpoint);
    }

    [Fact]
    public void Providers_ReportKeySourceButNeverTheKey()
    {
        var (registry, _, _) = Create(new() { ["GEMINI_API_KEY"] = "gemini-secret" });

        var providers = registry.Providers();
        var json = JsonSerializer.Serialize(providers);

        Assert.Equal(LlmProviders.All, providers.Select(p => p.Id));
        Assert.Equal(KeySource.Configuration, providers.Single(p => p.Id == LlmProviders.Google).KeySource);
        Assert.DoesNotContain("gemini-secret", json);
    }

    [Fact]
    public async Task ModelsAsync_ReadsOllamaTagsWithCapabilities_AndMarksTheDefault()
    {
        var (registry, _, _) = Create();
        http.Respond("http://localhost:11434/api/tags", """
            { "models": [
                { "name": "qwen3.6:35b", "capabilities": ["completion", "tools"], "details": { "context_length": 262144 } },
                { "name": "gemma4:31b", "capabilities": ["completion", "vision"] }
            ] }
            """);

        var catalogue = await registry.ModelsAsync(TestContext.Current.CancellationToken);
        var ollama = catalogue.Providers.Single(p => p.Id == LlmProviders.Ollama);

        Assert.Equal("ollama/qwen3.6:35b", catalogue.Default);
        Assert.Null(ollama.Problem);
        Assert.Equal(["gemma4:31b", "qwen3.6:35b"], ollama.Models.Select(m => m.Model));
        var qwen = ollama.Models.Single(m => m.Model == "qwen3.6:35b");
        Assert.True(qwen.IsDefault);
        Assert.True(qwen.Tools);
        Assert.Equal(262144, qwen.ContextLength);
        Assert.True(ollama.Models.Single(m => m.Model == "gemma4:31b").Vision);
    }

    [Fact]
    public async Task ModelsAsync_ProviderDownOrWithoutKey_IsListedWithItsProblem()
    {
        var (registry, _, _) = Create();
        // The stub answers 404 for anything it wasn't told about, so Ollama looks "not running".

        var catalogue = await registry.ModelsAsync(TestContext.Current.CancellationToken);

        var ollama = catalogue.Providers.Single(p => p.Id == LlmProviders.Ollama);
        Assert.Contains("Not running", ollama.Problem);
        // The default model is still offered, so the picker always has something to show.
        Assert.Equal(["qwen3.6:35b"], ollama.Models.Select(m => m.Model));

        var anthropic = catalogue.Providers.Single(p => p.Id == LlmProviders.Anthropic);
        Assert.Contains("ANTHROPIC_API_KEY", anthropic.Problem);
        Assert.DoesNotContain(catalogue.Providers, p => p.Id == LlmProviders.Azure); // disabled until configured
    }

    [Fact]
    public async Task TestAsync_SendsTheKeyToTheProviderAndFiltersNonChatModels()
    {
        var (registry, secrets, _) = Create();
        secrets.SetForSession(LlmProviders.OpenAI, "sk-test");
        http.Respond("https://api.openai.com/v1/models", """{ "data": [ { "id": "gpt-5" }, { "id": "text-embedding-3-small" }, { "id": "gpt-4o-realtime" } ] }""");

        var result = await registry.TestAsync(LlmProviders.OpenAI, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.True(result.Ok);
        Assert.Equal(1, result.Models);
        Assert.Equal("Bearer sk-test", http.LastAuthorization);
    }

    [Fact]
    public async Task TestAsync_UnknownProvider_IsNull()
    {
        var (registry, _, _) = Create();

        Assert.Null(await registry.TestAsync("litellm", TestContext.Current.CancellationToken));
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly Dictionary<string, string> responses = [];

        public string? LastAuthorization { get; private set; }

        public void Respond(string url, string json) => responses[url] = json;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastAuthorization = request.Headers.Authorization?.ToString();

            return Task.FromResult(responses.TryGetValue(request.RequestUri!.ToString(), out var json)
                ? new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) }
                : new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }

    private sealed class StubHttpClientFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => new(handler, disposeHandler: false);
    }
}

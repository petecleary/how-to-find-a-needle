using Microsoft.Extensions.Logging.Abstractions;
using PI.SearchApi.Llm;
using Xunit;

namespace PI.SearchApi.Tests.Llm;

// Building a client makes no network call, so these run without Ollama or a key.
public sealed class LlmClientFactoryTests
{
    [Fact]
    public void Create_UnknownProvider_ThrowsUnavailableWithTheValidChoices()
    {
        var options = new LlmOptions { Provider = "litellm", Model = "m" };

        var exception = Assert.Throws<LlmUnavailableException>(() => LlmClientFactory.Create(options, NullLoggerFactory.Instance, isDevelopment: false));

        Assert.Contains("ollama, openai, anthropic, google, azure, compat", exception.Message);
    }

    [Theory]
    [InlineData(LlmProviders.OpenAI, "OPENAI_API_KEY")]
    [InlineData(LlmProviders.Anthropic, "ANTHROPIC_API_KEY")]
    [InlineData(LlmProviders.Google, "GEMINI_API_KEY")]
    [InlineData(LlmProviders.Azure, "AZURE_OPENAI_API_KEY")]
    public void Create_HostedProviderWithoutKey_ThrowsUnavailableWithBothWaysToAddOne(string provider, string keyVariable)
    {
        var options = new LlmOptions { Provider = provider, Model = "m", Endpoint = "https://example.invalid/v1" };

        var exception = Assert.Throws<LlmUnavailableException>(() => LlmClientFactory.Create(options, NullLoggerFactory.Instance, isDevelopment: false));

        // The UI's panel for this session, or a user secret for every run (ADR-0019).
        Assert.Contains("Models and API keys", exception.Message);
        Assert.Contains($"dotnet user-secrets set \"{keyVariable}\"", exception.Message);
    }

    [Theory]
    [InlineData(LlmProviders.Azure)]
    [InlineData(LlmProviders.Compatible)]
    public void Create_OpenAICompatibleProviderWithoutBaseUrl_ThrowsUnavailable(string provider)
    {
        var options = new LlmOptions { Provider = provider, Model = "m", ApiKey = "k" };

        var exception = Assert.Throws<LlmUnavailableException>(() => LlmClientFactory.Create(options, NullLoggerFactory.Instance, isDevelopment: false));

        Assert.Contains("needs a base URL", exception.Message);
    }

    [Fact]
    public void Create_MissingModel_ThrowsUnavailable()
    {
        var options = new LlmOptions { Provider = LlmProviders.Ollama, Endpoint = "http://localhost:11434", Model = "" };

        Assert.Throws<LlmUnavailableException>(() => LlmClientFactory.Create(options, NullLoggerFactory.Instance, isDevelopment: false));
    }

    [Theory]
    [InlineData(LlmProviders.Ollama, null)]
    [InlineData(LlmProviders.OpenAI, "sk-test")]
    [InlineData(LlmProviders.Anthropic, "sk-ant-test")]
    [InlineData(LlmProviders.Google, "gemini-test")]
    [InlineData(LlmProviders.Azure, "azure-test")]
    [InlineData(LlmProviders.Compatible, null)] // a local compatible server needs no key
    public void Create_ConfiguredProvider_BuildsAChatClient(string provider, string? apiKey)
    {
        var options = new LlmOptions { Provider = provider, Model = "m", Endpoint = "http://localhost:11434", ApiKey = apiKey };

        using var chatClient = LlmClientFactory.Create(options, NullLoggerFactory.Instance, isDevelopment: true);

        Assert.NotNull(chatClient);
    }
}

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

        Assert.Contains("ollama, openai, anthropic", exception.Message);
    }

    [Theory]
    [InlineData(LlmProviders.OpenAI)]
    [InlineData(LlmProviders.Anthropic)]
    public void Create_HostedProviderWithoutKey_ThrowsUnavailableWithUserSecretsGuidance(string provider)
    {
        var options = new LlmOptions { Provider = provider, Model = "m" };

        var exception = Assert.Throws<LlmUnavailableException>(() => LlmClientFactory.Create(options, NullLoggerFactory.Instance, isDevelopment: false));

        Assert.Contains("dotnet user-secrets set \"Llm:ApiKey\"", exception.Message);
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
    public void Create_ConfiguredProvider_BuildsAChatClient(string provider, string? apiKey)
    {
        var options = new LlmOptions { Provider = provider, Model = "m", Endpoint = "http://localhost:11434", ApiKey = apiKey };

        using var chatClient = LlmClientFactory.Create(options, NullLoggerFactory.Instance, isDevelopment: true);

        Assert.NotNull(chatClient);
    }
}

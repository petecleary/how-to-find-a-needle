using Microsoft.Extensions.Configuration;
using PI.SearchApi.Llm;
using Xunit;

namespace PI.SearchApi.Tests.Llm;

public sealed class ConfigurationAndSessionSecretStoreTests
{
    private static ConfigurationAndSessionSecretStore Create(Dictionary<string, string?> values) =>
        new(new ConfigurationBuilder().AddInMemoryCollection(values).Build());

    [Fact]
    public void Get_SessionKey_OverridesConfigurationUntilRemoved()
    {
        var secrets = Create(new() { ["ANTHROPIC_API_KEY"] = "from-config" });

        Assert.Equal("from-config", secrets.Get(LlmProviders.Anthropic));
        Assert.Equal(KeySource.Configuration, secrets.SourceOf(LlmProviders.Anthropic));

        secrets.SetForSession(LlmProviders.Anthropic, "  pasted  ");
        Assert.Equal("pasted", secrets.Get(LlmProviders.Anthropic));
        Assert.Equal(KeySource.Session, secrets.SourceOf(LlmProviders.Anthropic));

        secrets.RemoveSessionKey(LlmProviders.Anthropic);
        Assert.Equal("from-config", secrets.Get(LlmProviders.Anthropic));
    }

    [Fact]
    public void Get_NoKeyAnywhere_IsNone()
    {
        var secrets = Create([]);

        Assert.Null(secrets.Get(LlmProviders.OpenAI));
        Assert.Equal(KeySource.None, secrets.SourceOf(LlmProviders.OpenAI));
    }

    [Fact]
    public void Get_LegacyLlmApiKey_BelongsToTheDefaultProviderOnly()
    {
        // ADR-0015's Llm:ApiKey keeps working for the provider Llm:Provider names, and no other.
        var secrets = Create(new() { ["Llm:Provider"] = "anthropic", ["Llm:ApiKey"] = "legacy" });

        Assert.Equal("legacy", secrets.Get(LlmProviders.Anthropic));
        Assert.Null(secrets.Get(LlmProviders.OpenAI));
    }

    [Fact]
    public void Get_ProviderVariable_WinsOverLegacyLlmApiKey()
    {
        var secrets = Create(new() { ["Llm:Provider"] = "openai", ["Llm:ApiKey"] = "legacy", ["OPENAI_API_KEY"] = "specific" });

        Assert.Equal("specific", secrets.Get(LlmProviders.OpenAI));
    }
}

using System.Text.Json;
using PI.SearchApi.Llm;
using Xunit;

namespace PI.SearchApi.Tests.Llm;

public sealed class LlmTraceInfoTests
{
    [Fact]
    public void From_HostedProviderWithKey_NeverContainsTheKey()
    {
        var options = new LlmOptions { Provider = LlmProviders.Anthropic, Model = "claude-sonnet-5", ApiKey = "sk-ant-secret-value" };

        var json = JsonSerializer.Serialize(LlmTraceInfo.From(options, LlmChatOptions.For(options)));

        Assert.DoesNotContain("sk-ant-secret-value", json);
        Assert.Contains("claude-sonnet-5", json);
    }

    [Fact]
    public void From_Ollama_ShowsEndpointHostAndSettings()
    {
        var options = new LlmOptions { Provider = LlmProviders.Ollama, Model = "qwen3.6:35b", Endpoint = "http://localhost:11434" };

        var trace = LlmTraceInfo.From(options, LlmChatOptions.For(options));

        Assert.Equal("localhost:11434", trace.EndpointHost);
        Assert.Equal(0.1f, trace.Settings["temperature"]);
        Assert.Equal("None", trace.Settings["reasoningEffort"]);
        Assert.Equal(0, trace.Settings["retries"]);
    }
}

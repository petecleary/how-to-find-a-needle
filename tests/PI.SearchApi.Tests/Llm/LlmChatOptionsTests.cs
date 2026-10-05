using Microsoft.Extensions.AI;
using PI.SearchApi.Llm;
using Xunit;

namespace PI.SearchApi.Tests.Llm;

public sealed class LlmChatOptionsTests
{
    [Fact]
    public void For_Anthropic_SendsNoTemperature()
    {
        // Current Claude models reject sampling parameters with a 400 (ADR-0015).
        var chatOptions = LlmChatOptions.For(new LlmOptions { Provider = LlmProviders.Anthropic, Model = "claude-sonnet-5" });

        Assert.Null(chatOptions.Temperature);
        Assert.Equal(ReasoningEffort.Low, chatOptions.Reasoning?.Effort);
    }

    [Fact]
    public void For_Ollama_UsesLowTemperatureAndTurnsThinkingOff()
    {
        var chatOptions = LlmChatOptions.For(new LlmOptions { Provider = LlmProviders.Ollama, Model = "qwen3.6:35b" });

        Assert.Equal(0.1f, chatOptions.Temperature);
        Assert.Equal(ReasoningEffort.None, chatOptions.Reasoning?.Effort);
    }

    [Theory]
    [InlineData(LlmProviders.OpenAI)]
    [InlineData(LlmProviders.Azure)]
    [InlineData(LlmProviders.Google)]
    [InlineData(LlmProviders.Compatible)]
    public void For_OpenAIProtocolProvider_UsesLowTemperatureAndLeavesReasoningToTheModel(string provider)
    {
        var chatOptions = LlmChatOptions.For(new LlmOptions { Provider = provider, Model = "a-model" });

        Assert.Equal(0.1f, chatOptions.Temperature);
        Assert.Null(chatOptions.Reasoning);
    }

    [Theory]
    [InlineData(LlmProviders.Ollama)]
    [InlineData(LlmProviders.OpenAI)]
    [InlineData(LlmProviders.Anthropic)]
    [InlineData(LlmProviders.Azure)]
    [InlineData(LlmProviders.Google)]
    [InlineData(LlmProviders.Compatible)]
    public void For_AnyProvider_CapsOutputTokens(string provider)
    {
        var chatOptions = LlmChatOptions.For(new LlmOptions { Provider = provider, Model = "m", MaxOutputTokens = 1234 });

        Assert.Equal(1234, chatOptions.MaxOutputTokens);
    }
}

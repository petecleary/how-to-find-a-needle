using PI.SearchApi.Llm;
using Xunit;

namespace PI.SearchApi.Tests.Llm;

public sealed class ModelRefTests
{
    [Theory]
    [InlineData("ollama/qwen3.6:35b", "ollama", "qwen3.6:35b")]
    [InlineData("anthropic/claude-sonnet-5", "anthropic", "claude-sonnet-5")]
    // Only the first slash separates provider and model: model names can contain their own.
    [InlineData("compat/meta-llama/llama-3.1-8b", "compat", "meta-llama/llama-3.1-8b")]
    public void TryParse_WellFormed_SplitsAtTheFirstSlashAndRoundTrips(string value, string provider, string model)
    {
        Assert.True(ModelRef.TryParse(value, out var parsed));
        Assert.Equal(provider, parsed.Provider);
        Assert.Equal(model, parsed.Model);
        Assert.Equal(value, parsed.ToString());
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("qwen3.6:35b")]       // no provider
    [InlineData("ollama/")]           // no model
    [InlineData("/qwen3.6:35b")]      // empty provider
    [InlineData("litellm/gpt-5")]     // unknown provider
    [InlineData("Ollama/qwen3.6:35b")] // provider IDs are lower-case, like stage slugs
    public void TryParse_MalformedOrUnknownProvider_Fails(string? value)
    {
        Assert.False(ModelRef.TryParse(value, out _));
    }
}

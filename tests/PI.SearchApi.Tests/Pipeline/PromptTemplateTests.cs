using PI.SearchApi.Pipeline;
using Xunit;

namespace PI.SearchApi.Tests.Pipeline;

public sealed class PromptTemplateTests
{
    [Fact]
    public void Render_EveryPlaceholderHasAValue_PastesTheValuesIn()
    {
        var rendered = PromptTemplate.Render("Q: {{question}}\nE: {{evidence}} ({{question}})", new Dictionary<string, string>
        {
            ["question"] = "power adapter",
            ["evidence"] = "[PROD-0012]",
        });

        Assert.Equal("Q: power adapter\nE: [PROD-0012] (power adapter)", rendered);
    }

    [Fact]
    public void Render_PlaceholderWithNoValue_Throws()
    {
        var exception = Assert.Throws<InvalidOperationException>(() =>
            PromptTemplate.Render("{{question}} {{evidnce}}", new Dictionary<string, string> { ["question"] = "q" }));

        Assert.Contains("evidnce", exception.Message);
    }

    [Theory]
    [InlineData("rag-system.md")]
    [InlineData("rag-user.md")]
    public void PromptLibrary_ShippedPrompt_IsCopiedToTheOutput(string fileName)
    {
        var library = new PromptLibrary(Path.Combine(AppContext.BaseDirectory, "assets", "prompts"));

        Assert.False(string.IsNullOrWhiteSpace(library.Get(fileName)));
    }
}

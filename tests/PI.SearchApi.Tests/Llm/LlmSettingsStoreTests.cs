using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using PI.SearchApi.Llm;
using Xunit;

namespace PI.SearchApi.Tests.Llm;

public sealed class LlmSettingsStoreTests : IDisposable
{
    private readonly string home = Path.Combine(Path.GetTempPath(), "needle-settings-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(home))
        {
            Directory.Delete(home, recursive: true);
        }
    }

    private LlmSettingsStore Create() => new(
        new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { [LlmSettingsStore.HomeSetting] = home }).Build(),
        NullLogger<LlmSettingsStore>.Instance);

    [Fact]
    public void Current_NoFile_IsTheDefaults()
    {
        var settings = Create().Current;

        Assert.Empty(settings.Providers);
        Assert.Null(settings.For(LlmProviders.Ollama).BaseUrl);
    }

    [Fact]
    public void Update_IsSavedAndReadBackByANewStore()
    {
        Create().Update(s => s with
        {
            Providers = new Dictionary<string, LlmProviderSettings>
            {
                [LlmProviders.Azure] = new() { Enabled = true, BaseUrl = "https://my-resource.openai.azure.com/openai/v1", ExtraModels = ["gpt-5-mini"] },
            },
        });

        var reloaded = Create().Current.For(LlmProviders.Azure);

        Assert.True(reloaded.Enabled);
        Assert.Equal("https://my-resource.openai.azure.com/openai/v1", reloaded.BaseUrl);
        Assert.Equal(["gpt-5-mini"], reloaded.ExtraModels);
        Assert.False(File.Exists(Path.Combine(home, "settings.json.tmp")));
    }

    [Fact]
    public void Current_UnreadableFile_FallsBackToTheDefaults()
    {
        Directory.CreateDirectory(home);
        File.WriteAllText(Path.Combine(home, "settings.json"), "{ not json");

        Assert.Empty(Create().Current.Providers);
    }
}

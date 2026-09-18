using System.Text.Json;
using PI.SearchApi.IntegrationTests.SearchApi;
using Xunit;

namespace PI.SearchApi.IntegrationTests.GoldenQueries;

// LLM stages are checked structurally only: what was cited and whether it was in the evidence, never the wording.
[Collection(AppHostCollection.Name)]
public sealed class RagStageGoldenQueryTests(AppHostFixture fixture)
{
    private static readonly string[] CompatibleChargers = ["PROD-0011", "PROD-0012"];

    [Fact]
    public async Task GQ03_Rag_EvidenceIncludesTheCompatibleChargerAndTheNearMiss()
    {
        // Talk moment: the model is given the near miss on purpose, with its reasons, so it can warn about it.
        RepositoryPaths.SkipUnlessNomicModelIsPresent();
        using var client = fixture.CreateSearchApiClient();

        var response = await SearchApiClient.SearchAsync(client, "rag", GoldenQueryCase.Load("GQ-03").Request, TestContext.Current.CancellationToken);

        var evidenceStep = response.DebugTrace.Steps[^1];
        Assert.Equal("rag", evidenceStep.Stage);
        Assert.NotNull(evidenceStep.Sql);

        var evidence = evidenceStep.Details!["evidence"].EnumerateArray()
            .ToDictionary(e => e.GetProperty("id").GetString()!, e => e.GetProperty("role").GetString());
        Assert.Equal("TargetDevice", evidence["PROD-0001"]);
        Assert.Equal("Compatible", evidence["PROD-0012"]);
        Assert.Equal("Incompatible", evidence["PROD-0014"]);
    }

    [Fact]
    public async Task GQ03_Rag_AnswerCitesACompatibleChargerFromTheEvidence()
    {
        // Talk moment: a grounded answer: it recommends a charger that fits, and every citation is checkable.
        RepositoryPaths.SkipUnlessNomicModelIsPresent();
        using var client = fixture.CreateSearchApiClient();

        var answer = await AnswerApiClient.AnswerAsync(client, "rag", GoldenQueryCase.Load("GQ-03").Request, TestContext.Current.CancellationToken);

        var section = Assert.Single(answer.Sections);
        Assert.Equal("answer", section.Section);
        Assert.False(section.InsufficientEvidence, section.Markdown);
        Assert.Contains(section.Citations, CompatibleChargers.Contains);
        Assert.All(section.Citations, id => Assert.Contains(id, answer.Evidence));
        Assert.Empty(section.InvalidCitations);
        Assert.DoesNotContain(section.Warnings, w => w.StartsWith("Cited", StringComparison.Ordinal));
        Assert.Equal(
            ["Prompt: the answer's system and user messages", "Generate: the streamed answer", "Validate: citations, sentinel and warnings"],
            answer.Trace.Select(step => step.Title));
    }

    [Fact]
    public async Task GQ09_Rag_NoDeviceButStatedRequirements_AnswersWithACompatibleCharger()
    {
        // Talk moment: with no device, the evidence says what the shopper asked for and which chargers meet it,
        // so the answer can recommend one instead of saying there isn't enough evidence.
        RepositoryPaths.SkipUnlessNomicModelIsPresent();
        using var client = fixture.CreateSearchApiClient();
        var request = GoldenQueryCase.Load("GQ-09").Request;

        var response = await SearchApiClient.SearchAsync(client, "rag", request, TestContext.Current.CancellationToken);
        var evidenceStep = response.DebugTrace.Steps[^1];
        Assert.Equal(2, evidenceStep.Details!["statedRequirements"].GetArrayLength());
        var compatible = evidenceStep.Details["evidence"].EnumerateArray()
            .Where(e => e.GetProperty("role").GetString() == "Compatible")
            .Select(e => e.GetProperty("id").GetString()!)
            .ToHashSet();
        Assert.Contains("PROD-0012", compatible);

        var answer = await AnswerApiClient.AnswerAsync(client, "rag", request, TestContext.Current.CancellationToken);

        var section = Assert.Single(answer.Sections);
        Assert.False(section.InsufficientEvidence, section.Markdown);
        Assert.Contains(section.Citations, compatible.Contains);
        Assert.True(
            section.InvalidCitations.Count == 0,
            $"Cited outside the evidence: {string.Join(", ", section.InvalidCitations)}\n{section.Markdown}");
    }

    [Fact]
    public async Task GQ03_Rag_EventStreamRunsFromMetaThroughToDone()
    {
        // Talk moment: results don't wait for the LLM; the answer streams in as Server-Sent Events.
        RepositoryPaths.SkipUnlessNomicModelIsPresent();
        using var client = fixture.CreateSearchApiClient();

        var events = await AnswerApiClient.ReadEventStreamAsync(client, "rag", GoldenQueryCase.Load("GQ-03").Request, TestContext.Current.CancellationToken);

        var names = events.Select(e => e.Name).ToList();
        Assert.Equal("meta", names[0]);
        Assert.Contains("delta", names);
        Assert.Equal(["final", "done"], names[^2..]);
        Assert.DoesNotContain("error", names);

        using var final = JsonDocument.Parse(events[^2].Data);
        Assert.Equal("answer", final.RootElement.GetProperty("section").GetString());
    }
}

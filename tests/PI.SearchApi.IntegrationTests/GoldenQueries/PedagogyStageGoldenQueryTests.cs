using System.Text.Json.Nodes;
using PI.SearchApi.IntegrationTests.SearchApi;
using Xunit;

namespace PI.SearchApi.IntegrationTests.GoldenQueries;

// Structural assertions only (tests CLAUDE.md): which products the explanation chose and whether its structure holds, never the wording.
[Collection(AppHostCollection.Name)]
public sealed class PedagogyStageGoldenQueryTests(AppHostFixture fixture)
{
    private static readonly string[] CompatibleChargers = ["PROD-0011", "PROD-0012", "PROD-0013"];
    private static readonly string[] IncompatibleChargers = ["PROD-0014", "PROD-0015", "PROD-0016"];

    [Fact]
    public async Task GQ01_Pedagogy_ExplainsTheCompatibleChargerWithTheNearMissAsCounterExample()
    {
        // Talk moment: pedagogy turns the near miss, the system's hardest case, into the shopper's clearest lesson.
        RepositoryPaths.SkipUnlessNomicModelIsPresent();
        using var client = fixture.CreateSearchApiClient();

        var answer = await AnswerApiClient.AnswerAsync(client, "pedagogy", Request(audience: "novice", applyPedagogy: true), TestContext.Current.CancellationToken);

        Assert.Equal(["answer", "explanation"], answer.Sections.Select(s => s.Section));
        var explanation = answer.Sections[1];
        var structure = Assert.IsType<ExplanationStructureDto>(explanation.Structure);
        Assert.Contains(structure.Decision.ProductId, CompatibleChargers);
        Assert.Contains(structure.NearMiss.ProductId, IncompatibleChargers);
        Assert.NotEmpty(structure.Concepts);
        Assert.Empty(explanation.InvalidCitations);
        Assert.Null(answer.Sections[0].Structure);
    }

    [Fact]
    public async Task GQ01_Pedagogy_BaselineHasNoStructureAndNoCitationWarnings()
    {
        // Talk moment: the baseline gets the same facts and audience through a plain prompt; only citations are checked.
        RepositoryPaths.SkipUnlessNomicModelIsPresent();
        using var client = fixture.CreateSearchApiClient();

        var answer = await AnswerApiClient.AnswerAsync(client, "pedagogy", Request(audience: "novice", applyPedagogy: false), TestContext.Current.CancellationToken);

        var explanation = answer.Sections[^1];
        Assert.Equal("explanation", explanation.Section);
        Assert.Null(explanation.Structure);
        Assert.Empty(explanation.InvalidCitations);
        Assert.DoesNotContain(explanation.Warnings, w => w.StartsWith("Cited", StringComparison.Ordinal));
        Assert.Contains(answer.Trace, step => step.Title == "Validate: citations (structure checks not applied)");
    }

    private static JsonObject Request(string audience, bool applyPedagogy)
    {
        var request = GoldenQueryCase.Load("GQ-01").Request.DeepClone().AsObject();
        request["options"] = new JsonObject { ["audience"] = audience, ["applyPedagogy"] = applyPedagogy };
        return request;
    }
}

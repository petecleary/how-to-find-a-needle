using System.Globalization;
using System.Text;
using PI.SearchApi.IntegrationTests.SearchApi;
using Xunit;

namespace PI.SearchApi.IntegrationTests.GoldenQueries;

/// <summary>
/// Runs a golden query against one stage and checks every expectation for that stage. A failure
/// lists every broken expectation and the actual top 10, because when a moment stops working the
/// first question is always "what did it rank instead?" (ADR-0005: fix the wording, not the test).
/// </summary>
public static class GoldenQueryRunner
{
    public static async Task<SearchResponseDto> RunAsync(AppHostFixture fixture, string goldenQueryId, string stage)
    {
        var goldenQuery = GoldenQueryCase.Load(goldenQueryId);
        Assert.True(
            goldenQuery.Expectations.TryGetValue(stage, out var expectations),
            $"{goldenQueryId} has no expectations for the '{stage}' stage.");

        using var client = fixture.CreateSearchApiClient();
        var response = await SearchApiClient.SearchAsync(client, stage, goldenQuery.Request, TestContext.Current.CancellationToken);

        var failures = expectations!.SelectMany(e => Check(e, response)).ToList();

        if (failures.Count > 0)
        {
            Assert.Fail(Describe(goldenQuery, stage, failures, response));
        }

        return response;
    }

    private static IEnumerable<string> Check(GoldenExpectation expectation, SearchResponseDto response)
    {
        var index = response.Results.ToList().FindIndex(r => r.Id == expectation.ProductId);
        var rank = index >= 0 ? index + 1 : (int?)null;
        var result = index >= 0 ? response.Results[index] : null;
        var id = expectation.ProductId;

        if (expectation.Present is { } present && present != (rank is not null))
        {
            yield return present ? $"{id} should be present but is missing" : $"{id} should be absent but is at rank {rank}";
        }

        if (expectation.Rank is { } bound)
        {
            if (rank is null)
            {
                yield return $"{id} should rank within {Format(bound)} but was not retrieved";
            }
            else if (!Within(rank.Value, bound))
            {
                yield return $"{id} should rank within {Format(bound)} but is at rank {rank}";
            }
        }

        if (expectation.AbsentFromTop is { } top && rank is { } r && r <= top)
        {
            yield return $"{id} should be absent from the top {top} but is at rank {r}";
        }

        if (expectation.Compatibility is { } compatibility)
        {
            if (result is null)
            {
                yield return $"{id} should be {compatibility} but was not retrieved";
            }
            else if (result.Compatibility.Status != compatibility)
            {
                yield return $"{id} should be {compatibility} but is {result.Compatibility.Status}";
            }
            else if (compatibility == "Incompatible" && result.Compatibility.Reasons.Count == 0)
            {
                yield return $"{id} is Incompatible but has no reasons — flagged items must say why";
            }
        }

        if (expectation.ConceptMatch is { } conceptMatch)
        {
            if (result is null)
            {
                yield return $"{id} should be {conceptMatch} but was not retrieved";
            }
            else if (result.Signals.ConceptMatch != conceptMatch)
            {
                yield return $"{id} should be {conceptMatch} but is {result.Signals.ConceptMatch ?? "null"}";
            }
        }

        if (expectation.KeywordRank is { } keywordBound)
        {
            var keywordRank = result?.Signals.KeywordRank;

            if (keywordRank is null || !Within(keywordRank.Value, keywordBound))
            {
                yield return $"{id} should have a keyword rank within {Format(keywordBound)} but has {keywordRank?.ToString(CultureInfo.InvariantCulture) ?? "none"}";
            }
        }
    }

    private static bool Within(int rank, RankBound bound) =>
        (bound.Min is null || rank >= bound.Min) && (bound.Max is null || rank <= bound.Max);

    private static string Format(RankBound bound) => (bound.Min, bound.Max) switch
    {
        (null, { } max) => $"the top {max}",
        ({ } min, null) => $"rank {min} or lower",
        ({ } min, { } max) => $"ranks {min}–{max}",
        _ => "any rank",
    };

    private static string Describe(GoldenQueryCase goldenQuery, string stage, List<string> failures, SearchResponseDto response)
    {
        var message = new StringBuilder()
            .AppendLine(CultureInfo.InvariantCulture, $"{goldenQuery.Id} \"{goldenQuery.Title}\" on the {stage} stage:")
            .AppendLine(CultureInfo.InvariantCulture, $"  Moment: {goldenQuery.Moment}");

        foreach (var failure in failures)
        {
            message.AppendLine(CultureInfo.InvariantCulture, $"  ✗ {failure}");
        }

        message.AppendLine("  Actual top 10:");

        foreach (var (result, i) in response.Results.Take(10).Select((r, i) => (r, i)))
        {
            message.AppendLine(CultureInfo.InvariantCulture,
                $"    {i + 1,2}. {result.Id} {result.Name} | score {result.Score:0.#####} | kw {result.Signals.KeywordRank} vec {result.Signals.VectorRank} ({result.Signals.VectorDistance:0.####}) fused {result.Signals.FusedRank} | {result.Signals.ConceptMatch} {result.Compatibility.Status}");
        }

        return message.ToString();
    }
}

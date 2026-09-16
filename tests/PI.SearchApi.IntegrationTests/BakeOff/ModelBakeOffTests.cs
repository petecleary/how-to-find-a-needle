using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Aspire.Hosting;
using Aspire.Hosting.ApplicationModel;
using Aspire.Hosting.Testing;
using PI.SearchApi.IntegrationTests.SearchApi;
using Xunit;

namespace PI.SearchApi.IntegrationTests.BakeOff;

// Model bake-off (ADR-0015): which local model should the presenter use? Opt-in, because it runs for a long time.
//
//   PI_BAKEOFF_MODELS=qwen3.6:35b,gemma4:31b dotnet test tests/PI.SearchApi.IntegrationTests --filter ModelBakeOff
//
// For each model it starts the AppHost with Llm__Model set, then sends every golden query that has a query to Stage 6
// and to Stage 7 (novice, pedagogy on and off), PI_BAKEOFF_RUNS times each (10 by default), through the real answer
// endpoints in JSON mode. It scores what the API's own validators report, never the wording, and writes a markdown
// report to TestResults/ for ADR-0015.
[Collection(BakeOffCollection.Name)]
public sealed class ModelBakeOffTests
{
    private const string ModelsVariable = "PI_BAKEOFF_MODELS";
    private const string RunsVariable = "PI_BAKEOFF_RUNS";
    private static readonly TimeSpan StartupTimeout = TimeSpan.FromMinutes(3);

    private static readonly Scenario[] Scenarios =
    [
        new("Stage 6", "rag", Audience: null, ApplyPedagogy: null),
        new("Stage 7, pedagogy on", "pedagogy", "novice", ApplyPedagogy: true),
        new("Stage 7, baseline", "pedagogy", "novice", ApplyPedagogy: false),
    ];

    [Fact]
    public async Task ModelBakeOff_CandidateModels_RecordsValidationAndLatency()
    {
        var modelsSetting = Environment.GetEnvironmentVariable(ModelsVariable);
        Assert.SkipWhen(
            string.IsNullOrWhiteSpace(modelsSetting),
            $"Opt-in: set {ModelsVariable} (e.g. qwen3.6:35b,gemma4:31b) to run the model bake-off.");
        RepositoryPaths.SkipUnlessNomicModelIsPresent();

        var models = modelsSetting!.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var runs = int.TryParse(Environment.GetEnvironmentVariable(RunsVariable), out var configuredRuns) ? configuredRuns : 10;
        var queries = GoldenQueriesWithAQuery();
        var ct = TestContext.Current.CancellationToken;
        var results = new List<RunResult>();
        var finishedModels = new List<string>();

        var path = Path.Combine(
            RepositoryPaths.Root, "tests", "PI.SearchApi.IntegrationTests", "TestResults",
            $"model-bake-off-{DateTime.Now:yyyyMMdd-HHmm}.md");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        foreach (var model in models)
        {
            results.AddRange(await RunModelAsync(model, queries, runs, ct));
            finishedModels.Add(model);

            // Written after each model, not at the end: a slow model can be abandoned without losing the others
            // (2026-09-15: gemma4:31b ran ~5× slower than qwen3.6:35b, and stopping lost the whole run).
            await File.WriteAllTextAsync(path, Report(finishedModels, runs, queries, results), ct);
            TestContext.Current.SendDiagnosticMessage($"Bake-off report after {model}: {path}");
        }

        // Structural sanity only: every request got an answer. Choosing a model is a judgement made from the report.
        Assert.All(results, result => Assert.True(result.Error is null, $"{result.Model} {result.QueryId} {result.Scenario.Name}: {result.Error}"));
    }

    private static async Task<List<RunResult>> RunModelAsync(string model, IReadOnlyList<(string Id, JsonObject Request)> queries, int runs, CancellationToken ct)
    {
        using var startup = CancellationTokenSource.CreateLinkedTokenSource(ct);
        startup.CancelAfter(StartupTimeout);

        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.PI_AppHost>(startup.Token);
        // The only difference between runs: the model the API's one IChatClient uses (ADR-0015).
        appHost.CreateResourceBuilder<ProjectResource>("searchapi").WithEnvironment("Llm__Model", model);

        await using var app = await appHost.BuildAsync(startup.Token);
        await app.StartAsync(startup.Token);
        await app.ResourceNotifications.WaitForResourceHealthyAsync("searchapi", startup.Token);

        using var client = app.CreateHttpClient("searchapi");
        client.Timeout = TimeSpan.FromMinutes(3);

        // One untimed request first, so the model's load time isn't counted against its first query.
        await SendAsync(client, model, queries[0], Scenarios[0], ct);

        var results = new List<RunResult>();
        for (var run = 1; run <= runs; run++)
        {
            foreach (var query in queries)
            {
                foreach (var scenario in Scenarios)
                {
                    results.Add(await SendAsync(client, model, query, scenario, ct));
                }
            }
        }

        return results;
    }

    private static async Task<RunResult> SendAsync(HttpClient client, string model, (string Id, JsonObject Request) query, Scenario scenario, CancellationToken ct)
    {
        var request = query.Request.DeepClone().AsObject();
        if (scenario.Audience is not null)
        {
            request["options"] = new JsonObject { ["audience"] = scenario.Audience, ["applyPedagogy"] = scenario.ApplyPedagogy };
        }

        try
        {
            var answer = await AnswerApiClient.AnswerAsync(client, scenario.Stage, request, ct);
            var sections = answer.Sections;
            var explanation = sections.FirstOrDefault(s => s.Section == "explanation");

            return new RunResult(
                model, query.Id, scenario,
                Error: null,
                InvalidCitations: sections.Sum(s => s.InvalidCitations.Count),
                HasWarnings: sections.Any(s => s.Warnings.Count > 0),
                InsufficientEvidence: sections.Any(s => s.InsufficientEvidence),
                StructurePassed: scenario.ApplyPedagogy == true ? StructureChecksPassed(answer) : null,
                TimeToFirstTokenMs: answer.TimeToFirstTokenMs,
                TotalMs: answer.TotalMs,
                ExplanationWarnings: explanation?.Warnings ?? []);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            return new RunResult(model, query.Id, scenario, exception.Message, 0, false, false, null, null, 0, []);
        }
    }

    // Every non-heuristic check in the explanation's validation step passed: headings, Decision, Near miss.
    private static bool StructureChecksPassed(AnswerResponseDto answer)
    {
        var step = answer.Trace.LastOrDefault(s => s.Title.StartsWith("Validate: citations and teaching structure", StringComparison.Ordinal));
        if (step?.Details is null || !step.Details.TryGetValue("checks", out var checks))
        {
            return false;
        }

        return checks.EnumerateArray()
            .Where(check => !check.GetProperty("isHeuristic").GetBoolean())
            .All(check => check.GetProperty("passed").GetBoolean());
    }

    private static List<(string Id, JsonObject Request)> GoldenQueriesWithAQuery()
    {
        var file = JsonNode.Parse(File.ReadAllText(RepositoryPaths.GoldenQueriesFile))!.AsArray();

        return
        [
            .. file
                .Select(node => node!.AsObject())
                .Where(query => !string.IsNullOrWhiteSpace(query["request"]?["query"]?.GetValue<string>()))
                .Select(query => (query["id"]!.GetValue<string>(), query["request"]!.AsObject())),
        ];
    }

    private static string Report(IReadOnlyList<string> models, int runs, IReadOnlyList<(string Id, JsonObject Request)> queries, List<RunResult> results)
    {
        var text = new StringBuilder()
            .AppendLine(CultureInfo.InvariantCulture, $"# Model bake-off, {DateTime.Now:yyyy-MM-dd HH:mm}")
            .AppendLine()
            .AppendLine(CultureInfo.InvariantCulture, $"Models: {string.Join(", ", models)}. Golden queries: {string.Join(", ", queries.Select(q => q.Id))}. {runs} runs of each query per scenario, after one untimed warm-up request per model.")
            .AppendLine()
            .AppendLine("| Model | Scenario | Requests | Failed | No invalid citations | No warnings | Structure checks pass | Insufficient evidence | First token median / max | Total median / max |")
            .AppendLine("|---|---|---|---|---|---|---|---|---|---|");

        foreach (var model in models)
        {
            foreach (var scenario in Scenarios)
            {
                var rows = results.Where(r => r.Model == model && r.Scenario == scenario).ToList();
                var answered = rows.Where(r => r.Error is null).ToList();
                var structure = answered.Where(r => r.StructurePassed is not null).ToList();

                text.AppendLine(CultureInfo.InvariantCulture,
                    $"| {model} | {scenario.Name} | {rows.Count} | {rows.Count - answered.Count} | {Share(answered, r => r.InvalidCitations == 0)} | {Share(answered, r => !r.HasWarnings)} | {(structure.Count == 0 ? "—" : Share(structure, r => r.StructurePassed == true))} | {answered.Count(r => r.InsufficientEvidence)} | {Timing(answered.Select(r => r.TimeToFirstTokenMs))} | {Timing(answered.Select(r => (double?)r.TotalMs))} |");
            }
        }

        text.AppendLine()
            .AppendLine("## By golden query")
            .AppendLine()
            .AppendLine("| Model | Query | Scenario | No invalid citations | No warnings | Structure checks pass | Total median |")
            .AppendLine("|---|---|---|---|---|---|---|");

        foreach (var group in results.Where(r => r.Error is null).GroupBy(r => (r.Model, r.QueryId, r.Scenario.Name)))
        {
            var rows = group.ToList();
            var structure = rows.Where(r => r.StructurePassed is not null).ToList();
            text.AppendLine(CultureInfo.InvariantCulture,
                $"| {group.Key.Model} | {group.Key.QueryId} | {group.Key.Name} | {Share(rows, r => r.InvalidCitations == 0)} | {Share(rows, r => !r.HasWarnings)} | {(structure.Count == 0 ? "—" : Share(structure, r => r.StructurePassed == true))} | {Timing(rows.Select(r => (double?)r.TotalMs), medianOnly: true)} |");
        }

        var commonWarnings = results
            .SelectMany(r => r.ExplanationWarnings.Select(w => (r.Model, Warning: w.Length > 90 ? w[..90] + "…" : w)))
            .GroupBy(w => w)
            .OrderByDescending(g => g.Count())
            .Take(15)
            .ToList();

        if (commonWarnings.Count > 0)
        {
            text.AppendLine().AppendLine("## Most common explanation warnings").AppendLine();
            foreach (var warning in commonWarnings)
            {
                text.AppendLine(CultureInfo.InvariantCulture, $"- {warning.Key.Model} ×{warning.Count()}: {warning.Key.Warning}");
            }
        }

        return text.ToString();
    }

    private static string Share(List<RunResult> rows, Func<RunResult, bool> predicate) =>
        rows.Count == 0 ? "—" : $"{rows.Count(predicate)}/{rows.Count}";

    private static string Timing(IEnumerable<double?> values, bool medianOnly = false)
    {
        var sorted = values.OfType<double>().Order().ToList();
        if (sorted.Count == 0)
        {
            return "—";
        }

        var median = sorted[sorted.Count / 2];
        return medianOnly ? Seconds(median) : $"{Seconds(median)} / {Seconds(sorted[^1])}";
    }

    private static string Seconds(double milliseconds) =>
        milliseconds < 1000
            ? string.Create(CultureInfo.InvariantCulture, $"{milliseconds:0} ms")
            : string.Create(CultureInfo.InvariantCulture, $"{milliseconds / 1000:0.0} s");

    private sealed record Scenario(string Name, string Stage, string? Audience, bool? ApplyPedagogy);

    private sealed record RunResult(
        string Model,
        string QueryId,
        Scenario Scenario,
        string? Error,
        int InvalidCitations,
        bool HasWarnings,
        bool InsufficientEvidence,
        bool? StructurePassed,
        double? TimeToFirstTokenMs,
        double TotalMs,
        IReadOnlyList<string> ExplanationWarnings);
}

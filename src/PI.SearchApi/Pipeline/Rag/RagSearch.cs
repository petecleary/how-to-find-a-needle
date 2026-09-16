using System.Diagnostics;
using Npgsql;
using PI.SearchApi.Contracts;
using PI.SearchApi.Pipeline.Ontology;

namespace PI.SearchApi.Pipeline.Rag;

// Stage 6 — RAG: retrieval and evidence (the results request)
//
// What:     Runs the whole Stage 5 pipeline, then chooses a bounded evidence set from its results and reads the
//           chosen products' descriptions. Returns Stage 5's candidates with one more trace step: the evidence.
// Strength: RAG adds no retrieval of its own. It decides *what to say* from results that were already relevant,
//           related and constrained, which is why the results can render before any LLM text exists.
// Failure:  The answer can only be as good as this evidence. If Stage 5 missed a product or the limits cut it,
//           the model can't mention it, however fluent it is.
// Decision: docs/decisions/0016-rag-grounding-and-citations.md
public sealed class RagSearch(
    IOntologySearch ontologySearch,
    EvidenceSetBuilder evidenceBuilder,
    NpgsqlDataSource dataSource) : IRagSearch
{
    // Stage 5 carries product summaries without descriptions, so the evidence reads them for just the chosen IDs.
    // = ANY(@ids) takes a Postgres text[] parameter: one statement however many IDs, still fully parameterised.
    private const string DescriptionsSql = """
        SELECT id, description
        FROM products
        WHERE id = ANY(@ids);
        """;

    public async Task<RagSearchResult> SearchAsync(SearchRequest request, string stage, CancellationToken ct)
    {
        var stage5 = await ontologySearch.SearchWithContextAsync(request, ct);

        using var activity = PipelineTelemetry.Source.StartActivity($"Stage {(stage == "rag" ? 6 : 7)}: evidence set");
        var start = Stopwatch.GetTimestamp();

        var evidence = evidenceBuilder.Build(
            stage5.Result.Candidates,
            stage5.Device.Product,
            stage5.Understanding.TaxonomyConcepts,
            stage5.Checks,
            stage5.Requirements);

        var ids = evidence.ProductIds.ToArray();
        var descriptions = await ReadDescriptionsAsync(ids, ct);
        evidence = evidence.WithDescriptions(descriptions);

        var evidenceStep = EvidenceStep(stage, evidence, ids, PipelineTelemetry.ElapsedMs(start));

        return new RagSearchResult(new StageResult(stage5.Result.Candidates, [.. stage5.Result.Trace, evidenceStep]), evidence);
    }

    private async Task<IReadOnlyDictionary<string, string>> ReadDescriptionsAsync(string[] ids, CancellationToken ct)
    {
        var descriptions = new Dictionary<string, string>();

        if (ids.Length == 0)
        {
            return descriptions;
        }

        await using var command = dataSource.CreateCommand(DescriptionsSql);
        command.Parameters.AddWithValue("ids", ids);

        await using var reader = await command.ExecuteReaderAsync(ct);

        while (await reader.ReadAsync(ct))
        {
            descriptions[reader.GetString(0)] = reader.GetString(1);
        }

        return descriptions;
    }

    private static TraceStep EvidenceStep(string stage, EvidenceSet evidence, string[] ids, double durationMs) => new()
    {
        Stage = stage,
        Title = "Evidence: what the model is given",
        DurationMs = durationMs,
        Sql = DescriptionsSql,
        Parameters = new Dictionary<string, object?> { ["ids"] = ids },
        Details = new Dictionary<string, object?>
        {
            ["evidence"] = evidence.Items.Select(item => new
            {
                id = item.Product.Id,
                name = item.Product.Name,
                role = item.Role.ToString(),
                rank = item.Rank,
                compatibility = item.Compatibility.Status.ToString(),
                reasons = item.Compatibility.Reasons,
                fits = item.Compatibility.Fits,
                conceptMatch = item.ConceptMatch?.ToString(),
                whyIncluded = item.WhyIncluded,
                description = item.Description,
            }).ToList(),
            ["limits"] = new
            {
                compatible = EvidenceLimits.Compatible,
                incompatible = EvidenceLimits.Incompatible,
                unknown = EvidenceLimits.Unknown,
                notChecked = EvidenceLimits.NotChecked,
                descriptionCharacters = EvidenceFormatter.MaxDescriptionLength,
            },
            ["statedRequirements"] = evidence.StatedRequirements,
            ["concepts"] = evidence.Concepts,
            ["rules"] = evidence.Rules,
        },
        Notes =
        [
            "The model is given only these products, in Stage 5's order; anything else it mentions is flagged when the answer is validated.",
            "Incompatible products are included on purpose, with their reasons, so the answer can warn about the near miss instead of recommending it.",
            "Out-of-concept products are left out, and each kind of product has a limit: a small, clear context grounds a small model better than fifty items.",
            "Retrieval is deterministic, so the answer request re-runs it and builds exactly this evidence set.",
        ],
    };
}

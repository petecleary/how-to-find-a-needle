using System.Diagnostics;
using System.Globalization;
using Npgsql;
using PI.SearchApi.Contracts;
using PI.SearchApi.Embeddings;
using PgVector = Pgvector.Vector;

namespace PI.SearchApi.Pipeline.Vector;

// Stage 3 — Vector search (pgvector)
//
// What:     Embeds the query with the local Nomic model, then orders products by cosine
//           distance to it (pgvector's <=> operator, HNSW index), after the request's filters.
// Strength: Finds meaning rather than words: "power brick" lands near "AC adapter" and
//           "laptop charger" although they share no terms.
// Failure:  Similarity is not compatibility. A 45W barrel charger reads almost exactly like
//           the 65W USB-C one, so it ranks near the top. And there's no threshold: nonsense
//           queries still return their nearest neighbours.
// Decision: docs/decisions/0010-vector-search-pgvector.md
public sealed class VectorSearch(NpgsqlDataSource dataSource, ISearchEmbedder embedder, SqlFilterBuilder filterBuilder) : IVectorSearch
{
    // HNSW explores ef_search candidates per query. pgvector's default of 40 would silently return fewer
    // than the default candidateDepth of 50, so it's raised for this transaction only.
    // set_config(name, value, true) is SET LOCAL in function form — unlike SET, it accepts a parameter.
    private const int MinimumEfSearch = 100;

    private const string SetEfSearchSql = """
        SELECT set_config('hnsw.ef_search', @efSearch, true);
        """;

    //   embedding_model = @activeModel — only vectors from the configured model; models are never mixed.
    //   <=>                            — cosine distance: 0 = same direction (same meaning), 1 = unrelated, 2 = opposite.
    //   ORDER BY ... <=> ..., id       — nearest first; id breaks ties so results are deterministic.
    //   {where}                        — the shared filters, in the same statement: they narrow first,
    //                                    and similarity only orders what already satisfies them.
    private const string SearchSqlTemplate = $"""
        SELECT {ProductRows.Columns},
               embedding_dense <=> @queryVector AS distance
        FROM products
        {"{where}"}
        ORDER BY embedding_dense <=> @queryVector, id
        LIMIT @candidateDepth;
        """;

    public async Task<StageResult> SearchAsync(SearchRequest request, string? embeddingText, CancellationToken ct)
    {
        using var activity = PipelineTelemetry.Source.StartActivity("Stage 3: vector search");

        var queryEmbedding = await embedder.EmbedQueryAsync(embeddingText ?? request.Query, ct);

        var start = Stopwatch.GetTimestamp();
        var filter = filterBuilder.Build(request.Filters);
        var searchSql = SearchSqlTemplate.Replace("{where}", filter.WhereClause("embedding_model = @activeModel"));
        var efSearch = Math.Max(MinimumEfSearch, request.Options.CandidateDepth);

        var queryVector = new PgVector(queryEmbedding.Vector);
        var parameters = new SqlParameters()
            .Add("queryVector", queryVector, traceValue: AbbreviateVector(queryEmbedding.Vector))
            .Add("activeModel", embedder.ModelId)
            .AddRange(filter.Parameters)
            .Add("candidateDepth", request.Options.CandidateDepth);

        await using var connection = await dataSource.OpenConnectionAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);

        await using (var command = new NpgsqlCommand(SetEfSearchSql, connection, transaction))
        {
            command.Parameters.AddWithValue("efSearch", efSearch.ToString(CultureInfo.InvariantCulture));
            await command.ExecuteScalarAsync(ct);
        }

        IReadOnlyList<string>? explainPlan = null;
        if (request.Options.Explain)
        {
            explainPlan = await ExplainAsync(connection, transaction, searchSql, parameters, ct);
        }

        var candidates = new List<Candidate>();
        var distances = new List<object>();

        await using (var command = new NpgsqlCommand(searchSql, connection, transaction))
        {
            parameters.ApplyTo(command);
            await using var reader = await command.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                var product = ProductRows.Read(reader);
                var distance = Math.Round(reader.GetDouble(reader.GetOrdinal("distance")), 5);
                var similarity = Math.Round(1 - distance, 5);
                var rank = candidates.Count + 1;

                candidates.Add(new Candidate(
                    product,
                    similarity,
                    new CandidateSignals { VectorRank = rank, VectorDistance = distance },
                    CompatibilityResult.NotEvaluated));

                distances.Add(new { id = product.Id, rank, distance, similarity });
            }
        }

        await transaction.CommitAsync(ct);

        var notes = new List<string>
        {
            "score = 1 − cosine distance (cosine similarity). Vectors are unit length, so inner product would rank identically.",
            "Similarity is not compatibility: a product can read exactly like the right answer and still not fit.",
            "There is no similarity threshold, so vector search always returns its nearest neighbours — even for nonsense.",
            "Filters run in the same SQL statement: they remove wrong answers first, then similarity orders the rest.",
            $"hnsw.ef_search = {efSearch} for this transaction; pgvector's default of 40 would cap results below candidateDepth.",
            "With a filter, an HNSW index finds neighbours first and filters after, which can lose recall. At this catalog size the planner usually scans exactly; production systems use hnsw.iterative_scan (pgvector ≥ 0.8), partial indexes or over-fetching.",
        };

        if (candidates.Count == 0)
        {
            notes.Add($"No product has a vector from {embedder.ModelId} that passes the filters. If there are no filters, seeding couldn't embed the catalog: check the startup log.");
        }

        var details = new Dictionary<string, object?>(filter.Details)
        {
            ["metric"] = "cosine distance (<=>)",
            ["activeModel"] = embedder.ModelId,
            ["efSearch"] = efSearch,
            ["distances"] = distances,
            ["optionsUsed"] = new { request.Options.CandidateDepth, request.Options.Explain },
        };

        if (explainPlan is not null)
        {
            details["explainPlan"] = explainPlan;
            details["usedHnswIndex"] = explainPlan.Any(line => line.Contains("ix_products_dense", StringComparison.Ordinal));
        }

        var searchStep = new TraceStep
        {
            Stage = "vector",
            Title = "pgvector cosine distance",
            DurationMs = PipelineTelemetry.ElapsedMs(start),
            Sql = SetEfSearchSql + "\n" + searchSql,
            Parameters = new Dictionary<string, object?>(parameters.ForTrace()) { ["efSearch"] = efSearch },
            Details = details,
            Notes = notes,
        };

        return new StageResult(candidates, [queryEmbedding.Trace, searchStep]);
    }

    private static async Task<IReadOnlyList<string>> ExplainAsync(
        NpgsqlConnection connection, NpgsqlTransaction transaction, string searchSql, SqlParameters parameters, CancellationToken ct)
    {
        await using var command = new NpgsqlCommand("EXPLAIN " + searchSql, connection, transaction);
        parameters.ApplyTo(command);

        var lines = new List<string>();
        await using var reader = await command.ExecuteReaderAsync(ct);

        while (await reader.ReadAsync(ct))
        {
            lines.Add(reader.GetString(0));
        }

        return lines;
    }

    private static string AbbreviateVector(float[] vector) =>
        "[" + string.Join(", ", vector.Take(4).Select(v => v.ToString("0.0000", CultureInfo.InvariantCulture)))
        + $", … {vector.Length} dimensions]";
}

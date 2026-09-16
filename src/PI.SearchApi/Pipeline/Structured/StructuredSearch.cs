using System.Diagnostics;
using Npgsql;
using PI.SearchApi.Contracts;

namespace PI.SearchApi.Pipeline.Structured;

// Stage 1 — Structured search
//
// What:     Parameterised SQL built only from the request's filters: brand, categories
//           (array overlap, with narrower concepts), price range and JSONB spec containment.
// Strength: Exact and fast. When the user already knows the attributes ("Brakk, 18V, under
//           £100"), a WHERE clause gives perfect precision and needs no ranking at all.
// Failure:  Users don't speak in attributes. It can't understand "something to charge my
//           laptop", and a request with no filters simply returns the whole catalog.
// Decision: docs/decisions/0007-structured-search.md
public sealed class StructuredSearch(NpgsqlDataSource dataSource, SqlFilterBuilder filterBuilder) : IStructuredSearch
{
    // {where} is replaced with fragments from SqlFilterBuilder's fixed list; every value is a parameter.
    // ORDER BY price, id is deterministic, and deliberately not a relevance order: there is no score.
    // This stage isn't bounded by candidateDepth, so it pages here, in SQL, with LIMIT/OFFSET.
    private const string SearchSqlTemplate = $"""
        SELECT {ProductRows.Columns}
        FROM products
        {"{where}"}
        ORDER BY price, id
        LIMIT @limit OFFSET @offset;
        """;

    // A real COUNT(*) over the same filters, so totalResults is the true number of matches.
    private const string CountSqlTemplate = """
        SELECT COUNT(*)
        FROM products
        {where};
        """;

    public async Task<StageResult> SearchAsync(SearchRequest request, CancellationToken ct)
    {
        using var activity = PipelineTelemetry.Source.StartActivity("Stage 1: structured search");
        var start = Stopwatch.GetTimestamp();

        var filter = filterBuilder.Build(request.Filters);
        var whereClause = filter.WhereClause();

        var searchSql = SearchSqlTemplate.Replace("{where}", whereClause);
        var countSql = CountSqlTemplate.Replace("{where}", whereClause);

        var searchParameters = new SqlParameters()
            .AddRange(filter.Parameters)
            .Add("limit", request.PageSize)
            .Add("offset", (request.Page - 1) * request.PageSize);

        await using var connection = await dataSource.OpenConnectionAsync(ct);

        var candidates = new List<Candidate>();
        await using (var command = new NpgsqlCommand(searchSql, connection))
        {
            searchParameters.ApplyTo(command);
            await using var reader = await command.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                candidates.Add(new Candidate(
                    ProductRows.Read(reader),
                    Score: null,
                    new CandidateSignals { StructuredMatch = true },
                    CompatibilityResult.NotEvaluated));
            }
        }

        int total;
        await using (var command = new NpgsqlCommand(countSql, connection))
        {
            filter.Parameters.ApplyTo(command);
            total = Convert.ToInt32(await command.ExecuteScalarAsync(ct));
        }

        var notes = new List<string>
        {
            "Structured search can't understand 'something to charge my laptop'. It only matches the attributes you give it.",
            "There is no relevance score: results are ordered by price, then ID.",
        };

        if (!string.IsNullOrWhiteSpace(request.Query))
        {
            notes.Add($"The query text \"{request.Query}\" was ignored: Stage 1 reads only filters.");
        }

        if (filter.Conditions.Count == 0)
        {
            notes.Add("No filters were given, so every product matches: structured search has no notion of relevance.");
        }

        var details = new Dictionary<string, object?>(filter.Details)
        {
            ["rowCount"] = candidates.Count,
            ["totalCount"] = total,
            ["countSql"] = countSql,
        };

        var step = new TraceStep
        {
            Stage = "structured",
            Title = "Parameterised SQL filters",
            DurationMs = PipelineTelemetry.ElapsedMs(start),
            Sql = searchSql,
            Parameters = searchParameters.ForTrace(),
            Details = details,
            Notes = notes,
        };

        return new StageResult(candidates, [step], total);
    }
}

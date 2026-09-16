using System.Diagnostics;
using System.Text.RegularExpressions;
using Npgsql;
using PI.SearchApi.Contracts;

namespace PI.SearchApi.Pipeline.Keyword;

// Stage 2 — Keyword search (BM25-style)
//
// What:     Postgres full-text search: websearch_to_tsquery + ts_rank_cd over a weighted
//           tsvector (A name, B brand + categories, C description, D reviews).
// Strength: Fast and exact; great for names, model numbers and specific terms.
// Failure:  Matches words, not meaning: misses synonyms ("power brick" vs "adapter")
//           and is fooled by shared words ("cordless" phone vs drill battery).
// Decision: docs/decisions/0008-keyword-search-bm25-style.md
public sealed partial class KeywordSearch(NpgsqlDataSource dataSource, SqlFilterBuilder filterBuilder) : IKeywordSearch
{
    // {tsquery} is websearch_to_tsquery('english', @query) AS q — a function call, which FROM accepts
    // directly — or, for Stage 5's expanded expression, (SELECT … AS q) AS expanded: FROM can't take a
    // bare expression like (a || b) && c, but it can take a one-row subquery that computes it.
    //   search_vector @@ q  — the match: true when the document satisfies the tsquery. Every term must
    //                         match (AND), which is exactly why a synonym the catalog never uses misses.
    //   ts_rank_cd          — "cover density": rewards query terms that appear close together, weighted by
    //                         field (A > B > C). No IDF and no term-frequency saturation, so it's
    //                         "BM25-style", not BM25.
    //   ts_headline         — marks the words that matched, so the trace can show why a product matched.
    //   LIMIT @candidateDepth — retrieve deep; the endpoint pages later.
    private const string SearchSqlTemplate = $"""
        SELECT {ProductRows.Columns},
               ts_rank_cd(search_vector, q) AS score,
               ts_headline('english', name || '. ' || brand || ' ' || array_to_string(categories, ' ') || '. ' || description || ' ' || array_to_string(reviews, ' '), q,
                           'MaxFragments=3, MinWords=3, MaxWords=10, StartSel=«, StopSel=»') AS headline
        FROM products, {"{tsquerySource}"}
        {"{where}"}
        ORDER BY score DESC, id
        LIMIT @candidateDepth;
        """;

    // The parsed query on its own, so learners see stemming and stop-word removal:
    // "cordless drill batteries" becomes 'cordless' & 'drill' & 'batteri'.
    private const string ParseSqlTemplate = """
        SELECT ({tsquery})::text;
        """;

    public async Task<StageResult> SearchAsync(SearchRequest request, KeywordExpansion? expansion, CancellationToken ct)
    {
        using var activity = PipelineTelemetry.Source.StartActivity("Stage 2: keyword search");
        var start = Stopwatch.GetTimestamp();

        var (tsQuery, queryParameters) = expansion is null
            ? TsQueryBuilder.ForQuery(request.Query)
            : TsQueryBuilder.ForExpansion(expansion, request.Query);

        var filter = filterBuilder.Build(request.Filters);

        var tsQuerySource = expansion is null
            ? $"{tsQuery} AS q"
            : $"(SELECT {tsQuery} AS q) AS expanded";

        var searchSql = SearchSqlTemplate
            .Replace("{tsquerySource}", tsQuerySource)
            .Replace("{where}", filter.WhereClause("search_vector @@ q"));
        var parseSql = ParseSqlTemplate.Replace("{tsquery}", tsQuery);

        var parameters = new SqlParameters()
            .AddRange(queryParameters)
            .AddRange(filter.Parameters)
            .Add("candidateDepth", request.Options.CandidateDepth);

        await using var connection = await dataSource.OpenConnectionAsync(ct);

        string parsedTsQuery;
        await using (var command = new NpgsqlCommand(parseSql, connection))
        {
            queryParameters.ApplyTo(command);
            parsedTsQuery = (string?)await command.ExecuteScalarAsync(ct) ?? "";
        }

        var candidates = new List<Candidate>();
        var matches = new List<object>();

        await using (var command = new NpgsqlCommand(searchSql, connection))
        {
            parameters.ApplyTo(command);
            await using var reader = await command.ExecuteReaderAsync(ct);

            while (await reader.ReadAsync(ct))
            {
                var product = ProductRows.Read(reader);
                var score = Math.Round(reader.GetFloat(reader.GetOrdinal("score")), 5);
                var rank = candidates.Count + 1;
                var matchedTerms = HighlightedWords(reader.GetString(reader.GetOrdinal("headline")));

                candidates.Add(new Candidate(
                    product,
                    score,
                    new CandidateSignals { KeywordRank = rank, KeywordScore = score },
                    CompatibilityResult.NotEvaluated));

                matches.Add(new { id = product.Id, rank, score, matchedTerms });
            }
        }

        var notes = new List<string>
        {
            "ts_rank_cd is not true BM25 — no IDF (rare words don't count for more) and no term-frequency saturation. Hence \"BM25-style\".",
            "Every term in the tsquery must match (AND), so a single word the catalog never uses — a synonym — returns nothing.",
            "The English stemmer reduces words to lexemes ('batteries' → 'batteri') and drops stop words ('for', 'my').",
            $"Keyword search retrieves at most candidateDepth = {request.Options.CandidateDepth} candidates; totalResults counts what was retrieved, not the whole catalog.",
        };

        if (candidates.Count == 0)
        {
            notes.Add("No product contains every term. Keyword search can't fall back to 'close enough' — that's what vector search is for.");
        }

        var details = new Dictionary<string, object?>(filter.Details)
        {
            ["tsquery"] = parsedTsQuery,
            ["tsqueryExpression"] = tsQuery,
            ["expanded"] = expansion is not null,
            ["ranking"] = "ts_rank_cd (cover density), field weights A = name, B = brand + categories, C = description, D = reviews",
            ["matches"] = matches,
            ["optionsUsed"] = new { request.Options.CandidateDepth },
        };

        var step = new TraceStep
        {
            Stage = "keyword",
            Title = expansion is null ? "Postgres full-text search (BM25-style)" : "Postgres full-text search with ontology expansion (BM25-style)",
            DurationMs = PipelineTelemetry.ElapsedMs(start),
            Sql = searchSql,
            Parameters = parameters.ForTrace(),
            Details = details,
            Notes = notes,
        };

        return new StageResult(candidates, [step]);
    }

    // ts_headline wraps each matching word as «word»; collect the distinct words, lower-cased.
    private static IReadOnlyList<string> HighlightedWords(string headline) =>
        [.. HighlightPattern().Matches(headline).Select(m => m.Groups[1].Value.ToLowerInvariant()).Distinct()];

    [GeneratedRegex("«([^»]+)»")]
    private static partial Regex HighlightPattern();
}

using FastEndpoints;
using PI.SearchApi.Contracts;
using PI.SearchApi.Pipeline;
using PI.SearchApi.Pipeline.Keyword;
using PI.SearchApi.Pipeline.Ontology;

namespace PI.SearchApi.Endpoints.Search.Keyword;

/// <summary><c>POST /api/search/keyword</c> — Stage 2 (ADR-0008).</summary>
public sealed class KeywordSearchEndpoint(IKeywordSearch search) : Endpoint<SearchRequest, SearchResponse>
{
    public override void Configure()
    {
        Post("/api/search/keyword");
        AllowAnonymous();
        Validator<KeywordSearchRequestValidator>();
        Summary(s => s.Summary = "Stage 2 — Keyword search: Postgres full-text search, BM25-style ranking");
        // Document the validation error, so the UI's generated types describe it (ADR-0003).
        Description(b => b.Produces<ValidationProblem>(StatusCodes.Status400BadRequest, "application/problem+json"));
    }

    public override async Task HandleAsync(SearchRequest req, CancellationToken ct)
    {
        var start = System.Diagnostics.Stopwatch.GetTimestamp();

        var result = await search.SearchAsync(req, expansion: null, ct);

        await Send.OkAsync(SearchResponseMapper.ToResponse("keyword", req, result, PipelineTelemetry.ElapsedMs(start)), ct);
    }
}

public sealed class KeywordSearchRequestValidator : Validator<SearchRequest>
{
    public KeywordSearchRequestValidator()
    {
        SearchRequestRules.AddTo(this, Resolve<IOntology>(), requireQuery: true);
    }
}

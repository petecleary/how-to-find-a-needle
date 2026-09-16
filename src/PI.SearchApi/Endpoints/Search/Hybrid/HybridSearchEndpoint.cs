using FastEndpoints;
using PI.SearchApi.Contracts;
using PI.SearchApi.Pipeline;
using PI.SearchApi.Pipeline.Hybrid;
using PI.SearchApi.Pipeline.Ontology;

namespace PI.SearchApi.Endpoints.Search.Hybrid;

/// <summary><c>POST /api/search/hybrid</c> — Stage 4 (ADR-0011).</summary>
public sealed class HybridSearchEndpoint(IHybridSearch search) : Endpoint<SearchRequest, SearchResponse>
{
    public override void Configure()
    {
        Post("/api/search/hybrid");
        AllowAnonymous();
        Validator<HybridSearchRequestValidator>();
        Summary(s => s.Summary = "Stage 4 — Hybrid search: keyword + vector fused with Reciprocal Rank Fusion");
        // Document the validation error and the missing-model 503, so the UI's generated types describe them (ADR-0003).
        Description(b => b
            .Produces<ValidationProblem>(StatusCodes.Status400BadRequest, "application/problem+json")
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable));
    }

    public override async Task HandleAsync(SearchRequest req, CancellationToken ct)
    {
        var start = System.Diagnostics.Stopwatch.GetTimestamp();

        var result = await search.SearchAsync(req, keywordExpansion: null, embeddingText: null, ct);

        await Send.OkAsync(SearchResponseMapper.ToResponse("hybrid", req, result, PipelineTelemetry.ElapsedMs(start)), ct);
    }
}

public sealed class HybridSearchRequestValidator : Validator<SearchRequest>
{
    public HybridSearchRequestValidator()
    {
        SearchRequestRules.AddTo(this, Resolve<IOntology>(), requireQuery: true);
    }
}

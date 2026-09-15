using FastEndpoints;
using PI.SearchApi.Contracts;
using PI.SearchApi.Pipeline;
using PI.SearchApi.Pipeline.Ontology;
using PI.SearchApi.Pipeline.Rag;

namespace PI.SearchApi.Endpoints.Search.Rag;

/// <summary><c>POST /api/search/rag</c> — Stage 6's results: Stage 5's pipeline plus the evidence step (ADR-0016). No LLM call.</summary>
public sealed class RagSearchEndpoint(IRagSearch search) : Endpoint<SearchRequest, SearchResponse>
{
    public override void Configure()
    {
        Post("/api/search/rag");
        AllowAnonymous();
        Validator<RagSearchRequestValidator>();
        Summary(s => s.Summary = "Stage 6 — RAG: Stage 5's results plus the evidence set the answer will use (the answer streams from /answer)");
        Description(b => b
            .Produces<ValidationProblem>(StatusCodes.Status400BadRequest, "application/problem+json")
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable));
    }

    public override async Task HandleAsync(SearchRequest req, CancellationToken ct)
    {
        var start = System.Diagnostics.Stopwatch.GetTimestamp();

        var result = await search.SearchAsync(req, "rag", ct);

        await Send.OkAsync(SearchResponseMapper.ToResponse("rag", req, result.Result, PipelineTelemetry.ElapsedMs(start)), ct);
    }
}

public sealed class RagSearchRequestValidator : Validator<SearchRequest>
{
    public RagSearchRequestValidator()
    {
        SearchRequestRules.AddTo(this, Resolve<IOntology>(), requireQuery: true);
    }
}

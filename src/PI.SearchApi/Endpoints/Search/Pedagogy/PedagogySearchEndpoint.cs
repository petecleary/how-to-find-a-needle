using FastEndpoints;
using PI.SearchApi.Contracts;
using PI.SearchApi.Pipeline;
using PI.SearchApi.Pipeline.Ontology;
using PI.SearchApi.Pipeline.Rag;

namespace PI.SearchApi.Endpoints.Search.Pedagogy;

/// <summary>
/// <c>POST /api/search/pedagogy</c> — Stage 7's results (ADR-0017): the same retrieval and evidence as Stage 6, because
/// pedagogy changes how the decision is explained, not what was found. No LLM call.
/// </summary>
public sealed class PedagogySearchEndpoint(IRagSearch search) : Endpoint<SearchRequest, SearchResponse>
{
    public override void Configure()
    {
        Post("/api/search/pedagogy");
        AllowAnonymous();
        Validator<PedagogySearchRequestValidator>();
        Summary(s => s.Summary = "Stage 7 — Pedagogy: Stage 5's results plus the evidence set (the answer and explanation stream from /answer)");
        Description(b => b
            .Produces<ValidationProblem>(StatusCodes.Status400BadRequest, "application/problem+json")
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable));
    }

    public override async Task HandleAsync(SearchRequest req, CancellationToken ct)
    {
        var start = System.Diagnostics.Stopwatch.GetTimestamp();

        var result = await search.SearchAsync(req, "pedagogy", ct);

        await Send.OkAsync(SearchResponseMapper.ToResponse("pedagogy", req, result.Result, PipelineTelemetry.ElapsedMs(start)), ct);
    }
}

public sealed class PedagogySearchRequestValidator : Validator<SearchRequest>
{
    public PedagogySearchRequestValidator()
    {
        SearchRequestRules.AddTo(this, Resolve<IOntology>(), requireQuery: true);
    }
}

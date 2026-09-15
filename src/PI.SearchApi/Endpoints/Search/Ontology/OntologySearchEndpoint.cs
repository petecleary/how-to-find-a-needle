using FastEndpoints;
using PI.SearchApi.Contracts;
using PI.SearchApi.Pipeline;
using PI.SearchApi.Pipeline.Ontology;

namespace PI.SearchApi.Endpoints.Search.Ontology;

/// <summary><c>POST /api/search/ontology</c> — Stage 5 (ADR-0013).</summary>
public sealed class OntologySearchEndpoint(IOntologySearch search) : Endpoint<SearchRequest, SearchResponse>
{
    public override void Configure()
    {
        Post("/api/search/ontology");
        AllowAnonymous();
        Validator<OntologySearchRequestValidator>();
        Summary(s => s.Summary = "Stage 5 — Ontology: SKOS concepts, synonym expansion, classification and domain rules");
        // Document the validation error and the missing-model 503, so the UI's generated types describe them (ADR-0003).
        Description(b => b
            .Produces<ValidationProblem>(StatusCodes.Status400BadRequest, "application/problem+json")
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable));
    }

    public override async Task HandleAsync(SearchRequest req, CancellationToken ct)
    {
        var start = System.Diagnostics.Stopwatch.GetTimestamp();

        var result = await search.SearchAsync(req, ct);

        await Send.OkAsync(SearchResponseMapper.ToResponse("ontology", req, result, PipelineTelemetry.ElapsedMs(start)), ct);
    }
}

public sealed class OntologySearchRequestValidator : Validator<SearchRequest>
{
    public OntologySearchRequestValidator()
    {
        SearchRequestRules.AddTo(this, Resolve<IOntology>(), requireQuery: true);
    }
}

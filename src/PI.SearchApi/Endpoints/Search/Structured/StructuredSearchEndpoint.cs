using FastEndpoints;
using PI.SearchApi.Contracts;
using PI.SearchApi.Pipeline;
using PI.SearchApi.Pipeline.Ontology;
using PI.SearchApi.Pipeline.Structured;

namespace PI.SearchApi.Endpoints.Search.Structured;

/// <summary><c>POST /api/search/structured</c> — Stage 1 (ADR-0007).</summary>
public sealed class StructuredSearchEndpoint(IStructuredSearch search) : Endpoint<SearchRequest, SearchResponse>
{
    public override void Configure()
    {
        Post("/api/search/structured");
        AllowAnonymous();
        Validator<StructuredSearchRequestValidator>();
        Summary(s => s.Summary = "Stage 1 — Structured search: exact SQL filters, no ranking");
    }

    public override async Task HandleAsync(SearchRequest req, CancellationToken ct)
    {
        var start = System.Diagnostics.Stopwatch.GetTimestamp();

        var result = await search.SearchAsync(req, ct);

        // Stage 1 already paged in SQL (ADR-0004), so the mapper mustn't page again.
        var response = SearchResponseMapper.ToResponse("structured", req, result, PipelineTelemetry.ElapsedMs(start), alreadyPaged: true);
        await Send.OkAsync(response, ct);
    }
}

/// <summary>Stage 1 ignores the query, so unlike the other stages it doesn't require one.</summary>
public sealed class StructuredSearchRequestValidator : Validator<SearchRequest>
{
    public StructuredSearchRequestValidator()
    {
        SearchRequestRules.AddTo(this, Resolve<IOntology>(), requireQuery: false);
    }
}

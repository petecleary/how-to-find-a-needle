using FastEndpoints;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;
using PI.SearchApi.Contracts;
using PI.SearchApi.Pipeline.Ontology;
using PI.SearchApi.Pipeline.Pedagogy;

namespace PI.SearchApi.Endpoints.Search.Pedagogy;

/// <summary>
/// <c>POST /api/search/pedagogy/answer</c> — Stage 7's answer, then its explanation (ADR-0017): Server-Sent Events by
/// default, or one JSON object for <c>Accept: application/json</c>. <c>options.audience</c> and
/// <c>options.applyPedagogy</c> choose how the explanation is written.
/// </summary>
public sealed class PedagogyAnswerEndpoint(
    IPedagogyEngine engine,
    IOptions<JsonOptions> jsonOptions,
    ILogger<PedagogyAnswerEndpoint> logger) : Endpoint<SearchRequest>
{
    public override void Configure()
    {
        Post("/api/search/pedagogy/answer");
        AllowAnonymous();
        Validator<PedagogyAnswerRequestValidator>();
        Summary(s => s.Summary = "Stage 7 — Pedagogy answer: the grounded answer, then an audience-aware explanation (or the baseline), streamed as Server-Sent Events (or JSON with Accept: application/json)");
        Description(b => b
            .Produces<AnswerResponse>(StatusCodes.Status200OK, "application/json")
            .Produces<ValidationProblem>(StatusCodes.Status400BadRequest, "application/problem+json")
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable));
    }

    public override Task HandleAsync(SearchRequest req, CancellationToken ct) =>
        AnswerStreamWriter.SendAsync(HttpContext, "pedagogy", engine.StreamAsync(req, ct), jsonOptions.Value.SerializerOptions, logger, ct);
}

public sealed class PedagogyAnswerRequestValidator : Validator<SearchRequest>
{
    public PedagogyAnswerRequestValidator()
    {
        SearchRequestRules.AddTo(this, Resolve<IOntology>(), requireQuery: true);
    }
}

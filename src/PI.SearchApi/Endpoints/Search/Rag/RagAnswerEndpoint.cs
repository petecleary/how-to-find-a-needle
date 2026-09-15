using FastEndpoints;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.Extensions.Options;
using PI.SearchApi.Contracts;
using PI.SearchApi.Pipeline.Ontology;
using PI.SearchApi.Pipeline.Rag;

namespace PI.SearchApi.Endpoints.Search.Rag;

/// <summary>
/// <c>POST /api/search/rag/answer</c> — Stage 6's grounded summary (ADR-0016): Server-Sent Events by default, or one
/// JSON object for <c>Accept: application/json</c>. Same request body as <c>/api/search/rag</c>, sent at the same time.
/// </summary>
public sealed class RagAnswerEndpoint(
    IAnswerGenerator generator,
    IOptions<JsonOptions> jsonOptions,
    ILogger<RagAnswerEndpoint> logger) : Endpoint<SearchRequest>
{
    public override void Configure()
    {
        Post("/api/search/rag/answer");
        AllowAnonymous();
        Validator<RagAnswerRequestValidator>();
        Summary(s => s.Summary = "Stage 6 — RAG answer: a grounded, cited markdown summary, streamed as Server-Sent Events (or JSON with Accept: application/json)");
        // OpenAPI can't describe an event stream's events, so the document describes the JSON form (ADR-0003).
        Description(b => b
            .Produces<AnswerResponse>(StatusCodes.Status200OK, "application/json")
            .Produces<ValidationProblem>(StatusCodes.Status400BadRequest, "application/problem+json")
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable));
    }

    public override Task HandleAsync(SearchRequest req, CancellationToken ct) =>
        // Closing the connection cancels ct, which flows through the generator into IChatClient and stops the model.
        AnswerStreamWriter.SendAsync(HttpContext, "rag", generator.StreamAsync(req, ct), jsonOptions.Value.SerializerOptions, logger, ct);
}

public sealed class RagAnswerRequestValidator : Validator<SearchRequest>
{
    public RagAnswerRequestValidator()
    {
        SearchRequestRules.AddTo(this, Resolve<IOntology>(), requireQuery: true);
    }
}

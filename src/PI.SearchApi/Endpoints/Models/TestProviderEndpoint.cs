using FastEndpoints;
using PI.SearchApi.Contracts;
using PI.SearchApi.Llm;

namespace PI.SearchApi.Endpoints.Models;

/// <summary>
/// <c>POST /api/providers/{id}/test</c>: lists the provider's models now and reports what happened (ADR-0019). A
/// failure is a normal result (<c>ok: false</c> with the reason), not an HTTP error: the test ran, the provider didn't answer.
/// </summary>
public sealed class TestProviderEndpoint(LlmModelRegistry models) : EndpointWithoutRequest<ConnectionTest>
{
    public override void Configure()
    {
        Post("/api/providers/{id}/test");
        AllowAnonymous();
        Summary(s => s.Summary = "Test a provider's connection by listing its models");
        Description(b => b.Produces(StatusCodes.Status404NotFound));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (await models.TestAsync(Route<string>("id") ?? "", ct) is not { } result)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        await Send.OkAsync(result, ct);
    }
}

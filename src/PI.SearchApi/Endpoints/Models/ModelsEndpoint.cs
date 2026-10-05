using FastEndpoints;
using PI.SearchApi.Contracts;
using PI.SearchApi.Llm;

namespace PI.SearchApi.Endpoints.Models;

/// <summary>
/// <c>GET /api/models</c>: the default model and every enabled provider's models, listed live (ADR-0019). A provider
/// that couldn't be listed is included with the reason, so the picker shows it rather than hiding it.
/// </summary>
public sealed class ModelsEndpoint(LlmModelRegistry models) : EndpointWithoutRequest<ModelCatalogue>
{
    public override void Configure()
    {
        Get("/api/models");
        AllowAnonymous();
        Summary(s => s.Summary = "The default model and the models each enabled provider offers, listed live");
    }

    public override async Task HandleAsync(CancellationToken ct) => await Send.OkAsync(await models.ModelsAsync(ct), ct);
}

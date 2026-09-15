using FastEndpoints;
using PI.SearchApi.Pipeline;

namespace PI.SearchApi.Endpoints.Demo;

/// <summary>
/// <c>GET /api/brands</c>: every brand in the catalogue, once each, in alphabetical order (ADR-0003). The UI
/// builds its brand filter from it, so a brand added to <c>products.json</c> appears after re-seeding.
/// </summary>
public sealed class BrandsEndpoint(ProductLookup products) : EndpointWithoutRequest<IReadOnlyList<string>>
{
    public override void Configure()
    {
        Get("/api/brands");
        AllowAnonymous();
        Summary(s => s.Summary = "Every brand in the catalogue");
    }

    public override async Task HandleAsync(CancellationToken ct) =>
        await Send.OkAsync(await products.GetBrandsAsync(ct), ct);
}

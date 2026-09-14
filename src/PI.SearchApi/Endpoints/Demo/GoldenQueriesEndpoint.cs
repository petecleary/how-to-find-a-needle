using FastEndpoints;
using PI.SearchApi.Data;

namespace PI.SearchApi.Endpoints.Demo;

/// <summary>
/// <c>GET /api/demo/queries</c>: the golden queries as UI presets (ADR-0003, ADR-0005). The same file
/// is the integration test suite, so a preset in the UI is always a moment the tests prove.
/// </summary>
public sealed class GoldenQueriesEndpoint : EndpointWithoutRequest<IReadOnlyList<GoldenQuery>>
{
    public override void Configure()
    {
        Get("/api/demo/queries");
        AllowAnonymous();
        Summary(s => s.Summary = "Golden queries: talk moments with per-stage expectations");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var dataDirectory = Path.Combine(AppContext.BaseDirectory, "assets", "data");

        await Send.OkAsync(CatalogLoader.LoadGoldenQueries(dataDirectory), ct);
    }
}

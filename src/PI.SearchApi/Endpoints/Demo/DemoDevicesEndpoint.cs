using FastEndpoints;
using PI.SearchApi.Contracts;
using PI.SearchApi.Pipeline;

namespace PI.SearchApi.Endpoints.Demo;

/// <summary>
/// <c>GET /api/demo/devices</c>: products that can be a target device (ADR-0003). Which products those
/// are comes from the ontology's device-type concepts, not from a flag on the product.
/// </summary>
public sealed class DemoDevicesEndpoint(ProductLookup products) : EndpointWithoutRequest<IReadOnlyList<DemoDevice>>
{
    public override void Configure()
    {
        Get("/api/demo/devices");
        AllowAnonymous();
        Summary(s => s.Summary = "Products that can be a target device");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var devices = await products.GetDevicesAsync(ct);

        await Send.OkAsync([.. devices.Select(d => new DemoDevice(d.Id, d.Name, d.Brand, d.Categories))], ct);
    }
}

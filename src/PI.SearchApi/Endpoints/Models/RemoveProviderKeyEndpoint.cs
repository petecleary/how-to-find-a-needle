using FastEndpoints;
using PI.SearchApi.Llm;

namespace PI.SearchApi.Endpoints.Models;

/// <summary>
/// <c>DELETE /api/providers/{id}/key</c>: forgets a key pasted for this session (ADR-0019). A key from configuration
/// (an environment variable or user secret) stays in use.
/// </summary>
public sealed class RemoveProviderKeyEndpoint(ISecretStore secrets) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("/api/providers/{id}/key");
        AllowAnonymous();
        Summary(s => s.Summary = "Forget a provider's session key");
        Description(b => b
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound));
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var id = Route<string>("id") ?? "";
        if (LlmProviders.Find(id) is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        secrets.RemoveSessionKey(id);
        await Send.NoContentAsync(ct);
    }
}

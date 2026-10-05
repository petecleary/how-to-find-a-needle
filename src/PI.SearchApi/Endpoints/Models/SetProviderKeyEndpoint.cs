using FastEndpoints;
using FluentValidation;
using PI.SearchApi.Contracts;
using PI.SearchApi.Llm;

namespace PI.SearchApi.Endpoints.Models;

/// <summary>An API key pasted into the UI.</summary>
public sealed record SetProviderKeyRequest
{
    /// <summary>The provider ID, from the route.</summary>
    public string Id { get; init; } = "";

    public string Key { get; init; } = "";
}

/// <summary>
/// <c>PUT /api/providers/{id}/key</c>: holds a key in the API's memory until it stops (ADR-0019). The key is never
/// written to disk, logged, traced or sent back; to keep it across restarts, use <c>dotnet user-secrets</c>.
/// </summary>
public sealed class SetProviderKeyEndpoint(ISecretStore secrets) : Endpoint<SetProviderKeyRequest>
{
    public override void Configure()
    {
        Put("/api/providers/{id}/key");
        AllowAnonymous();
        Summary(s => s.Summary = "Hold a provider's API key for this session only (never saved or returned)");
        Description(b => b
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ValidationProblem>(StatusCodes.Status400BadRequest, "application/problem+json")
            .Produces(StatusCodes.Status404NotFound));
    }

    public override async Task HandleAsync(SetProviderKeyRequest req, CancellationToken ct)
    {
        if (LlmProviders.Find(req.Id) is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        secrets.SetForSession(req.Id, req.Key);
        await Send.NoContentAsync(ct);
    }
}

public sealed class SetProviderKeyRequestValidator : Validator<SetProviderKeyRequest>
{
    public SetProviderKeyRequestValidator()
    {
        // The message never echoes the value: a validation error is shown in the UI and may be logged.
        RuleFor(r => r.Key).NotEmpty().WithMessage("Paste the key.").MaximumLength(1000);
    }
}

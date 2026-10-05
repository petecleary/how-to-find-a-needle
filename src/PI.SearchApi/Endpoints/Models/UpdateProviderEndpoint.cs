using FastEndpoints;
using FluentValidation;
using PI.SearchApi.Contracts;
using PI.SearchApi.Llm;

namespace PI.SearchApi.Endpoints.Models;

/// <summary>A change to one provider's saved settings. A null field keeps its current value; an empty base URL clears it.</summary>
public sealed record UpdateProviderRequest
{
    /// <summary>The provider ID, from the route.</summary>
    public string Id { get; init; } = "";

    public bool? Enabled { get; init; }

    public string? BaseUrl { get; init; }

    public IReadOnlyList<string>? ExtraModels { get; init; }
}

/// <summary>
/// <c>PUT /api/providers/{id}</c>: changes a provider's non-secret settings and saves them to
/// <c>~/.needle/settings.json</c> (ADR-0019). Keys have their own endpoint and are never saved.
/// </summary>
public sealed class UpdateProviderEndpoint(LlmSettingsStore settings) : Endpoint<UpdateProviderRequest>
{
    public override void Configure()
    {
        Put("/api/providers/{id}");
        AllowAnonymous();
        Summary(s => s.Summary = "Change a provider's base URL, extra models or whether the picker lists it");
        Description(b => b
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ValidationProblem>(StatusCodes.Status400BadRequest, "application/problem+json")
            .Produces(StatusCodes.Status404NotFound));
    }

    public override async Task HandleAsync(UpdateProviderRequest req, CancellationToken ct)
    {
        if (LlmProviders.Find(req.Id) is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }

        settings.Update(current =>
        {
            var saved = current.For(req.Id);
            var updated = saved with
            {
                Enabled = req.Enabled ?? saved.Enabled,
                BaseUrl = req.BaseUrl is null ? saved.BaseUrl : NullIfEmpty(req.BaseUrl.Trim().TrimEnd('/')),
                ExtraModels = req.ExtraModels is null
                    ? saved.ExtraModels
                    : [.. req.ExtraModels.Select(m => m.Trim()).Where(m => m.Length > 0).Distinct()],
            };

            return current with { Providers = new Dictionary<string, LlmProviderSettings>(current.Providers) { [req.Id] = updated } };
        });

        await Send.NoContentAsync(ct);
    }

    private static string? NullIfEmpty(string value) => value.Length == 0 ? null : value;
}

public sealed class UpdateProviderRequestValidator : Validator<UpdateProviderRequest>
{
    public UpdateProviderRequestValidator()
    {
        // An address the client can't use would only fail later, on the first answer, with a less helpful message.
        RuleFor(r => r.BaseUrl)
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            .WithMessage("'Base URL' must be an absolute http or https URL, e.g. http://localhost:11434.")
            .When(r => !string.IsNullOrWhiteSpace(r.BaseUrl));
    }
}

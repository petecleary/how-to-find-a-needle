using FastEndpoints;
using PI.SearchApi.Contracts;
using PI.SearchApi.Llm;

namespace PI.SearchApi.Endpoints.Models;

/// <summary>
/// <c>GET /api/providers</c>: every LLM provider with its settings and where its key comes from (ADR-0019). Never the
/// key itself. No network calls; the test endpoint checks a connection.
/// </summary>
public sealed class ProvidersEndpoint(LlmModelRegistry models) : EndpointWithoutRequest<IReadOnlyList<LlmProviderStatus>>
{
    public override void Configure()
    {
        Get("/api/providers");
        AllowAnonymous();
        Summary(s => s.Summary = "LLM providers, their settings and key sources (never the keys)");
    }

    public override Task HandleAsync(CancellationToken ct) => Send.OkAsync(models.Providers(), ct);
}

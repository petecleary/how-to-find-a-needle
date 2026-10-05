namespace PI.SearchApi.Contracts;

/// <summary><c>GET /api/models</c> (ADR-0019): the default model and every enabled provider's models, listed live.</summary>
public sealed record ModelCatalogue
{
    /// <summary>The model a request without <c>options.model</c> uses: <c>Llm:Provider</c>/<c>Llm:Model</c> (ADR-0015).</summary>
    public required string Default { get; init; }

    public required IReadOnlyList<ProviderModels> Providers { get; init; }
}

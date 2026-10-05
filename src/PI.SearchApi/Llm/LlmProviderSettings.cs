namespace PI.SearchApi.Llm;

/// <summary>One provider's saved settings (ADR-0019). Null means "use the catalogue's default".</summary>
public sealed record LlmProviderSettings
{
    /// <summary>Whether the model picker lists this provider. Null uses <see cref="LlmProviderDefinition.EnabledByDefault"/>.</summary>
    public bool? Enabled { get; init; }

    /// <summary>Overrides the provider's default address, e.g. Ollama on another machine or an Azure resource.</summary>
    public string? BaseUrl { get; init; }

    /// <summary>
    /// Model names to list even though the provider can't list them: Azure deployment names, or a model an
    /// OpenAI-compatible server doesn't advertise.
    /// </summary>
    public IReadOnlyList<string> ExtraModels { get; init; } = [];
}

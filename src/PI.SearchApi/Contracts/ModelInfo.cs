namespace PI.SearchApi.Contracts;

/// <summary>One model the picker can offer (ADR-0019). Capabilities are null when the provider doesn't say.</summary>
public sealed record ModelInfo
{
    /// <summary>What a request sends as <c>options.model</c>: <c>provider/model</c>.</summary>
    public required string Ref { get; init; }

    /// <summary>The provider's own model name, e.g. <c>qwen3.6:35b</c>.</summary>
    public required string Model { get; init; }

    public required bool IsDefault { get; init; }

    /// <summary>Whether the model can call tools. Stages 6–7 don't need tools; it is shown as a hint, as Ollama reports it.</summary>
    public bool? Tools { get; init; }

    public bool? Vision { get; init; }

    /// <summary>The context window in tokens, when the provider reports it (Ollama does).</summary>
    public int? ContextLength { get; init; }
}

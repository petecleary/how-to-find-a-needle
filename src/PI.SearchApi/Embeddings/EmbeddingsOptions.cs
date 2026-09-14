namespace PI.SearchApi.Embeddings;

/// <summary>The <c>Embeddings</c> configuration section (ADR-0009).</summary>
public sealed class EmbeddingsOptions
{
    public const string SectionName = "Embeddings";

    /// <summary>
    /// <c>nomic</c> (local ONNX, the default and the only provider built so far) or <c>openai</c>
    /// (planned for Phase 5).
    /// </summary>
    public string Provider { get; set; } = "nomic";

    /// <summary>
    /// When true, the seeder ignores the committed embeddings file, re-embeds every product live and
    /// overwrites the file. Use it after editing products.json, then set it back to false.
    /// </summary>
    public bool Rebuild { get; set; }

    public OpenAIEmbeddingsOptions OpenAI { get; set; } = new();
}

/// <summary>Settings for the planned OpenAI provider; unused until Phase 5.</summary>
public sealed class OpenAIEmbeddingsOptions
{
    public string Model { get; set; } = "text-embedding-3-small";

    /// <summary>768, so OpenAI vectors fit the same column as Nomic's.</summary>
    public int Dimensions { get; set; } = 768;
}

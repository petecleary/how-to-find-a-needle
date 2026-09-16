namespace PI.SearchApi.Llm;

/// <summary>
/// What one streamed LLM call produced, filled in as it runs (ADR-0015): the full text, time to first token, total
/// time, why it stopped and the token counts the provider reported. The stages copy it into their trace.
/// </summary>
public sealed class LlmGeneration
{
    public string Text { get; internal set; } = "";

    /// <summary>Milliseconds from sending the request to the first text chunk. Null if no text arrived.</summary>
    public double? TimeToFirstTokenMs { get; internal set; }

    public double TotalMs { get; internal set; }

    /// <summary><c>stop</c>, <c>length</c> (hit the output-token cap) or <c>content_filter</c> (Anthropic: a refusal).</summary>
    public string? FinishReason { get; internal set; }

    public long? InputTokens { get; internal set; }

    public long? OutputTokens { get; internal set; }

    /// <summary>Plain-English notes on how the call ended, for the section's warnings (a cut-off or a refusal).</summary>
    public IReadOnlyList<string> FinishWarnings => FinishReason switch
    {
        "length" => ["The model hit the output-token cap (Llm:MaxOutputTokens), so the text is cut off."],
        "content_filter" => ["The provider stopped the response (a refusal): the text may be incomplete."],
        _ => [],
    };
}

namespace PI.SearchApi.Pipeline.Pedagogy;

/// <summary>The explanation's messages, as sent, and what the trace needs to show about how they were chosen.</summary>
/// <param name="AudienceGuidance">The active section of <c>pedagogy-audiences.md</c>; null for the baseline, which names the audience only.</param>
/// <param name="WordsOffered">Each concept's words for this audience, identical whichever system prompt is used.</param>
public sealed record PedagogyPrompt(
    bool ApplyPedagogy,
    string Audience,
    string SystemPromptFile,
    string SystemPrompt,
    string UserPrompt,
    string? AudienceGuidance,
    IReadOnlyDictionary<string, IReadOnlyList<string>> WordsOffered);

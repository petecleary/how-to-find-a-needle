namespace PI.SearchApi.Pipeline.Rag;

/// <summary>What validation found in a finished answer (ADR-0016): citations, the sentinel, warnings and every check run.</summary>
public sealed record AnswerValidation(
    IReadOnlyList<string> Citations,
    IReadOnlyList<string> InvalidCitations,
    bool InsufficientEvidence,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<ValidationCheck> Checks);

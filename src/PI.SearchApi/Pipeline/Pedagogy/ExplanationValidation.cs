using PI.SearchApi.Contracts;
using PI.SearchApi.Pipeline.Rag;

namespace PI.SearchApi.Pipeline.Pedagogy;

/// <summary>What validation found in a finished explanation (ADR-0017). <see cref="Structure"/> is null for the baseline.</summary>
public sealed record ExplanationValidation(
    IReadOnlyList<string> Citations,
    IReadOnlyList<string> InvalidCitations,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<ValidationCheck> Checks,
    ExplanationStructure? Structure);

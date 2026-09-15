namespace PI.SearchApi.Contracts;

/// <summary>
/// A section's complete text and what validation found in it (ADR-0016). The text has already been shown, so
/// nothing is stripped or regenerated: problems are reported as warnings and stay visible.
/// </summary>
public sealed record AnswerFinal
{
    /// <summary><c>answer</c> or <c>explanation</c> (<see cref="AnswerSections"/>).</summary>
    public required string Section { get; init; }

    /// <summary>The full markdown exactly as the model wrote it, including any <c>INSUFFICIENT_EVIDENCE</c> first line.</summary>
    public required string Markdown { get; init; }

    /// <summary>Every <c>[PROD-…]</c> ID cited, once each, in order of first appearance.</summary>
    public required IReadOnlyList<string> Citations { get; init; }

    /// <summary>Cited IDs that weren't in the evidence set: the model mentioned a product it wasn't given.</summary>
    public required IReadOnlyList<string> InvalidCitations { get; init; }

    /// <summary>True when the first line is the <c>INSUFFICIENT_EVIDENCE</c> sentinel: the evidence didn't answer the question.</summary>
    public required bool InsufficientEvidence { get; init; }

    /// <summary>Plain-English problems found after generation. Heuristic checks say so in their text.</summary>
    public required IReadOnlyList<string> Warnings { get; init; }
}

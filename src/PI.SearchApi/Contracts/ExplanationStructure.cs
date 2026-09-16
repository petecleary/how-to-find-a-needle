namespace PI.SearchApi.Contracts;

/// <summary>
/// Stage 7's explanation, parsed from its five fixed headings (ADR-0017): decision → concepts → near miss → rule of
/// thumb → next step. A visible structure is a contract that can be streamed as text and still checked afterwards.
/// </summary>
public sealed record ExplanationStructure
{
    /// <summary>The product the Decision section recommends: its first citation.</summary>
    public required ExplanationProduct Decision { get; init; }

    /// <summary>The bold terms in the Concepts section, e.g. "Connector", "Wattage".</summary>
    public required IReadOnlyList<string> Concepts { get; init; }

    /// <summary>The counter-example the Near miss section uses: its first citation.</summary>
    public required ExplanationProduct NearMiss { get; init; }

    public required string? RuleOfThumb { get; init; }

    public required string? NextStep { get; init; }
}

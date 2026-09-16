namespace PI.SearchApi.Contracts;

/// <summary>
/// The first event of an answer stream (ADR-0016): which model is answering, and exactly which products it was
/// given, before a word of text arrives.
/// </summary>
public sealed record AnswerMeta
{
    public required string Stage { get; init; }

    public required string Provider { get; init; }

    public required string Model { get; init; }

    /// <summary>The product IDs in the evidence set, target device first. Every citation should be one of these.</summary>
    public required IReadOnlyList<string> Evidence { get; init; }
}

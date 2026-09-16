using PI.SearchApi.Pipeline;

namespace PI.SearchApi.Contracts;

/// <summary>
/// The whole answer as one JSON object, for <c>Accept: application/json</c> (ADR-0003): the same content the event
/// stream carries, after generation has finished. Integration tests, Scalar and the OpenAPI document use this form.
/// </summary>
public sealed record AnswerResponse
{
    public required string Stage { get; init; }

    public required string Provider { get; init; }

    public required string Model { get; init; }

    /// <summary>The product IDs in the evidence set, target device first.</summary>
    public required IReadOnlyList<string> Evidence { get; init; }

    /// <summary>One entry per section, in the order they were written.</summary>
    public required IReadOnlyList<AnswerFinal> Sections { get; init; }

    public required double? TimeToFirstTokenMs { get; init; }

    public required double TotalMs { get; init; }

    public required IReadOnlyList<TraceStep> Trace { get; init; }
}

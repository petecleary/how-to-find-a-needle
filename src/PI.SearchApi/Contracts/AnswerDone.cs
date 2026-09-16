using PI.SearchApi.Pipeline;

namespace PI.SearchApi.Contracts;

/// <summary>The last event of a successful answer stream: timings and the answer's own trace steps (ADR-0016).</summary>
public sealed record AnswerDone
{
    /// <summary>Milliseconds from sending the request to the first text chunk: what makes an answer feel fast. Null if no text arrived.</summary>
    public required double? TimeToFirstTokenMs { get; init; }

    /// <summary>Milliseconds for the whole answer request, including re-running retrieval.</summary>
    public required double TotalMs { get; init; }

    /// <summary>Prompt, generation and validation steps. The evidence step is in the results response's trace.</summary>
    public required IReadOnlyList<TraceStep> Trace { get; init; }
}

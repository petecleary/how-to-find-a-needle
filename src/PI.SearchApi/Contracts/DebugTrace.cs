using PI.SearchApi.Pipeline;

namespace PI.SearchApi.Contracts;

/// <summary>
/// The ordered list of pipeline steps behind a response (ADR-0003). Composed stages show their
/// whole pipeline, e.g. Stage 4 lists keyword, then vector, then fusion.
/// </summary>
public sealed record DebugTrace(IReadOnlyList<TraceStep> Steps);

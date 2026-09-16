namespace PI.SearchApi.Pipeline;

/// <summary>
/// What every pipeline service returns (ADR-0004): its candidates, in ranked order, plus the trace
/// steps that produced them. A composed stage appends its own step after the steps it received.
/// </summary>
/// <param name="TotalResults">
/// Only Stage 1 sets this, from a real COUNT(*). Ranked stages leave it null, and the endpoint
/// reports the number of candidates retrieved instead: at most candidateDepth per retriever, and up to
/// twice that after hybrid fusion, which keeps the union of the keyword and vector lists.
/// </param>
public sealed record StageResult(
    IReadOnlyList<Candidate> Candidates,
    IReadOnlyList<TraceStep> Trace,
    int? TotalResults = null);

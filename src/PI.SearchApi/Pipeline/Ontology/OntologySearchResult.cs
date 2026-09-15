namespace PI.SearchApi.Pipeline.Ontology;

/// <summary>
/// Stage 5's results plus what it worked out along the way: the target device, the query's understood concepts and
/// every rule check. Stages 6–7 build their evidence from these typed values rather than reading them back out of
/// the trace (ADR-0016).
/// </summary>
public sealed record OntologySearchResult(
    StageResult Result,
    TargetDevice Device,
    QueryUnderstanding Understanding,
    IReadOnlyList<CheckOutcome> Checks);

namespace PI.SearchApi.Pipeline;

/// <summary>
/// One entry in <c>debugTrace.steps</c> (ADR-0003): what a pipeline step did, how long it took, and
/// the evidence — the exact parameterised SQL with its parameters beside it (never interpolated),
/// plus stage-specific details such as the parsed tsquery, distances or RRF formulas.
/// </summary>
public sealed record TraceStep
{
    /// <summary>The stage slug that produced this step (keyword, vector, hybrid, ontology…).</summary>
    public required string Stage { get; init; }

    public required string Title { get; init; }

    public required double DurationMs { get; init; }

    /// <summary>The exact SQL statement that ran, with @parameters left in place.</summary>
    public string? Sql { get; init; }

    /// <summary>Parameter values for <see cref="Sql"/>, shown beside it.</summary>
    public IReadOnlyDictionary<string, object?>? Parameters { get; init; }

    /// <summary>
    /// Stage-specific structured data. Loosely typed on purpose, to avoid one response subtype per
    /// stage; the UI renders the keys it knows and shows the rest as JSON (ADR-0003).
    /// </summary>
    public IReadOnlyDictionary<string, object?>? Details { get; init; }

    /// <summary>Plain-English notes: caveats, honest naming, what this step can't do.</summary>
    public IReadOnlyList<string> Notes { get; init; } = [];
}

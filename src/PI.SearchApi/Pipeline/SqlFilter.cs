namespace PI.SearchApi.Pipeline;

/// <summary>
/// The WHERE conditions and parameters produced from a request's filters by <see cref="SqlFilterBuilder"/>.
/// </summary>
/// <param name="Conditions">SQL fragments to AND together, each drawn from a fixed set.</param>
/// <param name="Parameters">The values those fragments reference.</param>
/// <param name="Details">Trace details, e.g. how a broad category was expanded to narrower concepts.</param>
public sealed record SqlFilter(
    IReadOnlyList<string> Conditions,
    SqlParameters Parameters,
    IReadOnlyDictionary<string, object?> Details)
{
    /// <summary>
    /// Renders a WHERE clause from the stage's own conditions followed by the filter conditions,
    /// e.g. "WHERE search_vector @@ q\n  AND price &lt;= @maxPrice". Empty when there are none.
    /// </summary>
    public string WhereClause(params string[] stageConditions)
    {
        var all = stageConditions.Concat(Conditions).ToList();

        return all.Count == 0
            ? ""
            : "WHERE " + string.Join("\n  AND ", all);
    }
}

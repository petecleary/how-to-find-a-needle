using Npgsql;
using NpgsqlTypes;

namespace PI.SearchApi.Pipeline;

/// <summary>
/// An ordered set of SQL parameters that can be applied to a command <em>and</em> shown in the trace.
/// Keeping both views in one place guarantees the trace shows the values that actually ran, beside
/// the parameterised SQL rather than interpolated into it (ADR-0003).
/// </summary>
public sealed class SqlParameters
{
    private readonly List<Entry> _entries = [];

    /// <param name="name">Parameter name without the @.</param>
    /// <param name="value">The value sent to Postgres.</param>
    /// <param name="type">An explicit Npgsql type, when inference isn't enough.</param>
    /// <param name="traceValue">
    /// What the trace shows instead of <paramref name="value"/>, e.g. an abbreviated 768-dimension vector.
    /// </param>
    public SqlParameters Add(string name, object value, NpgsqlDbType? type = null, object? traceValue = null)
    {
        _entries.Add(new Entry(name, value, type, traceValue ?? value));
        return this;
    }

    public SqlParameters AddRange(SqlParameters other)
    {
        _entries.AddRange(other._entries);
        return this;
    }

    public void ApplyTo(NpgsqlCommand command)
    {
        foreach (var entry in _entries)
        {
            var parameter = entry.Type is { } type
                ? new NpgsqlParameter(entry.Name, type) { Value = entry.Value }
                : new NpgsqlParameter(entry.Name, entry.Value);

            command.Parameters.Add(parameter);
        }
    }

    public IReadOnlyDictionary<string, object?> ForTrace() =>
        _entries.ToDictionary(e => e.Name, e => (object?)e.TraceValue);

    private sealed record Entry(string Name, object Value, NpgsqlDbType? Type, object TraceValue);
}

using System.Diagnostics;

namespace PI.SearchApi.Pipeline;

/// <summary>
/// The OpenTelemetry source every pipeline step starts an Activity from (ADR-0003), so the Aspire
/// dashboard shows the same pipeline the debug trace shows. The name matches the application name
/// that <c>Extensions.ConfigureOpenTelemetry</c> subscribes to.
/// </summary>
public static class PipelineTelemetry
{
    public const string SourceName = "PI.SearchApi";

    public static ActivitySource Source { get; } = new(SourceName);

    /// <summary>Milliseconds since <paramref name="startTimestamp"/> (from <see cref="Stopwatch.GetTimestamp"/>), rounded for the trace.</summary>
    public static double ElapsedMs(long startTimestamp) =>
        Math.Round(Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds, 2);
}

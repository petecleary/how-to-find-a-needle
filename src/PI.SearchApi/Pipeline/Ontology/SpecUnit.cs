using System.Text.RegularExpressions;

namespace PI.SearchApi.Pipeline.Ontology;

/// <summary>
/// Spec names carry their unit (ADR-0005): <c>wattageW</c>, <c>voltageV</c>, <c>capacityAh</c>. Reading the
/// unit back out lets a reason say "45W", and lets "65W" in a query find the spec it constrains.
/// </summary>
public static partial class SpecUnit
{
    /// <summary>The unit at the end of a spec name ("wattageW" → "W"), or null if it has none.</summary>
    public static string? Of(string specName)
    {
        var match = UnitSuffix().Match(specName);
        return match.Success ? match.Value : null;
    }

    /// <summary>True for a unit a spec name can carry, compared case-insensitively ("w", "Ah", "MAH").</summary>
    public static bool IsKnown(string unit) =>
        Units.Contains(unit, StringComparer.OrdinalIgnoreCase);

    private static readonly string[] Units = ["W", "V", "Ah", "Gb", "Mah", "In", "Kg"];

    [GeneratedRegex("(W|V|Ah|Gb|Mah|In|Kg)$")]
    private static partial Regex UnitSuffix();
}

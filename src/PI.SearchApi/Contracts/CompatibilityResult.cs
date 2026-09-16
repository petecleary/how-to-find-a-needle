namespace PI.SearchApi.Contracts;

/// <summary>
/// Whether a candidate works with the target device, and why (ADR-0013). Reasons quote the
/// domain rule and the spec values compared, so a flagged item is never just "hidden".
/// </summary>
public sealed record CompatibilityResult(CompatibilityStatus Status, IReadOnlyList<string> Reasons)
{
    /// <summary>The value every stage before Stage 5 returns: no rule was checked.</summary>
    public static CompatibilityResult NotEvaluated { get; } = new(CompatibilityStatus.NotEvaluated, []);

    /// <summary>
    /// What the rules were checked against: the target device, the requirements stated in the query
    /// ("65W USB-C charger"), or nothing.
    /// </summary>
    public CompatibilitySource Source { get; init; } = CompatibilitySource.None;

    /// <summary>
    /// With no target device: for each device type the rules name, the catalog devices this product is
    /// compatible with ("fits 6 of 13 laptops"). Null when a target device was given or no rule applies.
    /// </summary>
    public IReadOnlyList<DeviceFit>? Fits { get; init; }
}

/// <summary>What Stage 5 checked a candidate's specs against (ADR-0013).</summary>
public enum CompatibilitySource
{
    /// <summary>Nothing: no rule was checked, or there was no device and no stated requirement.</summary>
    None,

    /// <summary>The target device's specs.</summary>
    Device,

    /// <summary>Requirements stated in the query, such as "USB-C" or "65W".</summary>
    Query,
}

/// <summary>The catalog devices of one type that a product is compatible with.</summary>
/// <param name="DeviceType">The device type's concept notation, e.g. "laptops".</param>
/// <param name="DeviceTypeLabel">Its English preferred label, e.g. "Laptops".</param>
/// <param name="Total">How many catalog devices of that type were checked.</param>
/// <param name="Devices">The ones every rule check passed for.</param>
public sealed record DeviceFit(string DeviceType, string DeviceTypeLabel, int Total, IReadOnlyList<FittingDevice> Devices);

/// <summary>A device a product fits.</summary>
public sealed record FittingDevice(string Id, string Name);

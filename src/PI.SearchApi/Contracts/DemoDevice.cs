namespace PI.SearchApi.Contracts;

/// <summary>
/// A product the shopper can pick as their target device (<c>GET /api/demo/devices</c>, ADR-0003):
/// any product in a category the ontology marks as a device type.
/// </summary>
public sealed record DemoDevice(string Id, string Name, string Brand, IReadOnlyList<string> Categories);

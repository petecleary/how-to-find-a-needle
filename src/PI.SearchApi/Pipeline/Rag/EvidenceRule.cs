namespace PI.SearchApi.Pipeline.Rag;

/// <summary>A domain rule that was checked for a product in the evidence, with each check's skos:definition.</summary>
/// <param name="Name">The accessory and device types it links, e.g. "chargers → laptops".</param>
/// <param name="SpecTerms">The spec names its checks compare, e.g. connector, chargingPort, wattageW: Stage 7's expert vocabulary.</param>
public sealed record EvidenceRule(string Name, IReadOnlyList<string> Definitions, IReadOnlyList<string> SpecTerms);

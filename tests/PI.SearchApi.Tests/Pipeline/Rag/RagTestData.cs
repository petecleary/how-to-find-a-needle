using System.Text.Json;
using PI.SearchApi.Contracts;
using PI.SearchApi.Pipeline;
using PI.SearchApi.Pipeline.Ontology;
using PI.SearchApi.Pipeline.Rag;

namespace PI.SearchApi.Tests.Pipeline.Rag;

/// <summary>Small hand-made products and evidence, shaped like GQ-01, for the Stage 6 unit tests.</summary>
public static class RagTestData
{
    public static readonly IOntology DomainModel = new DomainOntology(Path.Combine(AppContext.BaseDirectory, "assets", "data"));

    public static ProductSummary Product(string id, string name, string[] categories, string specsJson = "{}", decimal price = 49.99m) =>
        new(id, name, "Voltline", categories, price, JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(specsJson)!);

    public static Candidate Candidate(
        string id,
        CompatibilityStatus status,
        ConceptMatch? conceptMatch = ConceptMatch.InConcept,
        params string[] reasons) =>
        new(
            Product(id, $"Product {id}", ["laptop-chargers"]),
            0.5,
            new CandidateSignals { ConceptMatch = conceptMatch },
            new CompatibilityResult(status, reasons));

    /// <summary>GQ-01 in miniature: the laptop, a compatible USB-C charger and the 45W barrel near miss.</summary>
    public static EvidenceSet Gq01Evidence() => new(
        [
            new EvidenceItem(
                Product("PROD-0001", "Blackbird Aerobook 14", ["laptops"], """{"chargingPort":"usb-c","minChargerWattageW":65}""", 999.99m),
                null, CompatibilityResult.NotEvaluated, null, null, EvidenceRole.TargetDevice, "The device you own."),
            new EvidenceItem(
                Product("PROD-0012", "Voltline 65W USB-C GaN Charger", ["laptop-chargers"], """{"connector":"usb-c","wattageW":65,"powerDelivery":true}"""),
                "Compact 65W charger.",
                new CompatibilityResult(CompatibilityStatus.Compatible, ["✓ The charger's plug must fit the laptop's charging port. Voltline 65W USB-C GaN Charger has USB-C; Blackbird Aerobook 14 needs USB-C."]),
                ConceptMatch.InConcept, 1, EvidenceRole.Compatible, "#1"),
            new EvidenceItem(
                Product("PROD-0014", "Voltline 45W Barrel Charger", ["laptop-chargers"], """{"connector":"barrel-5.5mm","wattageW":45}""", 29.99m),
                null,
                new CompatibilityResult(CompatibilityStatus.Incompatible, ["✗ The charger's plug must fit the laptop's charging port. Voltline 45W Barrel Charger has 5.5mm barrel; Blackbird Aerobook 14 needs USB-C."]),
                ConceptMatch.InConcept, 5, EvidenceRole.Incompatible, "#5"),
        ],
        [new EvidenceConcept("laptop-chargers", "Laptop chargers", ["power adapter", "power brick"], "A charger for a laptop.")],
        [new EvidenceRule("chargers → laptops", ["The charger's plug must fit the laptop's charging port."])]);
}

namespace PI.SearchApi.Pipeline.Rag;

/// <summary>
/// Everything the model is allowed to use (ADR-0016): a bounded list of products with verdicts and reasons, the
/// concepts the query matched and the rules that were checked. "Answer only from the evidence" means this, and
/// the trace shows it in full.
/// </summary>
public sealed record EvidenceSet(
    IReadOnlyList<EvidenceItem> Items,
    IReadOnlyList<EvidenceConcept> Concepts,
    IReadOnlyList<EvidenceRule> Rules)
{
    /// <summary>Every product ID in the evidence, target device first. A citation outside this list is invalid.</summary>
    public IReadOnlyList<string> ProductIds => [.. Items.Select(i => i.Product.Id)];

    public EvidenceItem? TargetDevice => Items.FirstOrDefault(i => i.Role == EvidenceRole.TargetDevice);

    /// <summary>
    /// With no target device: what the query asked for, e.g. <c>"USB-C": connector is usb-c</c>. The products'
    /// compatibility was checked against these instead of a device (ADR-0013).
    /// </summary>
    public IReadOnlyList<string> StatedRequirements { get; init; } = [];

    public EvidenceItem? Find(string productId) => Items.FirstOrDefault(i => i.Product.Id == productId);

    /// <summary>The same evidence with each product's description filled in and truncated.</summary>
    public EvidenceSet WithDescriptions(IReadOnlyDictionary<string, string> descriptions) => this with
    {
        Items = [.. Items.Select(item => descriptions.TryGetValue(item.Product.Id, out var description)
            ? item with { Description = EvidenceFormatter.Truncate(description) }
            : item)],
    };
}

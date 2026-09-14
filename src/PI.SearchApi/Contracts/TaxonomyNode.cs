namespace PI.SearchApi.Contracts;

/// <summary>
/// One concept in the category tree returned by <c>GET /api/taxonomy</c> (ADR-0013). The UI builds
/// its category filter from it; the same SKOS concepts drive query understanding in Stage 6.
/// </summary>
public sealed record TaxonomyNode
{
    /// <summary>The skos:notation products reference in their categories, e.g. "laptop-chargers".</summary>
    public required string Notation { get; init; }

    /// <summary>The English preferred label.</summary>
    public required string Label { get; init; }

    /// <summary>Preferred labels by language tag, e.g. { "en": "Chargers", "es": "Cargadores" }.</summary>
    public required IReadOnlyDictionary<string, string> Labels { get; init; }

    /// <summary>Synonyms (skos:altLabel) in any language.</summary>
    public required IReadOnlyList<string> AltLabels { get; init; }

    public string? Definition { get; init; }

    /// <summary>A Lucide icon name.</summary>
    public string? Icon { get; init; }

    /// <summary>True when products in this category can be a target device.</summary>
    public required bool IsDeviceType { get; init; }

    public required IReadOnlyList<TaxonomyNode> Narrower { get; init; }
}

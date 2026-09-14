using PI.SearchApi.Contracts;

namespace PI.SearchApi.Pipeline.Ontology;

/// <summary>
/// Turns the flat list of SKOS concepts into the nested tree <c>GET /api/taxonomy</c> returns
/// (ADR-0013). Each concept knows only its skos:broader parent, so the tree is rebuilt top-down.
/// </summary>
public static class TaxonomyTreeBuilder
{
    public static IReadOnlyList<TaxonomyNode> Build(IReadOnlyList<OntologyConcept> concepts)
    {
        var childrenByParent = concepts
            .Where(c => c.BroaderNotation is not null)
            .GroupBy(c => c.BroaderNotation!)
            .ToDictionary(g => g.Key, g => g.OrderBy(c => c.Notation, StringComparer.Ordinal).ToList());

        var roots = concepts
            .Where(c => c.BroaderNotation is null)
            .OrderBy(c => c.Notation, StringComparer.Ordinal);

        return [.. roots.Select(root => ToNode(root, childrenByParent))];
    }

    private static TaxonomyNode ToNode(OntologyConcept concept, Dictionary<string, List<OntologyConcept>> childrenByParent)
    {
        var children = childrenByParent.GetValueOrDefault(concept.Notation) ?? [];

        return new TaxonomyNode
        {
            Notation = concept.Notation,
            Label = concept.PrefLabels.GetValueOrDefault("en") ?? concept.Notation,
            Labels = concept.PrefLabels,
            AltLabels = concept.AltLabels,
            Definition = concept.Definition,
            Icon = concept.Icon,
            IsDeviceType = concept.IsDeviceType,
            Narrower = [.. children.Select(child => ToNode(child, childrenByParent))],
        };
    }
}

using FastEndpoints;
using PI.SearchApi.Contracts;
using PI.SearchApi.Pipeline.Ontology;

namespace PI.SearchApi.Endpoints.Demo;

/// <summary>
/// <c>GET /api/taxonomy</c>: the SKOS concept tree from <c>domain-ontology.ttl</c> (ADR-0013), with
/// multilingual labels, synonyms, definitions and icons. The UI builds its category filter from it.
/// </summary>
public sealed class TaxonomyEndpoint(IOntology ontology) : EndpointWithoutRequest<IReadOnlyList<TaxonomyNode>>
{
    public override void Configure()
    {
        Get("/api/taxonomy");
        AllowAnonymous();
        Summary(s => s.Summary = "The product category tree (SKOS taxonomy)");
    }

    public override async Task HandleAsync(CancellationToken ct) =>
        await Send.OkAsync(TaxonomyTreeBuilder.Build(ontology.Concepts), ct);
}

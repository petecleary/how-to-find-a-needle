using FastEndpoints;
using PI.SearchApi.Contracts;
using PI.SearchApi.Pipeline.Ontology;

namespace PI.SearchApi.Endpoints.Demo;

/// <summary>
/// <c>GET /api/vocabularies</c>: the value vocabularies from <c>domain-ontology.ttl</c> (ADR-0013), each
/// with its values, synonyms and the spec keys that use it. The UI builds its spec filters from it.
/// </summary>
public sealed class VocabulariesEndpoint(IOntology ontology) : EndpointWithoutRequest<IReadOnlyList<ValueVocabulary>>
{
    public override void Configure()
    {
        Get("/api/vocabularies");
        AllowAnonymous();
        Summary(s => s.Summary = "Allowed spec values (SKOS value vocabularies)");
    }

    public override async Task HandleAsync(CancellationToken ct) =>
        await Send.OkAsync(ValueVocabularyBuilder.Build(ontology.Vocabularies, ontology.Rules), ct);
}

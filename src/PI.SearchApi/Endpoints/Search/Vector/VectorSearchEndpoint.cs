using FastEndpoints;
using PI.SearchApi.Contracts;
using PI.SearchApi.Pipeline;
using PI.SearchApi.Pipeline.Ontology;
using PI.SearchApi.Pipeline.Vector;

namespace PI.SearchApi.Endpoints.Search.Vector;

/// <summary><c>POST /api/search/vector</c> — Stage 3 (ADR-0010). Returns 503 if the embedding model is missing.</summary>
public sealed class VectorSearchEndpoint(IVectorSearch search) : Endpoint<SearchRequest, SearchResponse>
{
    public override void Configure()
    {
        Post("/api/search/vector");
        AllowAnonymous();
        Validator<VectorSearchRequestValidator>();
        Summary(s => s.Summary = "Stage 3 — Vector search: Nomic embeddings + pgvector cosine distance");
    }

    public override async Task HandleAsync(SearchRequest req, CancellationToken ct)
    {
        var start = System.Diagnostics.Stopwatch.GetTimestamp();

        var result = await search.SearchAsync(req, embeddingText: null, ct);

        await Send.OkAsync(SearchResponseMapper.ToResponse("vector", req, result, PipelineTelemetry.ElapsedMs(start)), ct);
    }
}

public sealed class VectorSearchRequestValidator : Validator<SearchRequest>
{
    public VectorSearchRequestValidator()
    {
        SearchRequestRules.AddTo(this, Resolve<IOntology>(), requireQuery: true);
    }
}

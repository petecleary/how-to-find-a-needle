using PI.SearchApi.Contracts;

namespace PI.SearchApi.Pipeline.Ontology;

/// <summary>
/// Stage 6: understand → expand → hybrid retrieval → classify → constrain (ADR-0013). Flagged items are
/// kept, demoted, with reasons.
/// </summary>
public interface IOntologySearch
{
    Task<StageResult> SearchAsync(SearchRequest request, CancellationToken ct);
}

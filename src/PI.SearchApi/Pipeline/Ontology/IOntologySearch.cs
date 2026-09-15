using PI.SearchApi.Contracts;

namespace PI.SearchApi.Pipeline.Ontology;

/// <summary>
/// Stage 5: understand → expand → hybrid retrieval → classify → constrain (ADR-0013). Flagged items are
/// kept, demoted, with reasons.
/// </summary>
public interface IOntologySearch
{
    Task<StageResult> SearchAsync(SearchRequest request, CancellationToken ct);

    /// <summary>The same search, also returning the target device, the understood query and the rule checks (for Stages 6–7).</summary>
    Task<OntologySearchResult> SearchWithContextAsync(SearchRequest request, CancellationToken ct);
}

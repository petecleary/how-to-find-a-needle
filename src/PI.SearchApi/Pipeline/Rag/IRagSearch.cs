using PI.SearchApi.Contracts;

namespace PI.SearchApi.Pipeline.Rag;

/// <summary>
/// The retrieval half of Stages 6–7 (ADR-0016): the Stage 5 pipeline, then the evidence set the model will be given,
/// as one more trace step. It never calls the LLM, so results render at once, even with the LLM stopped.
/// </summary>
public interface IRagSearch
{
    /// <param name="stage">The stage slug for the evidence trace step: <c>rag</c> or <c>pedagogy</c>.</param>
    Task<RagSearchResult> SearchAsync(SearchRequest request, string stage, CancellationToken ct);
}

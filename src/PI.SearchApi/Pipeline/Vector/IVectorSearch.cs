using PI.SearchApi.Contracts;

namespace PI.SearchApi.Pipeline.Vector;

/// <summary>
/// Stage 3: embed the query, then rank products by cosine distance with pgvector (ADR-0010).
/// </summary>
public interface IVectorSearch
{
    /// <summary>
    /// Returns up to <c>options.candidateDepth</c> candidates, nearest first, with the request's filters
    /// applied in the same SQL statement.
    /// </summary>
    /// <param name="embeddingText">
    /// Stage 5's expanded text (the query plus concept labels); null embeds <c>request.Query</c> as typed.
    /// </param>
    Task<StageResult> SearchAsync(SearchRequest request, string? embeddingText, CancellationToken ct);
}

using PI.SearchApi.Contracts;
using PI.SearchApi.Pipeline.Rag;

namespace PI.SearchApi.Pipeline.Pedagogy;

/// <summary>
/// Stage 7's answer stream (ADR-0017): the Stage 6 answer, generated and validated exactly as in Stage 6, then an
/// audience-aware explanation of it. Events: <c>meta</c>, answer <c>delta</c>… <c>final</c>, explanation
/// <c>delta</c>… <c>final</c>, <c>done</c>.
/// </summary>
public interface IPedagogyEngine
{
    IAsyncEnumerable<AnswerEvent> StreamAsync(SearchRequest request, CancellationToken ct);
}

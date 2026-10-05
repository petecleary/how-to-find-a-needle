using PI.SearchApi.Contracts;
using PI.SearchApi.Llm;

namespace PI.SearchApi.Pipeline.Rag;

/// <summary>
/// Stage 6's answer (ADR-0016): rebuilds the evidence set, streams a grounded markdown summary from the LLM, then
/// validates it. Events arrive in order: <c>meta</c>, <c>delta</c>…, <c>final</c>, <c>done</c>.
/// </summary>
public interface IAnswerGenerator
{
    IAsyncEnumerable<AnswerEvent> StreamAsync(SearchRequest request, CancellationToken ct);

    /// <summary>
    /// Streams only the answer section for evidence already built: <c>delta</c> events, then <c>final</c>.
    /// Stage 7 calls this before writing its explanation, so both stages' answers are generated identically.
    /// <paramref name="model"/> is the request's resolved model (ADR-0019), so the caller decides which model answers.
    /// </summary>
    IAsyncEnumerable<AnswerEvent> StreamAnswerSectionAsync(
        string stage,
        string question,
        EvidenceSet evidence,
        LlmCall model,
        AnswerSectionOutcome outcome,
        CancellationToken ct);
}

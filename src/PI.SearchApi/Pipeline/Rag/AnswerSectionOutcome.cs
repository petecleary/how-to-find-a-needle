using PI.SearchApi.Contracts;
using PI.SearchApi.Llm;

namespace PI.SearchApi.Pipeline.Rag;

/// <summary>
/// What streaming one section produced, filled in as its events are read: the LLM call, the validated final event and
/// the section's trace steps. A caller that streams several sections (Stage 7) reads each one after its final event.
/// </summary>
public sealed class AnswerSectionOutcome
{
    public LlmGeneration Generation { get; } = new();

    public AnswerFinal? Final { get; internal set; }

    public List<TraceStep> TraceSteps { get; } = [];
}

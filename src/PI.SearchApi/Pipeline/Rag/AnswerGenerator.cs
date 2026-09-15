using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using PI.SearchApi.Contracts;
using PI.SearchApi.Llm;

namespace PI.SearchApi.Pipeline.Rag;

// Stage 6 — RAG: grounded, cited answer (the answer request)
//
// What:     Rebuilds the evidence set, renders the system and user prompts from assets/prompts/, streams the
//           model's markdown chunk by chunk as it's written, then validates the finished text: citations must be
//           in the evidence, the INSUFFICIENT_EVIDENCE sentinel is detected, and near-miss mentions are checked.
// Strength: "Cite or it didn't happen." The answer is checkable claim by claim, it warns about the near miss
//           using the rules' own reasons, and streaming makes a multi-second call feel immediate.
// Failure:  The LLM is still the least reliable part: it can misquote a spec with a valid citation, or ignore an
//           instruction. Validation catches the form of a mistake, not every mistake, and shows what it found.
// Decision: docs/adr/0016-rag-grounding-and-citations.md
public sealed class AnswerGenerator(
    IRagSearch ragSearch,
    IChatClient chatClient,
    IOptions<LlmOptions> llmOptions,
    PromptLibrary prompts) : IAnswerGenerator
{
    public const string SystemPromptFile = "rag-system.md";
    public const string UserPromptFile = "rag-user.md";

    public async IAsyncEnumerable<AnswerEvent> StreamAsync(SearchRequest request, [EnumeratorCancellation] CancellationToken ct)
    {
        using var activity = PipelineTelemetry.Source.StartActivity("Stage 6: RAG answer");
        var start = Stopwatch.GetTimestamp();
        var options = llmOptions.Value;

        // The answer endpoint is stateless: it re-runs retrieval (tens of milliseconds) to get the same evidence.
        var retrieval = await ragSearch.SearchAsync(request, "rag", ct);

        yield return AnswerEvent.Meta(new AnswerMeta
        {
            Stage = "rag",
            Provider = options.Provider,
            Model = options.Model,
            Evidence = retrieval.Evidence.ProductIds,
        });

        var outcome = new AnswerSectionOutcome();
        double? timeToFirstTokenMs = null;

        await foreach (var answerEvent in StreamAnswerSectionAsync("rag", request.Query, retrieval.Evidence, outcome, ct))
        {
            // Measured from the start of the request, including retrieval: the wait the audience actually sees.
            if (answerEvent.Name == AnswerEvent.DeltaName)
            {
                timeToFirstTokenMs ??= PipelineTelemetry.ElapsedMs(start);
            }

            yield return answerEvent;
        }

        yield return AnswerEvent.Done(new AnswerDone
        {
            TimeToFirstTokenMs = timeToFirstTokenMs,
            TotalMs = PipelineTelemetry.ElapsedMs(start),
            Trace = outcome.TraceSteps,
        });
    }

    public async IAsyncEnumerable<AnswerEvent> StreamAnswerSectionAsync(
        string stage,
        string question,
        EvidenceSet evidence,
        AnswerSectionOutcome outcome,
        [EnumeratorCancellation] CancellationToken ct)
    {
        var options = llmOptions.Value;
        var chatOptions = LlmChatOptions.For(options);
        var llm = LlmTraceInfo.From(options, chatOptions);

        var promptStart = Stopwatch.GetTimestamp();
        var systemPrompt = prompts.Get(SystemPromptFile);
        var userPrompt = RenderUserPrompt(prompts, question, evidence);
        outcome.TraceSteps.Add(PromptStep(stage, systemPrompt, userPrompt, llm, PipelineTelemetry.ElapsedMs(promptStart)));

        // The rules go in the system message and the evidence in the user message: instructions the model should
        // always follow, kept apart from the data it should answer from.
        List<ChatMessage> messages =
        [
            new(ChatRole.System, systemPrompt),
            new(ChatRole.User, userPrompt),
        ];

        await foreach (var chunk in LlmStreaming.StreamTextAsync(chatClient, messages, chatOptions, options, outcome.Generation, ct))
        {
            yield return AnswerEvent.Delta(AnswerSections.Answer, chunk);
        }

        var generation = outcome.Generation;
        outcome.TraceSteps.Add(GenerateStep(stage, generation, llm));

        var validateStart = Stopwatch.GetTimestamp();
        var validation = AnswerValidator.Validate(generation.Text, evidence);
        var final = new AnswerFinal
        {
            Section = AnswerSections.Answer,
            Markdown = generation.Text,
            Citations = validation.Citations,
            InvalidCitations = validation.InvalidCitations,
            InsufficientEvidence = validation.InsufficientEvidence,
            Warnings = [.. generation.FinishWarnings, .. validation.Warnings],
        };
        outcome.Final = final;
        outcome.TraceSteps.Add(ValidateStep(stage, validation, final.Warnings, PipelineTelemetry.ElapsedMs(validateStart)));

        yield return AnswerEvent.Final(final);
    }

    /// <summary>The user message: the question and the evidence, pasted into <c>rag-user.md</c>.</summary>
    public static string RenderUserPrompt(PromptLibrary prompts, string question, EvidenceSet evidence) =>
        PromptTemplate.Render(prompts.Get(UserPromptFile), new Dictionary<string, string>
        {
            ["question"] = question,
            ["targetDevice"] = EvidenceFormatter.FormatTargetDevice(evidence),
            ["products"] = EvidenceFormatter.FormatProducts(evidence),
            ["concepts"] = EvidenceFormatter.FormatConcepts(evidence),
            ["rules"] = EvidenceFormatter.FormatRules(evidence),
        });

    private static TraceStep PromptStep(string stage, string systemPrompt, string userPrompt, LlmTraceInfo llm, double durationMs) => new()
    {
        Stage = stage,
        Title = "Prompt: the answer's system and user messages",
        DurationMs = durationMs,
        Details = new Dictionary<string, object?>
        {
            ["section"] = AnswerSections.Answer,
            ["promptFiles"] = new[] { $"assets/prompts/{SystemPromptFile}", $"assets/prompts/{UserPromptFile}" },
            ["systemPrompt"] = systemPrompt,
            ["userPrompt"] = userPrompt,
            ["llm"] = llm,
        },
        Notes =
        [
            "Shown exactly as sent. The system message holds the grounding rules; the user message holds the question and the evidence.",
            "Both come from files in assets/prompts/, so the prompt can be reviewed and changed without touching code.",
        ],
    };

    private static TraceStep GenerateStep(string stage, LlmGeneration generation, LlmTraceInfo llm) => new()
    {
        Stage = stage,
        Title = "Generate: the streamed answer",
        DurationMs = generation.TotalMs,
        Details = new Dictionary<string, object?>
        {
            ["section"] = AnswerSections.Answer,
            ["llm"] = llm,
            ["rawOutput"] = generation.Text,
            ["timeToFirstTokenMs"] = generation.TimeToFirstTokenMs,
            ["totalMs"] = generation.TotalMs,
            ["finishReason"] = generation.FinishReason,
            ["inputTokens"] = generation.InputTokens,
            ["outputTokens"] = generation.OutputTokens,
        },
        Notes =
        [
            "Each chunk went to the browser as a delta event the moment it arrived, before any of it was checked.",
            "Time to first token is measured here from sending the prompt; the done event measures it from the start of the request.",
        ],
    };

    private static TraceStep ValidateStep(string stage, AnswerValidation validation, IReadOnlyList<string> warnings, double durationMs) => new()
    {
        Stage = stage,
        Title = "Validate: citations, sentinel and warnings",
        DurationMs = durationMs,
        Details = new Dictionary<string, object?>
        {
            ["section"] = AnswerSections.Answer,
            ["citations"] = validation.Citations,
            ["invalidCitations"] = validation.InvalidCitations,
            ["insufficientEvidence"] = validation.InsufficientEvidence,
            ["checks"] = validation.Checks,
            ["warnings"] = warnings,
        },
        Notes =
        [
            "The finished text is checked after it has been shown. Nothing is stripped or regenerated: problems stay visible as warnings.",
            "No retries: a silent second attempt would hide the failure this step exists to show.",
        ],
    };
}

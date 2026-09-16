using System.Diagnostics;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;
using PI.SearchApi.Contracts;
using PI.SearchApi.Llm;
using PI.SearchApi.Pipeline.Rag;

namespace PI.SearchApi.Pipeline.Pedagogy;

// Stage 7 — Pedagogy: explain the decision, for an audience
//
// What:     Streams the Stage 6 answer first (same prompt, same validation), then a second LLM call turns that
//           checked answer into an explanation for the chosen audience: decision → concepts → near miss → rule
//           of thumb → next step, built on the ontology's definitions and labels. options.applyPedagogy: false
//           sends the same facts, audience and words through a plain baseline prompt instead.
// Strength: Separates *correct* from *understood*. The near miss, the system's hardest case, becomes the
//           reader's clearest lesson, and the baseline toggle shows what the teaching design adds on its own.
// Failure:  Two LLM calls per request, so it's the slowest stage; and the explanation is only as good as the
//           ontology's definitions and the model's discipline. Validation shows drift, it can't prevent it.
// Decision: docs/decisions/0017-pedagogy-engine.md
public sealed class PedagogyEngine(
    IRagSearch ragSearch,
    IAnswerGenerator answerGenerator,
    IChatClient chatClient,
    IOptions<LlmOptions> llmOptions,
    PedagogyPromptBuilder promptBuilder) : IPedagogyEngine
{
    private const string Stage = "pedagogy";

    public async IAsyncEnumerable<AnswerEvent> StreamAsync(SearchRequest request, [EnumeratorCancellation] CancellationToken ct)
    {
        using var activity = PipelineTelemetry.Source.StartActivity("Stage 7: pedagogy answer and explanation");
        var start = Stopwatch.GetTimestamp();
        var options = llmOptions.Value;
        var applyPedagogy = request.Options.ApplyPedagogy;
        var audience = request.Options.Audience;

        var retrieval = await ragSearch.SearchAsync(request, Stage, ct);
        var evidence = retrieval.Evidence;

        yield return AnswerEvent.Meta(new AnswerMeta
        {
            Stage = Stage,
            Provider = options.Provider,
            Model = options.Model,
            Evidence = evidence.ProductIds,
        });

        // 1. The answer: generated and validated exactly as in Stage 6.
        var answer = new AnswerSectionOutcome();
        double? timeToFirstTokenMs = null;

        await foreach (var answerEvent in answerGenerator.StreamAnswerSectionAsync(Stage, request.Query, evidence, answer, ct))
        {
            if (answerEvent.Name == AnswerEvent.DeltaName)
            {
                timeToFirstTokenMs ??= PipelineTelemetry.ElapsedMs(start);
            }

            yield return answerEvent;
        }

        // The answer section always sets Final before it finishes; a failure would have thrown instead.
        var answerFinal = answer.Final ?? throw new InvalidOperationException("The answer section finished without a final event.");
        var explanationStartMs = PipelineTelemetry.ElapsedMs(start);

        // 2. The explanation, built on the validated answer. It starts only after the answer's final event.
        var explanation = new AnswerSectionOutcome();
        var chatOptions = LlmChatOptions.For(options);
        var llm = LlmTraceInfo.From(options, chatOptions);

        var promptStart = Stopwatch.GetTimestamp();
        var prompt = promptBuilder.Build(request.Query, audience, applyPedagogy, evidence, answerFinal.Markdown);
        explanation.TraceSteps.Add(PromptStep(prompt, llm, PipelineTelemetry.ElapsedMs(promptStart)));

        List<ChatMessage> messages =
        [
            new(ChatRole.System, prompt.SystemPrompt),
            new(ChatRole.User, prompt.UserPrompt),
        ];

        await foreach (var chunk in LlmStreaming.StreamTextAsync(chatClient, messages, chatOptions, options, explanation.Generation, ct))
        {
            yield return AnswerEvent.Delta(AnswerSections.Explanation, chunk);
        }

        var generation = explanation.Generation;
        explanation.TraceSteps.Add(GenerateStep(prompt, generation, llm, answer.Generation, explanationStartMs));

        var validateStart = Stopwatch.GetTimestamp();
        var validation = ExplanationValidator.Validate(generation.Text, evidence, applyPedagogy, answerFinal.InsufficientEvidence);
        var final = new AnswerFinal
        {
            Section = AnswerSections.Explanation,
            Markdown = generation.Text,
            Citations = validation.Citations,
            InvalidCitations = validation.InvalidCitations,
            InsufficientEvidence = answerFinal.InsufficientEvidence,
            Warnings = [.. generation.FinishWarnings, .. validation.Warnings],
            Structure = validation.Structure,
        };
        explanation.TraceSteps.Add(ValidateStep(prompt, validation, final.Warnings, PipelineTelemetry.ElapsedMs(validateStart)));

        yield return AnswerEvent.Final(final);

        yield return AnswerEvent.Done(new AnswerDone
        {
            TimeToFirstTokenMs = timeToFirstTokenMs,
            TotalMs = PipelineTelemetry.ElapsedMs(start),
            Trace = [.. answer.TraceSteps, .. explanation.TraceSteps],
        });
    }

    private static TraceStep PromptStep(PedagogyPrompt prompt, LlmTraceInfo llm, double durationMs) => new()
    {
        Stage = Stage,
        Title = prompt.ApplyPedagogy
            ? "Prompt: the explanation, with the pedagogy prompt"
            : "Prompt: the explanation, with the baseline prompt",
        DurationMs = durationMs,
        Details = new Dictionary<string, object?>
        {
            ["section"] = AnswerSections.Explanation,
            ["applyPedagogy"] = prompt.ApplyPedagogy,
            ["audience"] = prompt.Audience,
            ["promptFiles"] = new[] { $"assets/prompts/{prompt.SystemPromptFile}", $"assets/prompts/{PedagogyPromptBuilder.UserPromptFile}" },
            ["systemPrompt"] = prompt.SystemPrompt,
            ["userPrompt"] = prompt.UserPrompt,
            ["audienceGuidance"] = prompt.AudienceGuidance,
            ["wordsOffered"] = prompt.WordsOffered,
            ["llm"] = llm,
        },
        Notes = prompt.ApplyPedagogy
            ?
            [
                $"Pedagogy on: teaching principles and five fixed headings, with the {prompt.Audience} section of {PedagogyPromptBuilder.AudiencesFile}.",
                "The user message is identical to the baseline's: same answer, evidence, audience and words. Only the system prompt differs.",
            ]
            :
            [
                "Same facts, same audience, no pedagogical structure.",
                "The baseline is a fair first prompt: it names the audience and keeps the grounding rules, but has no principles or headings.",
            ],
    };

    private static TraceStep GenerateStep(PedagogyPrompt prompt, LlmGeneration generation, LlmTraceInfo llm, LlmGeneration answerGeneration, double explanationStartMs) => new()
    {
        Stage = Stage,
        Title = "Generate: the streamed explanation",
        DurationMs = generation.TotalMs,
        Details = new Dictionary<string, object?>
        {
            ["section"] = AnswerSections.Explanation,
            ["applyPedagogy"] = prompt.ApplyPedagogy,
            ["llm"] = llm,
            ["rawOutput"] = generation.Text,
            ["timeToFirstTokenMs"] = generation.TimeToFirstTokenMs,
            ["totalMs"] = generation.TotalMs,
            ["finishReason"] = generation.FinishReason,
            ["inputTokens"] = generation.InputTokens,
            ["outputTokens"] = generation.OutputTokens,
            // Per-section timings (ADR-0017): the answer's call, then the explanation's, which starts after the answer's final event.
            ["sectionTimings"] = new
            {
                answer = new { timeToFirstTokenMs = answerGeneration.TimeToFirstTokenMs, totalMs = answerGeneration.TotalMs },
                explanation = new { startedAtMs = explanationStartMs, timeToFirstTokenMs = generation.TimeToFirstTokenMs, totalMs = generation.TotalMs },
            },
        },
        Notes =
        [
            "A second LLM call: the answer said what to choose; this one explains why, for the audience.",
            "The explanation streamed while the answer was already on screen, so the panel never sat empty.",
        ],
    };

    private static TraceStep ValidateStep(PedagogyPrompt prompt, ExplanationValidation validation, IReadOnlyList<string> warnings, double durationMs) => new()
    {
        Stage = Stage,
        Title = prompt.ApplyPedagogy
            ? "Validate: citations and teaching structure"
            : "Validate: citations (structure checks not applied)",
        DurationMs = durationMs,
        Details = new Dictionary<string, object?>
        {
            ["section"] = AnswerSections.Explanation,
            ["applyPedagogy"] = prompt.ApplyPedagogy,
            ["citations"] = validation.Citations,
            ["invalidCitations"] = validation.InvalidCitations,
            ["checks"] = validation.Checks,
            ["structure"] = validation.Structure,
            ["warnings"] = warnings,
        },
        Notes = prompt.ApplyPedagogy
            ? ["Structure is checked against the evidence: the decision must be Compatible, the near miss Incompatible. The concept check is a heuristic."]
            : ["Baseline prompt: only citations are checked. There is no structure to parse, so structure is null."],
    };
}

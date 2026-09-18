# ADR-0015: LLM provider & client

- **Status:** Accepted for Ollama (Phase 4, 2026-09-16): built, verified and the default model chosen by bake-off. The **Anthropic** provider is built and unit-tested but not yet run live, for want of an API key; **OpenAI** is wired up and tested in Phase 5.
- **Date:** 2026-09-13 (amended 2026-09-15 at the start of Phase 4, agreed with Pete: settings live on the API, not forwarded by the AppHost; the Anthropic default is `claude-sonnet-5`; the bake-off is an opt-in integration test over the two installed Ollama models)
- **Related:** ADR-0002, ADR-0016, ADR-0017; roadmap Phase 4

## Context

Stages 6 and 7 need an LLM. Pete already runs Ollama locally, and learners without it should be able to use a hosted provider (OpenAI or Anthropic) with their own API key.

We don't need to run a model in a container. Aspire doesn't manage the LLM at all; it only passes configuration to the API.

The original design put LiteLLM in front of the providers. .NET's `Microsoft.Extensions.AI` gives us a provider-neutral `IChatClient`, so LiteLLM would be an extra service without an extra lesson.

## Decision

### Providers

| Provider | Endpoint | Client package | Key |
|---|---|---|---|
| `ollama` (default for the presenter) | the existing local Ollama, e.g. `http://localhost:11434` | `Microsoft.Extensions.AI.OpenAI` against Ollama's OpenAI-compatible `/v1` API | none |
| `openai` | OpenAI API | `Microsoft.Extensions.AI.OpenAI` | user secrets |
| `anthropic` | Claude API | **`Anthropic`**, the official Anthropic .NET SDK, via its `Microsoft.Extensions.AI` `IChatClient` integration (native, not an OpenAI-compatible shim) | user secrets |

### Configuration

- An `Llm` section in the API's configuration: `Provider`, `Model`, `Endpoint` (Ollama only), `ApiKey`, `MaxOutputTokens` and `TimeoutSeconds`.
- The non-secret values are defaults in the API's `appsettings.json`. The key is stored with `dotnet user-secrets` on `PI.SearchApi` and **never** committed. User secrets are one more configuration source, so the factory reads the key like any other setting. `Llm__Model`-style environment variables override both (the bake-off uses this).
- The AppHost does **not** forward `Llm` settings as parameters: one obvious place to set them beats a second route through the dashboard.
- The provider and model are fixed for a run (one `IChatClient`, built at startup). Changing them means restarting `aspire run`; they are never a per-request choice, so the request contract stays the same for every stage.
- There is no container and no `CommunityToolkit.Aspire.Hosting.Ollama`.

### Client (API)

- **One `IChatClient` registered in DI**, built by a small `LlmClientFactory` that switches on `Provider`. This is the **only** provider-specific code; Stages 6–7 depend on `IChatClient` alone.
- **Middleware pipeline** (`ChatClientBuilder`): `.UseOpenTelemetry()`, so prompts and timings appear in the Aspire dashboard; `.UseLogging()` in development.
- **Streaming:** Stages 6–7 call `IChatClient.GetStreamingResponseAsync`, which is supported by every provider, and forward text chunks to the browser as Server-Sent Events ([ADR-0003](0003-search-api-contract-and-debug-trace.md)).
- **Markdown output, validated when complete.** The model writes markdown with inline `[PROD-…]` citations (and fixed headings in Stage 7). The API validates the finished text for every provider ([ADR-0016](0016-rag-grounding-and-citations.md), [ADR-0017](0017-pedagogy-engine.md)). JSON-schema output isn't used, because it can't be shown progressively.
- **Sampling and reasoning parameters are provider-specific** (all in `Llm/LlmChatOptions.cs`).
  - Ollama and OpenAI: `Temperature = 0.1` for predictable demo output.
  - **Ollama: thinking off.** Qwen 3.6 and Gemma 4 "think" before answering by default, and through Ollama's `/v1` API the reasoning arrives before any answer text. In a 60-token test both models spent every token reasoning and wrote no answer. `ChatOptions.Reasoning.Effort = None` is sent as `reasoning_effort: "none"` and turns it off: measured through the API on 2026-09-15, the first token arrives in 38–55 ms once the model is loaded (about 5 s for the very first call, while Ollama loads it: the warm-up service's job). Ollama's own `think: false` field is ignored on `/v1`.
  - Anthropic: **do not send `temperature`**. Current Claude models (for example Claude Opus 5) reject sampling parameters. Use the provider's effort setting to trade depth for latency instead: `Reasoning.Effort = Low`, which the SDK's `IChatClient` sends as `output_config.effort` with adaptive thinking (`AnthropicThinkingMode.Adaptive`).
- **Anthropic refusals:** the SDK's `IChatClient` reports a `refusal` stop reason as `ChatFinishReason.ContentFilter`; the stages surface it as a clear error in the trace. Server-side refusal fallbacks are a beta request parameter the `IChatClient` integration doesn't expose, and the refusal policy categories aren't expected for product answers, so fallbacks are not enabled: a recorded limitation. *(The live Anthropic check needs a key; see roadmap Phase 4 step 1.)*
- **Limits:** output-token cap sized for a short summary (about 1–2K), request timeout 60 s, **no retries** (a failed demo call should fail visibly, not hang).
- **Unavailable LLM:** `503` ProblemDetails with provider-specific guidance ("Is Ollama running? `ollama serve` / `ollama pull <model>`", or "Set `Llm:ApiKey` with `dotnet user-secrets`").
- **Warm-up (Ollama, development):** an optional one-token call at startup, so the first demo request isn't cold. It is logged and never blocks startup on failure.
- **Trace:** provider, model, endpoint host (never the key), sampling/effort settings, token counts when reported, and duration.

### Model defaults (confirm in Phase 4)

- **Ollama:** chosen by a bake-off on the presenter laptop (Apple silicon, 64 GB) between the two models already installed, `qwen3.6:35b` and `gemma4:31b`. `qwen3.6:35b` is the provisional default until the bake-off runs. No smaller model is tested; the README names one as an untested suggestion for learners on lighter hardware. The bake-off is an **opt-in integration test** (`tests/PI.SearchApi.IntegrationTests/BakeOff/`), skipped unless `PI_BAKEOFF_MODELS` names the models. It calls the real Stage 6–7 answer endpoints in JSON mode, so it scores the real prompts and validators, and it runs after they exist (roadmap Phase 4 step 4). The criteria:
  1. Correct structure (Stage 7 headings, sentinel when needed) and no citation warnings on all golden queries in 10 of 10 runs.
  2. Correct citations.
  3. Time to first token under ~1.5 s, and a complete Stage 6 answer under ~8 s, on the presenter laptop.
- **Anthropic:** `claude-sonnet-5` by default: lower cost per call for learners, and fast enough for a streamed summary. `claude-opus-5` is a documented config change for the most capable answers, `claude-haiku-4-5` for the cheapest. Claude Sonnet 5 rejects non-default sampling parameters and runs adaptive thinking by default, so latency is tuned with the effort setting, not temperature. Built and tested live in Phase 4.
- **OpenAI:** set by the learner in config. The README lists a suggested model at publish time, since model versions change quickly. The provider is wired to `IChatClient` but **built and tested late** (roadmap Phase 5), because there are no credits during the main build.

### Bake-off results (2026-09-16, presenter laptop: Apple silicon, 64 GB)

`qwen3.6:35b`, 7 golden queries × 10 runs × 3 scenarios = **210 requests, none failed** (`PI_BAKEOFF_MODELS=qwen3.6:35b PI_BAKEOFF_RUNS=10`):

| Scenario | No invalid citations | No warnings | Structure checks pass | First token median | Total median / max |
|---|---|---|---|---|---|
| Stage 6 | 70/70 | 70/70 | — | 73 ms | 2.3 s / 4.6 s |
| Stage 7, pedagogy on | 70/70 | 40/70 | 65/70 | 43 ms | 5.4 s / 8.7 s |
| Stage 7, baseline | 70/70 | 70/70 | — | 57 ms | 5.1 s / 8.9 s |

- **Every criterion is met:** no citation was ever outside the evidence, the five headings and the Decision and Near miss rules held in 65 of 70 runs, the first token arrives in well under 1.5 s, and Stage 7's two calls finish in about 5.4 s against the ~15 s budget.
- **The 30 "insufficient evidence" answers** are the three golden queries with no target device (GQ-02, GQ-04, GQ-07), where no rule can be checked. The model saying so is correct behaviour, not a failure.
- **The warnings are mostly the concept heuristic**, and the bake-off exposed two faults in the evidence and the checker, now fixed: with no target device the evidence carried no rules at all, and a concept named after a product's own spec ("Capacity (Ah)", "Form Factor") counted as "not from the ontology".
- **`gemma4:31b` was abandoned mid-run and is not the default.** It generates about 22 tokens/second here against qwen's much higher rate (it runs every weight per token; the Qwen model activates only part of itself), so a request took about 15 s and Stage 7 about 30 s: twice the talk's budget. Loading both models also pushed a 64 GB machine into swap.

**Chosen default: `qwen3.6:35b`** (already the value in `appsettings.json`).

**How long it takes:** the 210 requests are 350 LLM calls, about 20 minutes of model time. Measured, the run took **three hours**: with the display asleep, macOS throttled the .NET processes to roughly one call every 30 seconds, even under `caffeinate -i` (which prevents idle sleep but not throttling). Run it with the display kept awake:

```sh
caffeinate -dimsu env PI_BAKEOFF_MODELS=qwen3.6:35b PI_BAKEOFF_RUNS=10 dotnet test tests/PI.SearchApi.IntegrationTests --filter ModelBakeOff
```

The timings inside the report are the API's own and are unaffected: they measure each call, not the gaps between them.

## Consequences

- No LLM infrastructure to run. The presenter uses their existing Ollama, and learners pick a provider with two settings.
- Provider differences are contained in one factory: sampling parameters, refusal handling, native SDK vs OpenAI-compatible.
- Small local models are less fluent than hosted ones. Grounding and pedagogy prompts must be explicit and structured, and server-side validation catches mistakes ([ADR-0016](0016-rag-grounding-and-citations.md), [ADR-0017](0017-pedagogy-engine.md)).
- Hosted providers cost money per call; the README says so, next to the key setup.

## Alternatives considered

| Option | Why not (for this repo) |
|---|---|
| LiteLLM proxy | Extra service; `IChatClient` already gives provider switching |
| Ollama in an Aspire container | Not needed (Ollama is already running); containers on macOS can't use the Apple GPU |
| Anthropic via its OpenAI-compatible endpoint | A shim; the official `Anthropic` SDK has native `IChatClient` support and full feature coverage |
| Semantic Kernel | Much larger surface (planners, plugins) than two prompt calls need |
| Hosted-only | Needs keys and internet on stage; breaks the offline demo |
| In-process model (ONNX GenAI / LLamaSharp) | Model management and hardware acceleration complexity outweigh the benefit |

## Teaching notes

- Put the LLM behind an abstraction (`IChatClient`) and treat it as a replaceable dependency, not the architecture.
- Providers differ in the details (sampling parameters, refusals, streaming). Keep those differences in one place.
- Streaming makes an LLM feel fast; validating the finished text keeps it honest.

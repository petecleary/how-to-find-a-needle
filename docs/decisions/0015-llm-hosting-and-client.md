# ADR-0015: LLM provider & client

- **Status:** Accepted
- **Area:** AI
- **Related:** [ADR-0002](0002-solution-structure-and-orchestration.md), [ADR-0016](0016-rag-grounding-and-citations.md), [ADR-0017](0017-pedagogy-engine.md)

## Context

Stages 6 and 7 need an LLM. The presenter runs Ollama locally, so the talk works offline. Learners without a capable local machine should be able to use a hosted provider with their own API key.

.NET's `Microsoft.Extensions.AI` gives a provider-neutral `IChatClient`, so no proxy service is needed to switch provider.

## Decision

### Providers

| Provider | How the API talks to it | Key |
|---|---|---|
| `ollama` (default) | Your local Ollama, through its OpenAI-compatible `/v1` API, with `Microsoft.Extensions.AI.OpenAI` | none |
| `openai` | The OpenAI API, with `Microsoft.Extensions.AI.OpenAI` | user secrets |
| `anthropic` | The Claude API, with the official **`Anthropic`** .NET SDK's native `IChatClient` | user secrets |

### Configuration

- An `Llm` section in the API's `appsettings.json`: `Provider`, `Model`, `Endpoint` (Ollama only), `MaxOutputTokens` (1,500) and `TimeoutSeconds` (60).
- **The key never goes in a file.** Set it with `dotnet user-secrets` on `PI.SearchApi`; it is read like any other setting.
- The provider and model are fixed for a run: one `IChatClient`, built at startup. Restart `aspire run` after changing them. They are never a per-request choice, so every stage keeps the same request contract.
- Aspire doesn't run or manage the LLM, and there is no container.

### The client

- **One `IChatClient` in DI**, built by `LlmClientFactory`, which switches on `Provider`. **That factory and the per-provider options are the only provider-specific code**; Stages 6–7 depend on `IChatClient` alone.
- **Middleware:** OpenTelemetry, so prompts and timings appear in the Aspire dashboard, and logging in development.
- **Streaming:** Stages 6–7 call `GetStreamingResponseAsync` and forward text chunks to the browser as Server-Sent Events ([ADR-0003](0003-search-api-contract-and-debug-trace.md)).
- **Markdown out, validated when complete.** The model writes markdown with `[PROD-…]` citations, and the API checks the finished text ([ADR-0016](0016-rag-grounding-and-citations.md), [ADR-0017](0017-pedagogy-engine.md)). A JSON schema can't be shown as it streams.
- **Sampling and reasoning settings differ by provider** (in `Llm/LlmChatOptions.cs`):
  - Ollama and OpenAI: `Temperature = 0.1`, for predictable demo output.
  - **Ollama: thinking off.** Current local models "think" before answering by default, and through `/v1` all of that arrives before any answer text. Setting reasoning effort to none turns it off: the first token then arrives in tens of milliseconds once the model is loaded.
  - Anthropic: **no `temperature`**, which current Claude models reject. Latency is tuned with the effort setting (`Low`) instead.
- **No retries.** A failed demo call should fail visibly, not hang.
- **Unavailable LLM:** `503` with provider-specific guidance ("Is Ollama running?", "Set `Llm:ApiKey` with `dotnet user-secrets`"). Stages 1–5 are unaffected.
- **Warm-up:** with Ollama, an optional one-token call at startup loads the model, so the first answer on stage isn't the slow one. It never blocks startup.
- **Trace:** provider, model, endpoint host (never the key), settings, token counts when reported, and timings.

### Choosing the local model: a bake-off

The default Ollama model was chosen by an **opt-in integration test** (`tests/PI.SearchApi.IntegrationTests/BakeOff/`) that calls the real Stage 6–7 endpoints for every golden query, repeatedly, and scores the real prompts and validators. It is skipped unless `PI_BAKEOFF_MODELS` names the models. The criteria: correct structure and no invalid citations, time to first token under ~1.5 s, and a complete Stage 6 answer under ~8 s on the presenter's laptop.

Results for **`qwen3.6:35b`** (the default), 7 golden queries × 10 runs × 3 scenarios = 210 requests, none failed:

| Scenario | No invalid citations | Structure checks pass | First token (median) | Total (median / max) |
|---|---|---|---|---|
| Stage 6 | 70/70 | — | 73 ms | 2.3 s / 4.6 s |
| Stage 7, pedagogy on | 70/70 | 65/70 | 43 ms | 5.4 s / 8.7 s |
| Stage 7, baseline | 70/70 | — | 57 ms | 5.1 s / 8.9 s |

`gemma4:31b` was dropped mid-run: at about 22 tokens per second on the same machine, Stage 7 took around 30 seconds.

For hosted providers, the default Anthropic model is `claude-sonnet-5`: fast enough for a streamed summary and cheaper per call for learners. The README lists the suggested OpenAI model.

## Consequences

- No LLM infrastructure to run. Learners pick a provider with two settings.
- Provider differences (sampling parameters, reasoning, refusals, native SDK or compatible API) stay in one factory.
- Local models are less fluent than hosted ones, so prompts must be explicit and structured, and validation catches the rest.
- Hosted providers cost money per call; the README says so next to the key setup.

## Alternatives considered

| Option | Why not (for this repository) |
|---|---|
| LiteLLM proxy | Another service; `IChatClient` already switches provider |
| Ollama in an Aspire container | Unnecessary, and containers on macOS can't use the Apple GPU |
| Anthropic through its OpenAI-compatible endpoint | A shim; the official SDK has native `IChatClient` support |
| Semantic Kernel | Far more surface (planners, plugins) than two prompt calls need |
| Hosted providers only | Needs keys and internet on stage |
| An in-process model (ONNX GenAI, LLamaSharp) | Model management and hardware acceleration outweigh the benefit |

## What to take away

- **Put the LLM behind an abstraction** and treat it as a replaceable dependency, not the architecture.
- **Providers differ in the details** (sampling parameters, reasoning, refusals, streaming). Keep those differences in one place.
- **Streaming makes an LLM feel fast; validating the finished text keeps it honest.**
- **"Thinking" can eat the whole answer.** In a 60-token test, both local models spent every token reasoning and wrote nothing. Check what a model does by default before you set a token budget.
- **Choose a model by measuring it on your task.** A bake-off over the real prompts, validators and golden queries answers "which model?" better than a leaderboard.
- **Benchmarks measure the machine too.** The bake-off's 20 minutes of model time took three hours with the display asleep, because macOS throttled the processes. The per-call timings were unaffected; the wall clock wasn't.

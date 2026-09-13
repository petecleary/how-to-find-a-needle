# ADR-0015: LLM provider & client

- **Status:** Proposed
- **Date:** 2026-09-13
- **Related:** ADR-0002, ADR-0016, ADR-0017; roadmap Phase 4

## Context

Stages 7 and 8 need an LLM. Pete already runs Ollama locally, and learners without it should be able to use a hosted provider (OpenAI or Anthropic) with their own API key.

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

- An `Llm` section in the API's configuration: `Provider`, `Model`, `Endpoint` (Ollama only) and `ApiKey`.
- Keys are stored with `dotnet user-secrets` and **never** committed. The AppHost may forward `Llm` settings as parameters so everything is set in one place.
- There is no container and no `CommunityToolkit.Aspire.Hosting.Ollama`.

### Client (API)

- **One `IChatClient` registered in DI**, built by a small `LlmClientFactory` that switches on `Provider`. This is the **only** provider-specific code; Stages 7–8 depend on `IChatClient` alone.
- **Middleware pipeline** (`ChatClientBuilder`): `.UseOpenTelemetry()`, so prompts and timings appear in the Aspire dashboard; `.UseLogging()` in development.
- **Streaming:** Stages 7–8 call `IChatClient.GetStreamingResponseAsync`, which is supported by every provider, and forward text chunks to the browser as Server-Sent Events ([ADR-0003](0003-search-api-contract-and-debug-trace.md)).
- **Markdown output, validated when complete.** The model writes markdown with inline `[PROD-…]` citations (and fixed headings in Stage 8). The API validates the finished text for every provider ([ADR-0016](0016-rag-grounding-and-citations.md), [ADR-0017](0017-pedagogy-engine.md)). JSON-schema output isn't used, because it can't be shown progressively.
- **Sampling parameters are provider-specific.**
  - Ollama and OpenAI: `Temperature = 0.1` for predictable demo output.
  - Anthropic: **do not send `temperature`**. Current Claude models (for example Claude Opus 5) reject sampling parameters. Use the provider's effort setting to trade depth for latency instead.
- **Anthropic refusals:** check for a `refusal` stop reason and surface it as a clear error in the trace. Anthropic recommends opting into server-side refusal fallbacks for current models. Enable them if the `IChatClient` integration exposes them; otherwise record it as a limitation. *(Confirm when implementing.)*
- **Limits:** output-token cap sized for a short summary (about 1–2K), request timeout 60 s, **no retries** (a failed demo call should fail visibly, not hang).
- **Unavailable LLM:** `503` ProblemDetails with provider-specific guidance ("Is Ollama running? `ollama serve` / `ollama pull <model>`", or "Set `Llm:ApiKey` with `dotnet user-secrets`").
- **Warm-up (Ollama, development):** an optional one-token call at startup, so the first demo request isn't cold. It is logged and never blocks startup on failure.
- **Trace:** provider, model, endpoint host (never the key), sampling/effort settings, token counts when reported, and duration.

### Model defaults (confirm in Phase 4)

- **Ollama:** chosen by a bake-off on the presenter laptop (Apple silicon, 64 GB), which comfortably runs mid-size models such as the ~30–35B Qwen class Pete already uses. Include one smaller model as the suggestion for learners on lighter hardware. The criteria:
  1. Correct structure (Stage 8 headings, sentinel when needed) and no citation warnings on all golden queries in 10 of 10 runs.
  2. Correct citations.
  3. Time to first token under ~1.5 s, and a complete Stage 7 answer under ~8 s, on the presenter laptop.
- **Anthropic:** `claude-opus-5` by default. A smaller or cheaper Claude model is a documented config change the learner can choose.
- **OpenAI:** set by the learner in config. The README lists a suggested model at publish time, since model versions change quickly. The provider is wired to `IChatClient` but **built and tested late** (roadmap Phase 5), because there are no credits during the main build.

Record the bake-off results and chosen defaults here.

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

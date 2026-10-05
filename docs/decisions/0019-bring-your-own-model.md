# ADR-0019: Choose the model per request (bring your own model)

- **Status:** Proposed
- **Area:** AI
- **Related:** [ADR-0003](0003-search-api-contract-and-debug-trace.md), [ADR-0015](0015-llm-hosting-and-client.md), [ADR-0016](0016-rag-grounding-and-citations.md), [ADR-0017](0017-pedagogy-engine.md)

## Context

[ADR-0015](0015-llm-hosting-and-client.md) puts the LLM behind `IChatClient` and sets a default provider and model in configuration. A learner who clones the repo has a different local model, or a key for a different provider, and wants to compare answers without editing files and restarting.

## Decision

### Providers

| Provider | `id` | How the API talks to it | Key |
|---|---|---|---|
| Ollama | `ollama` | OpenAI client against Ollama's `/v1` | none |
| OpenAI | `openai` | OpenAI client | `OPENAI_API_KEY` |
| Anthropic | `anthropic` | Official `Anthropic` SDK, native `IChatClient` | `ANTHROPIC_API_KEY` |
| Azure OpenAI | `azure` | OpenAI client against the resource's v1 endpoint | `AZURE_OPENAI_API_KEY` |
| Google Gemini | `google` | OpenAI client against Gemini's OpenAI-compatible API | `GEMINI_API_KEY` |
| OpenAI-compatible | `compat` | OpenAI client against any compatible server (LM Studio, vLLM, OpenRouter) | `OPENAI_COMPAT_API_KEY` (optional) |

Every provider except Anthropic uses the same OpenAI client with a different base URL, so no extra packages are needed.

### The model is a request option

- `options.model` is `"provider/model"`, e.g. `"ollama/qwen3.6:35b"`. It is split at the first `/`, so model names may contain slashes.
- **Null means the configured default** (`Llm:Provider` and `Llm:Model`).
- Stages 1–5 ignore it. Stages 6–7 show the provider, model and endpoint host in the trace.
- An unknown provider is a `400`; a provider that can't be used right now (no key, not running) is a `503` with the fix.
- **The key is never part of the request.** The request names a model; the API finds its key.

### Keys and settings

- Keys come from, in order: a key pasted into **Settings**, held in the API's memory for this run only; the provider's environment variable or user secret (`dotnet user-secrets set ANTHROPIC_API_KEY …`); and, for the default provider, `Llm:ApiKey`.
- A key is never written to a file, returned by an endpoint, logged or traced.
- `~/.needle/settings.json` holds the non-secret settings per provider: enabled, base URL, and extra model names (Azure deployments can't be listed).

### The registry

- **A client is built per request.** Building one is cheap, and a cache would be hidden state.
- Model lists are fetched **live** from each provider when the UI asks. A provider that can't be listed appears as a problem, never silently dropped. No retries: a provider that is down should look down.

### UI

- A **model picker** in the Stage 6 and 7 options. The model is part of the URL (the key never is), and changing it re-runs the answer on the same evidence.
- A **Models and API keys** panel, opened from the header, to enable providers, set base URLs, paste a key for the session, and test the connection.

## Consequences

- Switching provider or model needs no restart, and two models can be compared on the same evidence.
- A shared URL reproduces the answer setup, minus the key.
- More to maintain: six providers, a settings file and six small endpoints.
- The API is a local, single-user tool: whoever can reach it can use the keys it holds.

## Alternatives considered

| Option | Why not (for this repository) |
|---|---|
| Restart to switch (the original ADR-0015 design) | The friction this decision removes |
| A server-side "active model" | The request would no longer describe the result |
| The model in a request header | Breaks the same-request contract ([ADR-0017](0017-pedagogy-engine.md) rejects a header for the audience too) |
| The key in the request, from browser storage | Puts keys in the browser and the network tab |
| A proxy such as LiteLLM | Another service; `IChatClient` already switches provider |
| Cache clients and model lists | Hidden state: a stale list hides a provider that just stopped |

## What to take away

- **The model is a request option, like the audience.** Change it and the same evidence, prompt and validators produce a different answer. That separates what the model contributes from what the pipeline contributes.
- **Validation doesn't care which model wrote the text.** Citations outside the evidence are caught either way, so warning counts are a quick way to compare models.
- **Name the model, never send the key.** The request says *which* model; the server decides *how* to reach it.

# ADR-0019: Choose the model per request (bring your own model)

- **Status:** Proposed (2026-10-05). Amends [ADR-0015](0015-llm-hosting-and-client.md) (the model is no longer fixed for a run) and [ADR-0003](0003-search-api-contract-and-debug-trace.md) (`options.model`).
- **Date:** 2026-10-05 (agreed with Pete: six providers, as the MOCA example app supports; the model travels in `options.model`; keys from configuration or pasted for the session only)
- **Related:** ADR-0003, ADR-0014, ADR-0015, ADR-0016, ADR-0017; roadmap Phase 7

## Context

ADR-0015 builds one `IChatClient` at startup from the `Llm` section. Changing the model means editing `appsettings.json` or user secrets and restarting `aspire run`. That suits a rehearsed talk, but not a learner who clones the repo: they have a different Ollama model, or a key for a different provider, and want to compare answers without restarting.

The MOCA example app (`openmoca/moca-example-app`, its ADR-0005) already solves this with a provider registry that builds a client per request, keys held for the session only, a settings file in the user's home folder and a model picker. This ADR ports that design and adapts it to this repo's rules: same request for every stage, no caching, no retries, and the trace shows what happened.

## Decision

### Providers

| Provider | `id` | How the API talks to it | Key (environment variable or user secret) | Default base URL |
|---|---|---|---|---|
| Ollama | `ollama` | OpenAI client against Ollama's `/v1` | none | `http://localhost:11434` |
| OpenAI | `openai` | OpenAI client | `OPENAI_API_KEY` | `https://api.openai.com/v1` |
| Anthropic | `anthropic` | Official `Anthropic` SDK, native `IChatClient` | `ANTHROPIC_API_KEY` | `https://api.anthropic.com` |
| Azure OpenAI | `azure` | OpenAI client against the resource's v1 endpoint | `AZURE_OPENAI_API_KEY` | none: set the resource URL |
| Google Gemini | `google` | OpenAI client against Gemini's OpenAI-compatible API | `GEMINI_API_KEY` | `https://generativelanguage.googleapis.com/v1beta/openai` |
| OpenAI-compatible | `compat` | OpenAI client against any compatible server (LM Studio, vLLM, OpenRouter) | `OPENAI_COMPAT_API_KEY` (optional) | `http://localhost:1234/v1` |

No new packages: every provider except Anthropic goes through the `OpenAI` client that `Microsoft.Extensions.AI.OpenAI` already brings, with a base URL. There is no `Azure.AI.OpenAI` and no `OllamaSharp`.

### The model is a request option

- `options.model` is `"provider/model"`, e.g. `"ollama/qwen3.6:35b"` or `"anthropic/claude-sonnet-5"`. It is split at the **first** `/`, so a model name may contain slashes (`"compat/meta-llama/llama-3.1-8b"`).
- **Null means the default**, the `Llm:Provider` and `Llm:Model` from configuration (ADR-0015). Existing requests, the golden-query tests and the bake-off's `Llm__Model` override are unchanged.
- Stages 1–5 ignore it, as they ignore `audience`. Stages 6–7 list it in the trace (provider, model, endpoint host).
- Validation: a model that isn't `provider/model` with a known provider is a `400`. A known provider that can't be used (no key, no base URL, not running) is a `503` with the fix, as before.
- **The key is never part of the request.** The request names a model; the API finds its key.

### Keys

Keys come from, in order:
1. A key pasted into **Settings** in the UI, held in the API's memory for this run only (`PUT /api/providers/{id}/key`). It is lost when the API stops.
2. The provider's environment variable from the table above, read through configuration, so `dotnet user-secrets set ANTHROPIC_API_KEY …` works too.
3. For the default provider only, `Llm:ApiKey`: the ADR-0015 setting keeps working.

A key is never written to a file by the app, never returned by an endpoint, never logged and never traced. Endpoints report only where a key came from (`none`, `configuration`, `session`). An OS keychain is out of scope; `ISecretStore` is the seam if one is wanted.

### Non-secret settings

`~/.needle/settings.json` (override the folder with `NEEDLE_HOME`) holds, per provider: enabled, base URL, and extra model names (Azure deployments can't be listed with a key, so they are typed in). It never holds a key. The default model stays in the `Llm` configuration: one place, as ADR-0015 says.

Ollama, OpenAI, Anthropic and Google are enabled by default; Azure and the compatible endpoint start disabled, because they need a URL first.

### The registry

- `LlmModelRegistry.Resolve(model)` returns the client, the effective `LlmOptions` and the provider's `ChatOptions` for one request. **A client is built per request**: building one is cheap, and a cache would be hidden state.
- Model lists are fetched **live** when the UI asks (`GET /api/models`): Ollama's `/api/tags` (with tool, vision and context-length details), Anthropic's `/v1/models`, and `/models` for the OpenAI-style providers. They are **not cached**, unlike the MOCA app. A provider that can't be listed appears as a problem in the response, never silently dropped.
- The model-list HTTP client has **no resilience handler**: the service defaults' retries would hide a provider that is down.

### Endpoints

| Route | Purpose |
|---|---|
| `GET /api/providers` | Each provider: enabled, base URL, where its key comes from, extra models. No network calls. |
| `PUT /api/providers/{id}` | Change enabled, base URL or extra models |
| `PUT /api/providers/{id}/key` | Hold a key for this run |
| `DELETE /api/providers/{id}/key` | Forget the session key (a configured key stays) |
| `POST /api/providers/{id}/test` | List the provider's models now; report success or the reason it failed |
| `GET /api/models` | The models of every enabled provider, the default model, and any provider problems |

### UI

- A **model picker** in the Stage 6 and 7 options row. The model is part of the search state, so it is in the URL (never the key), and changing it re-runs the answer with the same evidence.
- A **Models and API keys** sheet, opened from the header: per provider, enable, base URL, key (password field, "this session only"), and Test connection.

## Consequences

- A learner switches provider and model without restarting, and can compare two models on the same evidence: the point of ADR-0016's validation.
- The request now names its model, so a shared URL reproduces the answer setup (minus the key).
- More surface: six providers, a settings file and six endpoints. Only Ollama is verified live; the others are wired and unit-tested, and the README says so.
- Session keys are lost on restart, deliberately.
- The API is a single-user, local tool: anyone who can reach it can use the keys it holds. That fits the "clone and run" model; a shared deployment would need per-user keys (see "What would change this decision" in the MOCA ADR).

## Alternatives considered

| Option | Why not (for this repo) |
|---|---|
| Keep ADR-0015: restart to switch | The friction this ADR removes |
| A server-side "active model" set from the UI | The request would no longer describe the result; two tabs would fight over one setting |
| The model in a request header | ADR-0017 already rejects headers for `audience`: it breaks the same-request contract |
| The key in the request, from browser storage | Puts keys in the browser, the network tab and possibly the URL |
| LiteLLM or another proxy | Another service; `IChatClient` already switches provider (ADR-0015) |
| Cache clients and model lists (as the MOCA app does) | Hidden state; a stale list hides a provider that just stopped. Building a client costs microseconds |
| `Azure.AI.OpenAI` | An extra package; Azure's v1 endpoint accepts the plain OpenAI client |

## Teaching notes

- **The model is a request option, like `audience`.** Switch it and the same evidence, prompt and validators get a different answer: that is how you see what the model contributes and what the pipeline contributes.
- **Validation is model-agnostic.** Citations outside the evidence are caught whichever model wrote them; comparing warning counts across models is a quick quality check.
- **Name the model, never send the key.** The request says *which* model; the server decides *how* to reach it.
- Suggested talk moment: GQ-03 at Stage 6 with the local model, then the same URL with a hosted model; the evidence panel doesn't change, the prose does.

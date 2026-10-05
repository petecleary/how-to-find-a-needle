# PI.SearchApi — the search pipeline

The ASP.NET Core API that runs every stage of the talk, from a `WHERE` clause to an audience-aware LLM explanation. Run it through the AppHost (`aspire run` from the repository root), never on its own: Aspire supplies the Postgres connection string.

Once it is running, open **Search API (Scalar)** from the `searchapi` resource in the Aspire dashboard to call any endpoint from the browser.

## How the code is organised

| Folder | Holds |
|---|---|
| [`Program.cs`](Program.cs) | The composition root: one commented block per stage registers its services. Read it top to bottom for an overview |
| [`Contracts/`](Contracts) | The request and response every stage shares (`SearchRequest`, `SearchResponse`, `DebugTrace`), plus the answer and model contracts |
| [`Endpoints/`](Endpoints) | Thin [FastEndpoints](https://fast-endpoints.com/) classes: validate, call one service, page, map. No search logic lives here |
| [`Pipeline/`](Pipeline) | One folder per technique: `Structured/`, `Keyword/`, `Vector/`, `Fusion/` + `Hybrid/`, `Ontology/`, `Rag/`, `Pedagogy/`. Shared types (`Candidate`, `StageResult`, `TraceStep`, `SqlFilterBuilder`) sit at the top |
| [`Embeddings/`](Embeddings) | The local Nomic embedding model on ONNX Runtime: tokenise, run, mean-pool, normalise |
| [`Llm/`](Llm) | Everything provider-specific for Stages 6–7: the provider catalogue, the per-request model registry, keys, settings, streaming |
| [`Data/`](Data) | Schema creation and seeding: loads `products.json`, diffs by content hash, loads or computes embeddings |
| [`assets/data/`](assets/data) | `products.json`, `domain-ontology.ttl`, SPARQL queries (`queries/*.rq`), `golden-queries.json`, `init.sql`, committed embeddings |
| [`assets/prompts/`](assets/prompts) | The LLM prompts as markdown, so they can be read and changed without touching C# |
| [`assets/models/`](assets/models) | The downloaded ONNX model (not committed): see [its README](assets/models/README.md) |

## Reading a stage

Every pipeline service starts with the same header:

```csharp
// Stage 4 — Hybrid search (Reciprocal Rank Fusion)
//
// What:     …what the technique does, with its formula
// Strength: …what it is good at
// Failure:  …where it breaks, which is what the next stage fixes
// Decision: docs/decisions/0011-hybrid-search-rrf.md
```

Read the header, then the method that runs the search, then the SQL constant next to it. Later stages are **composed** from earlier ones (Stage 4 calls Stages 2 and 3; Stage 5 calls Stage 4; Stages 6–7 call Stage 5), and each adds its own trace step after the ones it received ([ADR-0004](../../docs/decisions/0004-pipeline-composition.md)).

## The trace

Every response carries `debugTrace.steps`: one entry per step, with the **exact parameterised SQL and its parameters** (never interpolated), timings, and stage-specific details such as tsqueries, cosine distances, RRF arithmetic, matched concepts, rule checks and prompts. The web UI's *Under the hood* tab renders these; in Scalar you see the raw JSON. The trace is the point of the API, not a debugging aid ([ADR-0003](../../docs/decisions/0003-search-api-contract-and-debug-trace.md)).

## Configuration

| Setting | Where | What it does |
|---|---|---|
| `Embeddings:Rebuild` | `appsettings.json` or `Embeddings__Rebuild=true` | Re-embeds every product and rewrites `assets/data/embeddings/nomic.jsonl`. Use after editing product text |
| `Llm:Provider`, `Llm:Model`, `Llm:Endpoint` | `appsettings.json` | The **default** model for Stages 6–7 (a local Ollama). A request can name another in `options.model` |
| `Llm:MaxOutputTokens`, `Llm:TimeoutSeconds` | `appsettings.json` | Limits for every model call. There are no retries: a failure shows as a `503` with the fix |
| `ANTHROPIC_API_KEY`, `OPENAI_API_KEY`, `GEMINI_API_KEY`, `AZURE_OPENAI_API_KEY`, `OPENAI_COMPAT_API_KEY` | `dotnet user-secrets` or environment | Hosted-provider keys. Never put a key in a file |
| `NEEDLE_HOME` | environment | Where model settings are saved (default `~/.needle`) |

Set a key for every run with:

```sh
dotnet user-secrets set "ANTHROPIC_API_KEY" "<your key>" --project src/PI.SearchApi
```

Or paste one into **Models and API keys** in the web UI: the API holds it in memory until it stops ([ADR-0019](../../docs/decisions/0019-bring-your-own-model.md)).

## When something is missing

A missing embedding model, a stopped Ollama or a provider without a key is an expected state, not a crash. The API returns `503 Service Unavailable` with ProblemDetails whose `detail` says exactly what to do, and the stages that don't need the missing piece keep working.

## Things to try

- **Add a synonym** to a concept in `assets/data/domain-ontology.ttl` (`skos:altLabel`), restart, and search for it on Stage 5. The trace's expansion step shows it.
- **Change the RRF weights** (`options.keywordWeight`, `options.vectorWeight`) on a Stage 4 request and watch the fused ranks move in the trace.
- **Edit a prompt** in `assets/prompts/` and compare Stage 6's answer; the validator still checks every citation.
- **Answer with another model**: add `"model": "ollama/<another model>"` to `options` and compare the Stage 6 answers on the same evidence.

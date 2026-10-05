# How to Find a Needle

The supporting repo for the developer talk **"How to Find a Needle"** — how modern search systems work, and how to build them in practice.

## The talk

Search is often treated as a single problem with a single solution ("just add a vector database"). In practice, real systems need a *pipeline* of complementary techniques, each solving a different failure mode of the last. This repo builds that pipeline, one dataset, one step at a time.

### The pipeline

1. **Structured search** — databases, filtering, normalised data, and why a simple `WHERE` clause is sometimes the best solution.
2. **Keyword search** — lexical relevance with Postgres full-text search, ranked "BM25-style" (and why that isn't quite BM25).
3. **Semantic search** — embeddings and vector search, using local models such as [Nomic](https://www.nomic.ai/).
4. **Hybrid search** — combining keyword and vector retrieval with Reciprocal Rank Fusion (RRF).
5. **Ontology** — a SKOS taxonomy, synonyms, language-tagged labels and value vocabularies in RDF/Turtle, plus class-level compatibility rules (for example, a charger's plug must fit the laptop's port).
6. **LLM + RAG** — giving an LLM the retrieved evidence *and* ontology facts so it can answer questions and make recommendations, with citations that are checked.
7. **Pedagogy** — turning that grounded answer into an explanation for a novice, enthusiast or expert, compared with a plain prompt to show what teaching design adds.

### The central idea

| Layer | Question it answers |
|---|---|
| Search | What is relevant? |
| Ontology | How is it related? |
| Pedagogy | How should I explain it? |

A recurring example throughout the talk: **similarity ≠ compatibility**. A vector search might return both a 65W USB-C charger and a 45W proprietary barrel-connector charger because they read as semantically similar ("black rectangular power adapter") — but an ontology can explicitly represent which connector type and wattage each device actually needs. Search finds candidates; ontology constrains and explains them.

### Thesis

> AI does not replace good search, data structures, or information architecture — it makes them more important.

The talk is deliberately practical and experimental: the same dataset is used throughout, and each technique is measured against the same queries so the audience can see how results *change* and *improve* at each stage, rather than watching disconnected demos.

## What's in this repo

- **.NET / C# API** ([src/PI.SearchApi](src/PI.SearchApi)) — FastEndpoints-based API exposing the search pipeline, one endpoint/feature per technique as the talk progresses.
- **PostgreSQL + pgvector** for storage, structured querying, and vector similarity search.
- **Local embeddings** — [Nomic](https://www.nomic.ai/) Embed Text v1.5 runs via ONNX Runtime, no external API calls required.
- **RDF/Turtle ontology** — a SKOS taxonomy of product categories, multilingual synonyms and class-level compatibility rules (e.g. "a laptop charger's connector must match the laptop's charging port"). It describes product *types*, never individual products.
- **.NET Aspire AppHost** ([src/PI.AppHost](src/PI.AppHost)) to orchestrate the API, database, and dependencies locally.
- **An LLM for Stages 6–7** through `Microsoft.Extensions.AI`'s `IChatClient`: a local [Ollama](https://ollama.com/) by default, or bring your own model (OpenAI, Claude, Azure OpenAI, Gemini or any OpenAI-compatible server) and switch between them in the UI. Answers stream in, cite the products they use, and are checked when complete.
- **A React web UI** ([src/web-ui](src/web-ui)) that is the talk itself (a slide deck and a demo that follows it, no external slides): the audience sees the same query go through simple filtering → BM25-style keyword → vector → hybrid → ontology → RAG → pedagogy, with the trace behind every result.

## How to learn from this repo

The code is written to be read. Every pipeline service opens with a header saying **what** the technique does, its **strength**, its **failure mode** and the **decision** record behind it, and the inline comments explain the search, ML and ontology ideas at the line where they happen. The comments assume you know C#, TypeScript and web APIs, but not search.

**A suggested path:**

1. **Run it** (see [Getting started](#getting-started)) and open the demo (`/demo`) in the web UI.
2. **Load a golden query** from the picker in the search bar — start with *GQ-03 Similarity is not compatibility* — and step through Stages 1–7 with the stepper. The query, device and filters stay the same; only the technique changes.
3. On each stage, read **How it works**, compare the **Results**, then open **Under the hood** to see the exact SQL, parameters, scores and rule checks that produced them.
4. **Read the code for that stage** (table below), then its **decision record**, which explains why it was built that way and what was rejected.
5. **Change something and watch the trace**: a synonym in the ontology, a product's wording, the RRF weights, a prompt, the model. The tests tell you if you broke a talk moment.

| Stage | Try | Read the code | Decision |
|---|---|---|---|
| 1 Structured | GQ-01 | [`StructuredSearch.cs`](src/PI.SearchApi/Pipeline/Structured/StructuredSearch.cs), [`SqlFilterBuilder.cs`](src/PI.SearchApi/Pipeline/SqlFilterBuilder.cs) | [ADR-0007](docs/decisions/0007-structured-search.md) |
| 2 Keyword | GQ-04, GQ-02 | [`KeywordSearch.cs`](src/PI.SearchApi/Pipeline/Keyword/KeywordSearch.cs), [`TsQueryBuilder.cs`](src/PI.SearchApi/Pipeline/Keyword/TsQueryBuilder.cs) | [ADR-0008](docs/decisions/0008-keyword-search-bm25-style.md) |
| 3 Vector | GQ-02, GQ-08 | [`VectorSearch.cs`](src/PI.SearchApi/Pipeline/Vector/VectorSearch.cs), [`Embeddings/`](src/PI.SearchApi/Embeddings) | [ADR-0009](docs/decisions/0009-local-embeddings-onnx-runtime.md), [ADR-0010](docs/decisions/0010-vector-search-pgvector.md) |
| 4 Hybrid | GQ-04 | [`HybridSearch.cs`](src/PI.SearchApi/Pipeline/Hybrid/HybridSearch.cs), [`ReciprocalRankFusion.cs`](src/PI.SearchApi/Pipeline/Fusion/ReciprocalRankFusion.cs) | [ADR-0011](docs/decisions/0011-hybrid-search-rrf.md) |
| 5 Ontology | GQ-03, GQ-05, GQ-06, GQ-07, GQ-09 | [`OntologySearch.cs`](src/PI.SearchApi/Pipeline/Ontology/OntologySearch.cs), [`CompatibilityEvaluator.cs`](src/PI.SearchApi/Pipeline/Ontology/CompatibilityEvaluator.cs), [`domain-ontology.ttl`](src/PI.SearchApi/assets/data/domain-ontology.ttl) | [ADR-0013](docs/decisions/0013-domain-ontology-and-compatibility.md) |
| 6 RAG | GQ-03 | [`EvidenceSetBuilder.cs`](src/PI.SearchApi/Pipeline/Rag/EvidenceSetBuilder.cs), [`AnswerGenerator.cs`](src/PI.SearchApi/Pipeline/Rag/AnswerGenerator.cs), [`rag-system.md`](src/PI.SearchApi/assets/prompts/rag-system.md) | [ADR-0016](docs/decisions/0016-rag-grounding-and-citations.md) |
| 7 Pedagogy | GQ-03 (flip *Apply pedagogy*) | [`PedagogyEngine.cs`](src/PI.SearchApi/Pipeline/Pedagogy/PedagogyEngine.cs), [`pedagogy-system.md`](src/PI.SearchApi/assets/prompts/pedagogy-system.md) | [ADR-0017](docs/decisions/0017-pedagogy-engine.md) |

Cross-cutting decisions worth reading early: [ADR-0003](docs/decisions/0003-search-api-contract-and-debug-trace.md) (one request and response for every stage, and the trace), [ADR-0004](docs/decisions/0004-pipeline-composition.md) (how later stages reuse earlier ones), [ADR-0005](docs/decisions/0005-curated-dataset-and-golden-queries.md) (the dataset and golden queries) and [ADR-0019](docs/decisions/0019-bring-your-own-model.md) (choosing the LLM per request). The web UI renders all of them on its **Decisions** page, and the **Glossary** page explains every term.

### Follow one request

1. The browser sends `POST /api/search/hybrid` (the same body for every stage) to the Vite dev server, which proxies `/api` to the API — [`vite.config.ts`](src/web-ui/vite.config.ts).
2. A thin endpoint validates the request and calls one pipeline service — [`Endpoints/Search/`](src/PI.SearchApi/Endpoints/Search).
3. The service runs its technique, often by calling earlier stages (hybrid calls keyword and vector, then fuses them), and appends a **trace step** with the exact SQL and its parameters — [`Pipeline/`](src/PI.SearchApi/Pipeline).
4. The endpoint pages the candidates once and returns results plus `debugTrace`. The UI's **Under the hood** tab renders each trace step with a purpose-built view — [`components/trace/`](src/web-ui/src/components/trace).
5. On Stages 6–7 the UI sends a second request with the same body to `/answer`, which streams the LLM's text as Server-Sent Events, so results never wait for the model.

### Repository map

| Path | What it holds | Guide |
|---|---|---|
| [`src/PI.AppHost`](src/PI.AppHost) | .NET Aspire: starts Postgres + pgvector, the API and the web UI with one command | — |
| [`src/PI.SearchApi`](src/PI.SearchApi) | The search pipeline: endpoints, stage services, embeddings, LLM providers, data seeding | [README](src/PI.SearchApi/README.md) |
| [`src/PI.SearchApi/assets`](src/PI.SearchApi/assets) | The catalogue, ontology, SPARQL queries, golden queries, committed embeddings and LLM prompts | [models README](src/PI.SearchApi/assets/models/README.md) |
| [`src/web-ui`](src/web-ui) | React UI: home, slide deck, demo, glossary and decisions; talk content in `content/` | [README](src/web-ui/README.md) |
| [`tests`](tests) | Unit tests, golden-query integration tests and the opt-in model bake-off | [README](tests/README.md) |
| [`tools/PI.CatalogGenerator`](tools/PI.CatalogGenerator) | Generates the 240 "distractor" products that make the catalogue realistic | — |
| [`docs/decisions`](docs/decisions) | The decision records (ADRs): why each part is built the way it is | [README](docs/decisions/README.md) |
| [`docs/design`](docs/design) | The UI's visual design reference | [README](docs/design/README.md) |

## Dataset

The demo uses a single hand-curated, synthetic electronics catalog throughout — 300 products under fictional brands: 60 hand-written, the rest generated by `tools/PI.CatalogGenerator` (*Blackbird* laptops, *Voltline* chargers, *Kestrel* SSDs and memory, *Brakk* and *Tornio* power tools) — so every search technique can be compared on the same data and queries. Products are written to create specific moments: near-miss chargers that look right but aren't, a cordless phone battery that fools keyword search, chargers a shopper would call a "power brick". See [src/PI.SearchApi/assets/data](src/PI.SearchApi/assets/data): `products.json` (the catalog), `domain-ontology.ttl` (the taxonomy, synonyms and compatibility rules — it never names a product) and `golden-queries.json` (the talk moments, used as both integration tests and UI presets).

## Getting started

**Get the code**

```sh
git clone https://github.com/petecleary/how-to-find-a-needle.git
cd how-to-find-a-needle
```

**Prerequisites**
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/) (for the Postgres + pgvector container)
- [.NET Aspire CLI](https://learn.microsoft.com/dotnet/aspire/fundamentals/setup-tooling)
- [Node.js](https://nodejs.org/) 24 LTS, for the web UI (the version is pinned in `src/web-ui/.nvmrc`)
- [Hugging Face CLI](https://huggingface.co/docs/huggingface_hub/guides/cli), to download the local embedding model
- For Stages 6–7: [Ollama](https://ollama.com/), or an API key for a hosted provider

**Download the embedding model**

Vector, hybrid and ontology search (Stages 3, 4 and 5) embed queries with Nomic Embed Text v1.5, running locally on ONNX Runtime. The model files are large, so they aren't committed. Follow [src/PI.SearchApi/assets/models/README.md](src/PI.SearchApi/assets/models/README.md) (section 1, Nomic) to download `model_int8.onnx` and `tokenizer.json`.

Without the model, structured and keyword search (Stages 1–2) still work, and the other stages return `503 Service Unavailable` with the same instructions.

**Set up the LLM (Stages 6–7)**

RAG and pedagogy (Stages 6 and 7) need an LLM. The **default model** is the `Llm` section of [src/PI.SearchApi/appsettings.json](src/PI.SearchApi/appsettings.json), a local Ollama:

```sh
ollama pull qwen3.6:35b
```

A 35B model needs plenty of memory (the presenter's laptop has 64 GB). On lighter hardware, pull a smaller model and set `Llm:Model` to its name; smaller models follow the prompts less reliably, and the Answer tab shows the resulting warnings.

**Bring your own model.** Stages 6 and 7 have a **Model** picker, and the key button beside it (also in the header) opens **Models and API keys**: turn providers on, set a base URL, paste a key and test the connection — no restart needed ([ADR-0019](docs/decisions/0019-bring-your-own-model.md)). The chosen model goes in the URL and the request as `options.model` (e.g. `anthropic/claude-sonnet-5`); the key never does.

| Provider | Key |
|---|---|
| Ollama (default) | none |
| OpenAI | `OPENAI_API_KEY` |
| Anthropic (Claude) | `ANTHROPIC_API_KEY` |
| Google Gemini | `GEMINI_API_KEY` |
| Azure OpenAI | `AZURE_OPENAI_API_KEY`, plus your resource's base URL and deployment names |
| OpenAI-compatible (LM Studio, vLLM, OpenRouter) | `OPENAI_COMPAT_API_KEY` (optional), plus the server's base URL |

A key pasted in the UI is held by the API until it stops and is never saved. To keep one, use user secrets (never a file; hosted calls cost money):

```sh
dotnet user-secrets set "ANTHROPIC_API_KEY" "<your key>" --project src/PI.SearchApi
```

Base URLs and enabled providers are saved in `~/.needle/settings.json` (set `NEEDLE_HOME` to move it). Only Ollama has been run live; the hosted providers are wired up and unit-tested. Without an LLM, Stages 6–7 still return their results, the Answer tab shows a `503` saying what to fix, and Stages 1–5 are unaffected.

**Run everything**

```sh
aspire run
```

(or press F5 in VS Code / Visual Studio on the "Aspire: Launch default AppHost" configuration). This starts Postgres, the API and the web UI, and installs the web UI's npm packages on first run; open the `web-ui` resource's URL from the Aspire dashboard to see the talk and the demo. On first run, watch the `searchapi` resource's logs in the Aspire dashboard for:

```
Seeded 300 products (300 inserted, 0 updated, 0 deleted)
Embeddings (nomic): 300 loaded from file, 0 embedded live, 0 missing (0 already current)
```

Product vectors come from the committed `assets/data/embeddings/nomic.jsonl`, so the first run takes seconds, not minutes. A second run logs `0 inserted, 0 updated, 0 deleted` and `(300 already current)`, and starts noticeably faster.

**Present the talk**

Open `/slides` (or **Present the slides** on Home) for the presenter's deck: ← and → move one slide, and **D** opens the demo in a second window. Put that window on the other screen: it follows the deck, loading each slide's stage and golden query, so you can switch to it when a slide says "watch this". Reading along on your own? Open the deck and the demo side by side: every slide's demo is an ordinary `/demo` URL you can bookmark.

**Try the search stages**

Open **Search API (Scalar)** from the `searchapi` resource in the dashboard. Every stage takes the same request and returns the same response, including a `debugTrace` that shows the SQL, parsed queries, distances, RRF formulas and rule checks behind the results:

| Stage | Endpoint |
|---|---|
| 1 Structured | `POST /api/search/structured` |
| 2 Keyword | `POST /api/search/keyword` |
| 3 Vector | `POST /api/search/vector` |
| 4 Hybrid | `POST /api/search/hybrid` |
| 5 Ontology | `POST /api/search/ontology` |
| 6 RAG | `POST /api/search/rag`, and `POST /api/search/rag/answer` for the streamed answer |
| 7 Pedagogy | `POST /api/search/pedagogy`, and `POST /api/search/pedagogy/answer` for the answer and its explanation |

The answer endpoints stream Server-Sent Events; send `Accept: application/json` to get the whole answer as one JSON object instead. Try the talk's opening example on each stage:

```json
{ "query": "power adapter for my laptop", "context": { "targetProductId": "PROD-0001" } }
```

`GET /api/demo/queries` lists every golden query, `GET /api/demo/devices` lists the products that can be a target device, `GET /api/taxonomy` returns the category tree from the ontology, `GET /api/vocabularies` returns the allowed spec values (connectors, storage interfaces, memory types, battery platforms) with their synonyms, and `GET /api/brands` lists the catalogue's brands.

**Run the tests**

```sh
dotnet test tests/PI.SearchApi.Tests              # fast, no Docker
dotnet test tests/PI.SearchApi.IntegrationTests    # needs Docker running

cd src/web-ui
npm ci
npm run format:check && npm run typecheck && npm run lint && npm test && npm run build
```

These are the same checks CI runs ([.github/workflows/ci.yml](.github/workflows/ci.yml)), except the integration tests, which need Docker and the models. More detail on what each layer tests is in [tests/README.md](tests/README.md).

The integration tests start the whole AppHost and run the golden queries against each stage. Vector, hybrid and ontology tests skip, with a message, if the Nomic model isn't downloaded; the Stage 6–7 tests skip if the LLM isn't available. They check answers structurally (what was cited, whether the explanation's structure holds), never their wording.

The model bake-off, which compares local models for Stages 6–7, is opt-in because it runs for a long time. It writes a report to `tests/PI.SearchApi.IntegrationTests/TestResults/`:

```sh
PI_BAKEOFF_MODELS=qwen3.6:35b,gemma4:31b dotnet test tests/PI.SearchApi.IntegrationTests --filter ModelBakeOff
```

**After editing products.json**

A changed product's vector no longer matches its text, so the seeder embeds it live and warns. To regenerate the committed file, run once with a rebuild, then commit `nomic.jsonl`:

```sh
Embeddings__Rebuild=true dotnet run --project src/PI.AppHost
```

**After editing the ontology**

The API loads `assets/data/domain-ontology.ttl` once, at startup. After adding a category, synonym, spec value or rule, stop the AppHost and run `aspire run` again: the build copies the edited file, and `/api/taxonomy` and `/api/vocabularies` return the change. The unit tests (`dotnet test tests/PI.SearchApi.Tests`) check that products still use known categories and values.

**Reset the data**

Stop the AppHost, then:

```sh
docker volume rm pgvector-data-search
```

The next `aspire run` rebuilds the schema and re-seeds the catalog from scratch.

## Troubleshooting

| Symptom | Fix |
|---|---|
| `aspire run` fails to find Postgres, or the container never becomes healthy | Start Docker Desktop (or your Docker daemon) first; Aspire orchestrates containers, it doesn't start Docker itself. |
| `aspire: command not found` | Install the [.NET Aspire CLI](https://learn.microsoft.com/dotnet/aspire/fundamentals/setup-tooling); it's separate from the .NET SDK. |
| Stages 3–7 return `503 Service Unavailable` | The response's `detail` names the missing piece (usually the Nomic model or the LLM) and how to fix it. Stages 1–2 are unaffected either way. |
| Stage 6 or 7's Answer tab shows a `503` | Ollama isn't running, or the model in `Llm:Model` hasn't been pulled. Run `ollama serve` and `ollama pull <model>`. For a hosted model, open **Models and API keys** and use **Test connection**: it says what is missing. |
| The first Stage 6/7 answer is slow, then later ones are fast | A cold local model has to load into memory. Send one throwaway query to warm it up before you go on stage, or set a longer `keep_alive` in Ollama. |
| `npm run gen:api` fails or returns an empty schema | It reads the API from `http://localhost:5377`, so `aspire run` must already be running. Node doesn't trust the ASP.NET Core development certificate, which is also why the Vite proxy sets `secure: false`. |
| A product edit doesn't show up in search | Editing `products.json` only takes effect on the next `aspire run`: the seeder diffs by `content_hash` and nulls that product's embeddings, which are then re-embedded live (and logged) on that run. |
| An ontology edit (a new synonym, category or rule) doesn't show up | The API loads `domain-ontology.ttl` once, at startup. Restart `aspire run`. |
| A golden query doesn't produce its moment (e.g. the wrong product ranks first) | Change the product **wording** in `products.json`, not the algorithm — see [ADR-0005](docs/decisions/0005-curated-dataset-and-golden-queries.md). Re-run with `Embeddings__Rebuild=true` if you touched embedded text. |
| `npm test` or `dotnet test` fails only in CI, not locally | Check the Node version (`.nvmrc`) and that `schema.d.ts` is committed and current — CI never runs the API, Docker or the models. |

## Status

All seven stages (structured → keyword → vector → hybrid → ontology → RAG → pedagogy) are built and rehearsed, in the API and the web UI, with a local Ollama model. Hosted chat models (OpenAI, Anthropic, Azure OpenAI, Gemini, OpenAI-compatible) can be chosen per request ([ADR-0019](docs/decisions/0019-bring-your-own-model.md)); they are built and unit-tested, and the OpenAI-compatible path was checked against Ollama's `/v1`, but none has been run live against a hosted API. OpenAI hosted embeddings aren't built. See [docs/adr/roadmap.md](docs/adr/roadmap.md) for the build history.

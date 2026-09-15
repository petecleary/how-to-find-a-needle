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
- A fictional frontend (to be added) so the audience can see the pipeline progress from simple filtering → BM25-style keyword → vector → hybrid → ontology → RAG → pedagogy.

## Dataset

The demo uses a single hand-curated, synthetic electronics catalog throughout — around 60 products under fictional brands (*Blackbird* laptops, *Voltline* chargers, *Kestrel* SSDs and memory, *Brakk* and *Tornio* power tools) — so every search technique can be compared on the same data and queries. Products are written to create specific moments: near-miss chargers that look right but aren't, a cordless phone battery that fools keyword search, chargers a shopper would call a "power brick". See [src/PI.SearchApi/assets/data](src/PI.SearchApi/assets/data): `products.json` (the catalog), `domain-ontology.ttl` (the taxonomy, synonyms and compatibility rules — it never names a product) and `golden-queries.json` (the talk moments, used as both integration tests and UI presets).

## Getting started

**Prerequisites**
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/) (for the Postgres + pgvector container)
- [.NET Aspire CLI](https://learn.microsoft.com/dotnet/aspire/fundamentals/setup-tooling)
- [Hugging Face CLI](https://huggingface.co/docs/huggingface_hub/guides/cli), to download the local embedding model

**Download the embedding model**

Vector, hybrid and ontology search (Stages 3, 4 and 5) embed queries with Nomic Embed Text v1.5, running locally on ONNX Runtime. The model files are large, so they aren't committed. Follow [src/PI.SearchApi/assets/models/README.md](src/PI.SearchApi/assets/models/README.md) (section 1, Nomic) to download `model_int8.onnx` and `tokenizer.json`.

Without the model, structured and keyword search (Stages 1–2) still work, and the other stages return `503 Service Unavailable` with the same instructions.

**Run everything**

```sh
aspire run
```

(or press F5 in VS Code / Visual Studio on the "Aspire: Launch default AppHost" configuration). This starts Postgres and the API. On first run, watch the `searchapi` resource's logs in the Aspire dashboard for:

```
Seeded 60 products (60 inserted, 0 updated, 0 deleted)
Embeddings (nomic): 60 loaded from file, 0 embedded live, 0 missing (0 already current)
```

Product vectors come from the committed `assets/data/embeddings/nomic.jsonl`, so the first run takes seconds, not minutes. A second run logs `0 inserted, 0 updated, 0 deleted` and `(60 already current)`, and starts noticeably faster.

**Try the search stages**

Open **Search API (Scalar)** from the `searchapi` resource in the dashboard. Every stage takes the same request and returns the same response, including a `debugTrace` that shows the SQL, parsed queries, distances, RRF formulas and rule checks behind the results:

| Stage | Endpoint |
|---|---|
| 1 Structured | `POST /api/search/structured` |
| 2 Keyword | `POST /api/search/keyword` |
| 3 Vector | `POST /api/search/vector` |
| 4 Hybrid | `POST /api/search/hybrid` |
| 5 Ontology | `POST /api/search/ontology` |

Try the talk's opening example on each stage:

```json
{ "query": "power adapter for my laptop", "context": { "targetProductId": "PROD-0001" } }
```

`GET /api/demo/queries` lists every golden query, `GET /api/demo/devices` lists the products that can be a target device, `GET /api/taxonomy` returns the category tree from the ontology, `GET /api/vocabularies` returns the allowed spec values (connectors, storage interfaces, memory types, battery platforms) with their synonyms, and `GET /api/brands` lists the catalogue's brands.

**Run the tests**

```sh
dotnet test tests/PI.SearchApi.Tests              # fast, no Docker
dotnet test tests/PI.SearchApi.IntegrationTests    # needs Docker running
```

The integration tests run the golden queries against each stage. Vector, hybrid and ontology tests skip, with a message, if the Nomic model isn't downloaded.

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

## Status

This repo is built incrementally alongside the talk, following the phases in [docs/adr/roadmap.md](docs/adr/roadmap.md). Expect each stage (structured → keyword → semantic → hybrid → ontology → LLM/RAG → pedagogy) to land as a corresponding feature/endpoint in [src/PI.SearchApi](src/PI.SearchApi).

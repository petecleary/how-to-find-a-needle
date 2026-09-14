# How to Find a Needle

The supporting repo for the developer talk **"How to Find a Needle"** — how modern search systems work, and how to build them in practice.

## The talk

Search is often treated as a single problem with a single solution ("just add a vector database"). In practice, real systems need a *pipeline* of complementary techniques, each solving a different failure mode of the last. This repo builds that pipeline, one dataset, one step at a time.

### The pipeline

1. **Structured search** — databases, filtering, normalized data, and why a simple `WHERE` clause is sometimes the best solution.
2. **Keyword search** — BM25 and lexical relevance.
3. **Semantic search** — embeddings and vector search, using local models such as [Nomic](https://www.nomic.ai/).
4. **Hybrid search** — combining sparse and dense retrieval with Reciprocal Rank Fusion (RRF).
5. **BGE-M3** — multilingual dense/sparse retrieval and cross-language search.
6. **Ontology / knowledge graphs** — using RDF/Turtle to represent explicit concepts and relationships such as `compatibleWith`, `requires`, `partOf`.
7. **LLM + RAG** — giving an LLM the retrieved evidence *and* ontology facts so it can answer questions and make recommendations.
8. **Pedagogy** — turning retrieved information and domain knowledge into an explanation that's actually useful to the user.

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
- **Local embeddings** — [Nomic](https://www.nomic.ai/) (dense) and [BGE-M3](https://huggingface.co/BAAI/bge-m3) (multilingual dense + sparse) run via ONNX Runtime, no external API calls required.
- **RDF/Turtle ontology** — a SKOS taxonomy of product categories, multilingual synonyms and class-level compatibility rules (e.g. "a laptop charger's connector must match the laptop's charging port"). It describes product *types*, never individual products.
- **.NET Aspire AppHost** ([src/PI.AppHost](src/PI.AppHost)) to orchestrate the API, database, and dependencies locally.
- A fictional frontend (to be added) so the audience can see the pipeline progress from simple filtering → BM25 → vector → hybrid → ontology → LLM/RAG.

## Dataset

The demo uses a single hand-curated, synthetic electronics catalog throughout — around 60 products under fictional brands (*Blackbird* laptops, *Voltline* chargers, *Kestrel* SSDs and memory, *Brakk* and *Tornio* power tools) — so every search technique can be compared on the same data and queries. Products are written to create specific moments: near-miss chargers that look right but aren't, a cordless phone battery that fools keyword search, chargers a shopper would call a "power brick". See [src/PI.SearchApi/assets/data](src/PI.SearchApi/assets/data): `products.json` (the catalog), `domain-ontology.ttl` (the taxonomy, synonyms and compatibility rules — it never names a product) and `golden-queries.json` (the talk moments, used as both integration tests and UI presets).

## Getting started

**Prerequisites**
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/) (for the Postgres + pgvector container)
- [.NET Aspire CLI](https://learn.microsoft.com/dotnet/aspire/fundamentals/setup-tooling)

**Run everything**

```sh
aspire run
```

(or press F5 in VS Code / Visual Studio on the "Aspire: Launch default AppHost" configuration). This starts Postgres and the API. On first run, watch the `searchapi` resource's logs in the Aspire dashboard for:

```
Seeded 60 products (60 inserted, 0 updated, 0 deleted)
```

A second run logs `0 inserted, 0 updated, 0 deleted` and starts noticeably faster — restarts don't re-seed products that haven't changed.

**Run the tests**

```sh
dotnet test tests/PI.SearchApi.Tests              # fast, no Docker
dotnet test tests/PI.SearchApi.IntegrationTests    # needs Docker running
```

**Reset the data**

Stop the AppHost, then:

```sh
docker volume rm pgvector-data-search
```

The next `aspire run` rebuilds the schema and re-seeds the catalog from scratch.

## Status

This repo is built incrementally alongside the talk, following the phases in [docs/adr/roadmap.md](docs/adr/roadmap.md). Expect each stage (structured → keyword → semantic → hybrid → BGE-M3 → ontology → LLM/RAG → pedagogy) to land as a corresponding feature/endpoint in [src/PI.SearchApi](src/PI.SearchApi).

# How to Find a Needle — Project & Architecture Overview

System context, backend architecture, search pipeline stages, API conventions and frontend specification for the 30-minute developer talk **"How to Find a Needle"**.

> **Working document (build branch).** Decisions behind each part are recorded in the ADRs (see [README.md](README.md)); build order is in [roadmap.md](roadmap.md). If this overview and an ADR disagree, the ADR wins, so update this file.

---

## 1. Core Thesis & Talk Paradigm

- **Main thesis:** *"AI does not replace good search, data structures, or information architecture — it makes them more important."*
- **The core hook ("Similarity ≠ Compatibility"):**
  A vector search model may rate a *65W USB-C charger* and a *45W barrel-connector charger* as highly similar (cosine similarity ≈ 0.89), because both are "black rectangular power adapters for laptops". An explicit **domain ontology** checks connector type and wattage against the user's laptop and flags the 45W charger as **incompatible, with a reason** (`USB-C` ≠ `5.5mm barrel`, `45W` < `65W required`). The **pedagogy engine** then has the LLM explain *why* it doesn't fit and recommend the right charger. A baseline prompt, with the same facts and audience, shows what the pedagogical design adds.

### The triad

1. **Search** (structured / keyword / vector / hybrid): *What is relevant?*
2. **Ontology** (SKOS taxonomy, synonyms and value vocabularies, plus a small class-level rule vocabulary; RDF / Turtle / SPARQL): *How is it related and constrained?*
3. **Pedagogy** (RAG / audience-aware prompt design): *How should I explain it to the user?*

### Scope

Seven stages are built. Topics the talk discusses but doesn't build (chunking, BGE-M3 and learned sparse retrieval, re-ranking, query rewriting, OWL / SHACL / knowledge graphs, RAG evaluation) are gathered in one "Going further" talk step. Agent protocols are out of scope. → [ADR-0018](0018-scope-and-going-further.md)

---

## 2. Dataset Strategy

The demo uses a **hand-curated, synthetic electronics catalog**, `products.json`. It starts at ~60 items and later grows to ~500 by adding generated distractors around the curated core. It uses **fictional brands** and is built so that each stage produces a **visibly different** result for the same query. → [ADR-0005](0005-curated-dataset-and-golden-queries.md)

Items are authored to create:
- **Near-miss semantic matches:** similar in vector space, wrong in reality (similarity ≠ compatibility).
- **Keyword traps:** shared words, unrelated products (e.g. *cordless phone battery* vs *cordless drill battery*).
- **Synonym gaps:** what the user says ("power brick") differs from what the catalog says ("adapter").
- **Domain constraints:** connector and wattage, voltage platform, SSD interface, memory type. Products carry the spec values; the ontology defines each rule once per pair of product types, and never names a product.
- **Multiple categories per product**, each a concept in the ontology's SKOS taxonomy.
- **Rich descriptive text:** descriptions and short reviews, so embeddings differ meaningfully from token matching.

**Golden queries** (`golden-queries.json`) record the expected per-stage outcome for each talk moment. They are used as integration tests, UI presets and the talk-mode stage steps.

The Datafiniti/Kaggle CSV has been **dropped**.

---

## 3. Solution Layout & Technology Stack

```text
how-to-find-a-needle.slnx
Directory.Build.props / Directory.Packages.props / .editorconfig
README.md
docs/adr/                         # working ADRs, roadmap, this overview (build branch; public ADRs after the build)

src/
  PI.AppHost/                     # .NET Aspire: postgres (+pgvector), searchapi, web-ui, LLM settings
    AppHost.cs

  PI.SearchApi/                   # Web API (FastEndpoints, REPR)
    Program.cs                    # composition root: DI per stage, seeding before app.Run()
    Extensions.cs                 # Aspire service defaults (OpenTelemetry, health checks)
    Contracts/                    # SearchRequest, SearchResponse, ProductResult, DebugTrace
    Data/                         # DatabaseSeeder (schema, catalog upsert, embedding backfill), EmbeddingFile (jsonl)
    Embeddings/                   # ISearchEmbedder, NomicOnnxEmbeddingGenerator (ONNX Runtime)
    Pipeline/                     # shared types (Candidate, StageResult, TraceStep, SqlFilterBuilder) + one technique per folder
      Structured/  Keyword/  Vector/  Fusion/  Hybrid/  Ontology/  Rag/  Pedagogy/
    Endpoints/
      Search/{Stage}/             # thin endpoint + validator per stage
      Demo/                       # golden queries, device list, taxonomy, value vocabularies
    assets/
      data/
        products.json             # curated catalog: names, prices, descriptions, specs
        golden-queries.json       # talk moments + per-stage expectations
        domain-ontology.ttl       # SKOS taxonomy, synonyms, value vocabularies + class-level rules (no product ids)
        queries/*.rq              # SPARQL lookups: labels, taxonomy, vocabularies, narrower concepts, rules
        embeddings/               # nomic.jsonl, openai.jsonl: committed product vectors
        init.sql                  # idempotent schema + indexes
      prompts/                    # rag-*.md, pedagogy-system.md, pedagogy-baseline.md
      models/                     # downloaded ONNX models (gitignored; README committed)
        nomic/                    # model_int8.onnx, tokenizer.json

  web-ui/                         # React + Vite + TS + Tailwind + shadcn/ui; the talk itself (no slides)
    content/                      # speaker.md, talk.json + talk/*.md, stages/*.md, glossary.json
    src/
      api/                        # schema.d.ts (openapi-typescript), client.ts
      components/                 # SearchBar, PipelineStepper, StageTabs, ResultRow, trace renderers
      hooks/usePipelineSearch.ts
      App.tsx

tests/
  PI.SearchApi.Tests/             # unit: RRF, pooling, rules, validators
  PI.SearchApi.IntegrationTests/  # Aspire.Hosting.Testing: golden queries per stage
```

### Technology stack

| Concern | Choice | ADR |
|---|---|---|
| Backend | .NET 10 / ASP.NET Core, FastEndpoints (REPR), FluentValidation | [0002](0002-solution-structure-and-orchestration.md) |
| Orchestration | .NET Aspire (Postgres, API, Vite app; OpenTelemetry dashboard) | [0002](0002-solution-structure-and-orchestration.md) |
| Database | PostgreSQL + pgvector (`vector`, HNSW) + built-in full-text search, via `Aspire.Npgsql` + `Pgvector`, raw SQL | [0006](0006-database-schema-and-seeding.md) |
| Keyword ranking | Postgres FTS (`websearch_to_tsquery`, `ts_rank_cd`), labelled **BM25-style** | [0008](0008-keyword-search-bm25-style.md) |
| Embeddings | `IEmbeddingGenerator` with a configured provider: local Nomic Embed v1.5 via ONNX Runtime (default, offline) or OpenAI `text-embedding-3-small` at 768d (tested later). Product vectors committed per provider in `assets/data/embeddings/*.jsonl`, regenerated with `Embeddings:Rebuild` | [0009](0009-local-embeddings-onnx-runtime.md) |
| Fusion | Reciprocal Rank Fusion (k = 60), pure C# | [0011](0011-hybrid-search-rrf.md) |
| Ontology | dotNetRDF (in-memory), hand-written Turtle: standard SKOS (taxonomy, synonyms, language-tagged labels, value vocabularies) plus a small class-level rule vocabulary beyond SKOS; SPARQL lookups; no instance data | [0013](0013-domain-ontology-and-compatibility.md) |
| LLM (stages 6–7) | `Microsoft.Extensions.AI` `IChatClient`, provider set in config: existing local **Ollama** (OpenAI-compatible `/v1`), **OpenAI**, or **Anthropic** (official `Anthropic` .NET SDK). No containers, **no LiteLLM** | [0015](0015-llm-hosting-and-client.md) |
| Frontend | React + Vite + TypeScript, Tailwind, shadcn/ui, Lucide, React Router, react-markdown; `openapi-typescript` types; Vite proxy (no CORS). Home, talk mode, demo, glossary and ADR pages **replace slides** | [0014](0014-web-ui-architecture.md) |
| Testing | xUnit unit tests + `Aspire.Hosting.Testing` golden-query integration tests; Vitest for the UI hook | [0002](0002-solution-structure-and-orchestration.md) |
| API docs | Scalar + `Microsoft.AspNetCore.OpenApi` | — |

---

## 4. API Conventions & Pipeline Stages

All search stages use **POST** with a shared JSON request and response, so the UI can switch stages with the same query. Stages 6–7 add a second request, sent at the same time: `POST /api/search/{rag|pedagogy}/answer` streams the LLM's markdown summary as Server-Sent Events, shown above the results like an AI overview. This replaces the legacy `GET /api/products`. Supporting read endpoints for the UI are `GET /api/demo/queries`, `GET /api/demo/devices`, `GET /api/taxonomy` (the category tree) and `GET /api/vocabularies` (the allowed spec values). The last two are read from the ontology, so the UI's filters are data. → [ADR-0003](0003-search-api-contract-and-debug-trace.md)

**Request:** `POST /api/search/{stage}`

```json
{
  "query": "charger for my Blackbird Aerobook 14",
  "page": 1,
  "pageSize": 10,
  "filters": { "brand": "Voltline", "categories": ["laptop-chargers"], "minPrice": 20, "maxPrice": 150, "specs": { "connector": "usb-c" } },
  "context": { "targetProductId": "PROD-0001" },
  "options": { "candidateDepth": 50, "rrfK": 60, "keywordWeight": 1.0, "vectorWeight": 1.0, "expandSynonyms": true, "applyConstraints": true, "audience": "novice", "applyPedagogy": true }
}
```

**Response**

```json
{
  "stage": "ontology",
  "query": "charger for my Blackbird Aerobook 14",
  "page": 1, "pageSize": 10, "totalResults": 37, "executionTimeMs": 21.4,
  "results": [
    {
      "id": "PROD-0014", "name": "Voltline 45W Barrel Laptop Adapter", "brand": "Voltline",
      "categories": ["laptop-chargers"], "price": 29.99, "specs": { "connector": "barrel-5.5mm", "wattageW": 45 },
      "score": 0.0318,
      "signals": { "keywordRank": 3, "vectorRank": 2, "vectorDistance": 0.11, "fusedRank": 2, "conceptMatch": "InConcept" },
      "compatibility": {
        "status": "Incompatible",
        "reasons": ["Connector mismatch: device requires USB-C, charger provides 5.5mm barrel", "Insufficient power: 45W < 65W required"]
      }
    }
  ],
  "debugTrace": {
    "steps": [
      { "stage": "keyword", "title": "Postgres full-text search (BM25-style)", "durationMs": 3.1, "sql": "…", "details": { "tsquery": "…" } },
      { "stage": "vector",  "title": "Nomic embedding + pgvector cosine",       "durationMs": 9.8, "sql": "…", "details": { "distances": "…" } },
      { "stage": "hybrid",  "title": "Reciprocal Rank Fusion (k=60)",           "durationMs": 0.2, "details": { "formulas": ["PROD-0012: 1/(60+2) + 1/(60+1) = 0.03252"] } },
      { "stage": "ontology","title": "SKOS concepts, expansion & domain rules", "durationMs": 7.9, "details": { "concepts": ["chargers", "laptops"], "expandedTerms": ["charger", "ac adapter", "power brick"], "ruleChecks": ["Charger fits laptop: connector barrel-5.5mm = usb-c ✗", "wattageW 45 ≥ 65 ✗"] } }
    ]
  }
}
```

### Pipeline stages

| Stage | Endpoint | Technique | Composes | ADR |
|---|---|---|---|---|
| 1. Structured | `/api/search/structured` | Parameterised SQL over columns + JSONB specs. Exact and precise; no notion of relevance. | — | [0007](0007-structured-search.md) |
| 2. Keyword | `/api/search/keyword` | Postgres FTS, **BM25-style** (`ts_rank_cd`, weighted fields). Fast and exact; misses synonyms. | — | [0008](0008-keyword-search-bm25-style.md) |
| 3. Vector | `/api/search/vector` | Nomic embeddings (local ONNX) + pgvector cosine. Understands meaning; similar ≠ compatible. | — | [0009](0009-local-embeddings-onnx-runtime.md), [0010](0010-vector-search-pgvector.md) |
| 4. Hybrid | `/api/search/hybrid` | RRF over keyword + vector ranks: RRF(d) = Σ wᵢ / (k + rᵢ(d)). | 2 + 3 | [0011](0011-hybrid-search-rrf.md) |
| 5. Ontology | `/api/search/ontology` | SKOS first: resolves the target device (a device name in the query is context, not search text), matches the query to SKOS concepts in any language, expands synonyms and narrower concepts, re-runs Hybrid, demotes out-of-concept items. Then one step beyond SKOS: class-level domain rules against the target device. **Flagged items are kept, with reasons.** Toggles: `expandSynonyms`, `applyConstraints`. Trace steps: understand → expand → keyword → vector (embed, search) → RRF → classify → constrain. | 4 | [0013](0013-domain-ontology-and-compatibility.md) |
| 6. RAG | `/api/search/rag` | Results as JSON immediately; `/answer` streams a markdown summary from a bounded evidence set (compatible + incompatible-with-reasons, concept definitions and labels), with `[PROD-…]` citations validated when complete. | 5 | [0016](0016-rag-grounding-and-citations.md) |
| 7. Pedagogy | `/api/search/pedagogy` | Results as JSON immediately; `/answer` streams the Stage 6 summary, then an audience-aware explanation. Pedagogy on: fixed headings (decision → concepts → near miss → rule of thumb → next step), with words chosen from ontology labels for the audience. Toggle `applyPedagogy: false`: the same facts and audience through a plain baseline prompt, to show what the design adds. | 6 | [0017](0017-pedagogy-engine.md) |

A **"Going further"** talk step follows Stage 7. It maps the discussed-not-built topics to where they sit in the pipeline ([ADR-0018](0018-scope-and-going-further.md)).

---

## 5. Frontend Layout

The `web-ui` **is the talk**. It has a home page (speaker, abstract, thesis), a keyboard-driven talk mode that replaces slides, the live demo, a glossary with inline term definitions, and the ADRs. The demo shows how the same query changes across the stages. It is branded Pi & Mash, in light and dark themes. → [ADR-0014](0014-web-ui-architecture.md), pictures in [docs/design](../design/README.md)

```text
+------------------------------------------------------------------------------------------+
| (logo) How to Find a Needle / Stage 5 of 7                    ←/→  [Presentation]  [☾]   |
+------------------------------------------------------------------------------------------+
| SEARCH · what is relevant?            | ONTOLOGY · how related? | PEDAGOGY · explain it?  |
| (1) Structured (2) Keyword (3) Vector (4) Hybrid | (5) Ontology | (6) RAG (7) Pedagogy    |
+------------------------------------------------------------------------------------------+
| [GQ-01 ▾] [ power adapter for my laptop           ] [I own: Aerobook 14 ▾] [Filters 0]    |
+------------------------------------------------------------------------------------------+
| How it works | Results 50 | Answer (6–7) | Under the hood 8     stage options: toggles, |
|                                                                 audience, apply pedagogy |
+------------------------------------------------------------------------------------------+
| The selected tab, full width:                                                            |
|   How it works    stage explanation with glossary hover cards                            |
|   Results         badges · Stage 5: in concept | out of concept | flagged, with checks   |
|   Answer          answer + citation chips | streamed explanation | evidence set          |
|   Under the hood  trace steps as a flow → the selected step's renderer                   |
+------------------------------------------------------------------------------------------+
```

- **Fixed search state:** query, filters, device, audience, toggles and the tab persist across stage switches (and live in the URL).
- **Stepper:** stages grouped by triad colour (purple Search, green Ontology, orange Pedagogy); click, or use ←/→ in the demo.
- **Tabs:** in talk mode → steps through a stage's tabs, then to the next step; H / R / A / U jump to a tab.
- **Filters:** a Filters button in the search bar; a sidebar in the demo, a drawer in talk mode; every value comes from the ontology.
- **Under the hood, per stage:**
  1. SQL + parameters + row count.
  2. Parsed tsquery, matched lexemes, "BM25-style" note.
  3. Query embedding details + cosine distances.
  4. Per-item RRF formula table.
  5. Matched concepts, expanded terms, in/out-of-concept classification, domain rule checks with values, flagged items.
  6. Evidence set, prompts, raw output, citation validation.
  7. Pedagogy or baseline prompt (audience section, labels offered), output validation.
- The 2D vector-space plot is a stretch goal, not in scope.

---

## 6. Coding standards & guidelines for AI coding assistants

Coding standards, commenting and naming conventions, and guidelines for AI assistants live in [CLAUDE.md](../../CLAUDE.md), with area-specific files in [src/PI.SearchApi](../../src/PI.SearchApi/CLAUDE.md), [src/web-ui](../../src/web-ui/CLAUDE.md) and [tests](../../tests/CLAUDE.md).

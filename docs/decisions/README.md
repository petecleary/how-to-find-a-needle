# Decisions

Why *How to Find a Needle* is built the way it is. Each record gives the context, the decision, the alternatives considered and what to take away. The web UI renders the same files on its **Decisions** page.

New here? Read [ADR-0004](0004-pipeline-composition.md) for how the stages fit together, then follow the stages in order.

## By stage

| Stage | Technique | Decision |
|---|---|---|
| 1 | Structured search | [ADR-0007](0007-structured-search.md) |
| 2 | Keyword search (BM25-style) | [ADR-0008](0008-keyword-search-bm25-style.md) |
| 3 | Vector search | [ADR-0009](0009-local-embeddings-onnx-runtime.md) (embeddings), [ADR-0010](0010-vector-search-pgvector.md) (pgvector) |
| 4 | Hybrid search (RRF) | [ADR-0011](0011-hybrid-search-rrf.md) |
| 5 | Ontology | [ADR-0013](0013-domain-ontology-and-compatibility.md) |
| 6 | RAG | [ADR-0016](0016-rag-grounding-and-citations.md) |
| 7 | Pedagogy | [ADR-0017](0017-pedagogy-engine.md) |

## All decisions

| # | Decision | Area | Status |
|---|---|---|---|
| [0001](0001-record-architecture-decisions.md) | Record architecture decisions | Foundation | Accepted |
| [0002](0002-solution-structure-and-orchestration.md) | Solution structure & orchestration | Foundation | Accepted |
| [0003](0003-search-api-contract-and-debug-trace.md) | Search API contract & debug trace | Foundation | Accepted |
| [0004](0004-pipeline-composition.md) | Pipeline composition | Foundation | Accepted |
| [0005](0005-curated-dataset-and-golden-queries.md) | Curated dataset & golden queries | Data | Accepted |
| [0006](0006-database-schema-and-seeding.md) | Database schema & seeding | Data | Accepted |
| [0007](0007-structured-search.md) | Stage 1 — Structured search | Search | Accepted |
| [0008](0008-keyword-search-bm25-style.md) | Stage 2 — Keyword search (BM25-style) | Search | Accepted |
| [0009](0009-local-embeddings-onnx-runtime.md) | Embedding providers & committed embedding files | Search | Accepted |
| [0010](0010-vector-search-pgvector.md) | Stage 3 — Vector search (pgvector) | Search | Accepted |
| [0011](0011-hybrid-search-rrf.md) | Stage 4 — Hybrid search with RRF | Search | Accepted |
| [0012](0012-bge-m3-dense-and-sparse.md) | BGE-M3 dense + sparse (not built) | Search | Rejected |
| [0013](0013-domain-ontology-and-compatibility.md) | Stage 5 — Ontology: SKOS taxonomy, vocabularies & domain rules | Search | Accepted |
| [0014](0014-web-ui-architecture.md) | Web UI — the talk, the demo & learning pages | Frontend | Accepted |
| [0015](0015-llm-hosting-and-client.md) | LLM provider & client | AI | Accepted |
| [0016](0016-rag-grounding-and-citations.md) | Stage 6 — RAG: streamed, grounded summary with citations | AI | Accepted |
| [0017](0017-pedagogy-engine.md) | Stage 7 — Pedagogy engine, with a baseline toggle | AI | Accepted |
| [0018](0018-scope-and-going-further.md) | Scope — seven stages, what the talk discusses, and what it leaves out | Scope | Accepted |

ADR numbers identify decisions, not stages ([ADR-0001](0001-record-architecture-decisions.md)).

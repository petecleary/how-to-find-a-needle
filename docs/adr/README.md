# Architecture Decision Records

Working decisions for building **How to Find a Needle**.

> **Working documents.** These ADRs are committed on the build branch while the talk is built. Once the build is complete, they will be rewritten as public, learner-facing ADRs (see [ADR-0001](0001-record-architecture-decisions.md)).

- [architecture.md](architecture.md): system overview (the "what").
- [roadmap.md](roadmap.md): build phases, tasks and acceptance criteria (the "when").
- The ADRs below: individual decisions (the "why").

ADR numbers identify decisions, not stages. The talk has seven stages ([ADR-0018](0018-scope-and-going-further.md)).

## Status legend

| Status | Meaning |
|---|---|
| **Proposed** | Written and agreed in principle; not yet implemented, or reopened because an amendment changes the code |
| **Accepted** | Implemented and verified in code |
| **Superseded** | Replaced by a later ADR (link to it) |
| **Rejected** | Considered and decided against; kept with the reason |
| **Deprecated** | No longer relevant; kept for history |

An ADR amended in place by a later ADR names it in its status line ([ADR-0001](0001-record-architecture-decisions.md)).

## Index

| # | Decision | Area | Roadmap phase | Status |
|---|---|---|---|---|
| [0001](0001-record-architecture-decisions.md) | Record architecture decisions | Foundation | — | Proposed |
| [0002](0002-solution-structure-and-orchestration.md) | Solution structure & orchestration | Foundation | 1 | Proposed |
| [0003](0003-search-api-contract-and-debug-trace.md) | Search API contract & debug trace | Foundation | 2 | Proposed (reopened by 0018; Phase 2 rework) |
| [0004](0004-pipeline-composition.md) | Pipeline composition | Foundation | 2 | Accepted (amended by 0018) |
| [0005](0005-curated-dataset-and-golden-queries.md) | Curated dataset & golden queries | Data | 1 | Proposed |
| [0006](0006-database-schema-and-seeding.md) | Database schema & seeding | Data | 1 (+ embeddings in 2) | Proposed (reopened by 0018; Phase 1 rework) |
| [0007](0007-structured-search.md) | Stage 1 — Structured search | Search | 2 | Accepted (amended by 0018) |
| [0008](0008-keyword-search-bm25-style.md) | Stage 2 — Keyword search (BM25-style) | Search | 2 | Accepted (amended by 0018) |
| [0009](0009-local-embeddings-onnx-runtime.md) | Embedding providers (local Nomic ONNX, OpenAI) & committed embedding files | Search | 2 (Nomic), 5 (OpenAI) | Proposed (Nomic built and verified; OpenAI provider in Phase 5) |
| [0010](0010-vector-search-pgvector.md) | Stage 3 — Vector search (pgvector) | Search | 2 | Accepted (amended by 0018) |
| [0011](0011-hybrid-search-rrf.md) | Stage 4 — Hybrid search with RRF | Search | 2 | Accepted (amended by 0018) |
| [0012](0012-bge-m3-dense-and-sparse.md) | BGE-M3 dense + sparse | Search | — | Rejected (by 0018) |
| [0013](0013-domain-ontology-and-compatibility.md) | Stage 5 — Ontology: SKOS taxonomy, vocabularies & domain rules | Search | 1 (data), 2 (stage) | Accepted (amended by 0018) |
| [0014](0014-web-ui-architecture.md) | Web UI: the talk, the demo & learning pages | Frontend | 3 | Proposed |
| [0015](0015-llm-hosting-and-client.md) | LLM provider & client | AI | 4 | Proposed |
| [0016](0016-rag-grounding-and-citations.md) | Stage 6 — RAG grounding & citations | AI | 4 | Proposed |
| [0017](0017-pedagogy-engine.md) | Stage 7 — Pedagogy engine, with baseline toggle | AI | 4 | Proposed |
| [0018](0018-scope-and-going-further.md) | Scope: seven stages, what the talk discusses, what it leaves out | Scope | 0–5 | Proposed |

Testing and observability are cross-cutting. They are covered in [0002](0002-solution-structure-and-orchestration.md) (test projects, conventions) and [0003](0003-search-api-contract-and-debug-trace.md) (tracing, timings).

## Template

Copy this for new ADRs. Name the file `NNNN-kebab-case-title.md`.

```markdown
# ADR-NNNN: Title

- **Status:** Proposed
- **Date:** YYYY-MM-DD
- **Related:** ADR-XXXX, roadmap phase N

## Context
What problem are we solving? What forces and constraints apply?

## Decision
What we will do, stated plainly. Include the concrete shapes (SQL, types, config) where they remove ambiguity.

## Consequences
What becomes easier, what becomes harder, and what risks we accept.

## Alternatives considered
| Option | Why not (for this repo) |
|---|---|

## Teaching notes
What a learner should take away. This seeds the public ADR.
```

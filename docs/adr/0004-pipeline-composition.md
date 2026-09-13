# ADR-0004: Pipeline composition

- **Status:** Proposed
- **Date:** 2026-09-13
- **Related:** ADR-0002, ADR-0003, ADR-0007 to ADR-0017; roadmap Phase 2

## Context

The talk's argument is that search is a **pipeline**, with each technique fixing a failure mode of the one before. The code should make that visible: Hybrid is Keyword + Vector, and Ontology checks what Hybrid found.

If every endpoint re-implemented its whole pipeline, we would duplicate SQL and model code, and the lesson that stages build on each other would be lost.

## Decision

### One service per technique

Each technique is a small service in `Pipeline/{Technique}/` behind an interface. Each returns a **candidate list** plus the trace steps it produced.

| Interface | Implementation | Input | Output |
|---|---|---|---|
| `IStructuredSearch` | SQL filters | filters, paging | products |
| `IKeywordSearch` | Postgres FTS | query, filters, depth | ranked candidates |
| `IVectorSearch` | Nomic + pgvector | query, filters, depth | ranked candidates |
| `IRankFusion` | RRF (pure, no I/O) | N ranked lists + weights, k | fused ranking |
| `IBgeM3Search` | BGE-M3 dense + sparse → `IRankFusion` | query, filters, depth | ranked candidates |
| `IOntologyEvaluator` | dotNetRDF + SPARQL | candidates, target device | candidates with compatibility |
| `IAnswerGenerator` | LLM (RAG) | evaluated candidates, query | grounded answer + citations |
| `IPedagogyEngine` | LLM (pedagogy) | answer, evaluated candidates, audience | explanation |

Supporting services: `INomicEmbedder` and `IBgeM3Embedder` ([ADR-0009](0009-local-embeddings-onnx-runtime.md), [ADR-0012](0012-bge-m3-dense-and-sparse.md)), and `IKnowledgeGraph`, which loads and queries the RDF graph ([ADR-0013](0013-domain-ontology-and-compatibility.md)).

Shared types (in `Pipeline/`):

```csharp
public sealed record Candidate(ProductSummary Product, double Score, CandidateSignals Signals);
public sealed record StageResult(IReadOnlyList<Candidate> Candidates, IReadOnlyList<TraceStep> Trace);
```

### How stages compose

```text
Stage 1  Structured ─────────────────────────────────────────────▶ results
Stage 2  Keyword ────────────────────────────────────────────────▶ results
Stage 3  Vector ─────────────────────────────────────────────────▶ results
Stage 4  Keyword ─┐
                  ├─▶ RRF ───────────────────────────────────────▶ results
         Vector ──┘
Stage 5  BGE dense ─┐
                    ├─▶ RRF ─────────────────────────────────────▶ results
         BGE sparse ┘
Stage 6  [Stage 4 pipeline] ─▶ Ontology evaluate ────────────────▶ results (+ rejected, with reasons)
Stage 7  [Stage 6 pipeline] ─▶ RAG answer ───────────────────────▶ results + answer
Stage 8  [Stage 7 pipeline] ─▶ Pedagogy ─────────────────────────▶ results + answer + explanation
```

- **Stage 6 uses Hybrid (Stage 4) as its candidate source.** Hybrid is the strongest English retriever, and one fixed source keeps the demo predictable. BGE-M3 stays a separate comparison stage.
- **Retrieval depth vs page size.** Services retrieve `candidateDepth` items (default 50). Fusion and evaluation run over that whole set, and **paging is applied once, at the end**, in the endpoint. This ensures RRF doesn't only see the first page.
- **Trace steps accumulate.** A composed stage adds its own step after the steps of the services it called ([ADR-0003](0003-search-api-contract-and-debug-trace.md)).
- **Endpoints stay thin.** Each one validates, calls the top-level service for its stage, pages the results, and maps them to `SearchResponse`.
- Services are registered in DI in `Program.cs`, in a clearly commented block per stage. Embedders and the knowledge graph are **singletons**, because they load their model or graph once. Search services are scoped or transient, and use the pooled `NpgsqlDataSource`.

## Consequences

- The call graph mirrors the talk's slides, so learners can follow a request from endpoint to technique.
- RRF is a pure function and is exhaustively unit-testable.
- Composed stages run earlier stages again on each request; for example, Stage 8 runs Keyword, Vector, RRF, Ontology, RAG and Pedagogy. That is fine at demo scale and is the honest cost of a pipeline. No caching, to keep behaviour transparent.
- Changing Keyword ranking changes Stages 4 and 6–8 too. Golden-query tests catch surprises ([ADR-0005](0005-curated-dataset-and-golden-queries.md)).

## Alternatives considered

| Option | Why not (for this repo) |
|---|---|
| Fully independent endpoints | Duplicates logic; hides the "pipeline" idea |
| Generic middleware-style pipeline (`IPipelineStep` chain) | Elegant but abstract; learners must understand the framework before the search |
| Client-side composition (UI calls stages and combines) | Moves search logic into the browser; the trace becomes fragmented |
| Configurable candidate source for Stage 6 | Adds a demo variable with little teaching value; can be added later |

## Teaching notes

- Retrieval (find candidates) → fusion (combine opinions) → evaluation (apply rules) → generation (explain). Each layer has a single responsibility.
- "Retrieve deep, page late": ranking quality depends on fusing *enough* candidates.

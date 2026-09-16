# ADR-0004: Pipeline composition

- **Status:** Accepted
- **Area:** Foundation
- **Related:** [ADR-0003](0003-search-api-contract-and-debug-trace.md), [ADR-0007](0007-structured-search.md) to [ADR-0011](0011-hybrid-search-rrf.md), [ADR-0013](0013-domain-ontology-and-compatibility.md), [ADR-0016](0016-rag-grounding-and-citations.md), [ADR-0017](0017-pedagogy-engine.md)

## Context

The talk's argument is that search is a **pipeline**: each technique fixes a failure of the one before. The code should make that visible. Hybrid search *is* keyword plus vector search; the ontology stage *checks* what hybrid search found.

If every endpoint re-implemented its whole pipeline, the SQL and model code would be duplicated, and the lesson that stages build on each other would disappear.

## Decision

### One small service per technique

Each technique lives in `Pipeline/{Technique}/` behind an interface, and returns a **candidate list plus the trace steps it produced**.

| Interface | Technique | Output |
|---|---|---|
| `IStructuredSearch` | SQL filters | products, with a real count |
| `IKeywordSearch` | Postgres full-text search | ranked candidates |
| `IVectorSearch` | Embedding + pgvector | ranked candidates |
| `IRankFusion` | Reciprocal Rank Fusion (pure maths, no I/O) | one fused ranking |
| `IOntologySearch` | Understand, expand, retrieve, classify, constrain | candidates with concept match and compatibility |
| `IAnswerGenerator` | LLM answer from bounded evidence | streamed markdown, then validated citations |
| `IPedagogyEngine` | LLM explanation of that answer | streamed markdown, then validated structure |

```csharp
public sealed record Candidate(ProductSummary Product, double? Score, CandidateSignals Signals, CompatibilityResult Compatibility);
public sealed record StageResult(IReadOnlyList<Candidate> Candidates, IReadOnlyList<TraceStep> Trace, int? TotalResults = null);
```

### How the stages compose

```text
Stage 1  Structured ───────────────────────────────────────────────▶ results
Stage 2  Keyword ──────────────────────────────────────────────────▶ results
Stage 3  Vector ───────────────────────────────────────────────────▶ results
Stage 4  Keyword ─┐
                  ├─▶ RRF ─────────────────────────────────────────▶ results
         Vector ──┘
Stage 5  understand ─▶ expand ─▶ Keyword ─┐
                                          ├─▶ RRF ─▶ classify ─▶ constrain ─▶ results, flagged items kept
                                 Vector ──┘
Stage 6  [Stage 5] ─▶ results      /answer: [Stage 5] ─▶ RAG ─────────────▶ streamed answer
Stage 7  [Stage 5] ─▶ results      /answer: [Stage 5] ─▶ RAG ─▶ Pedagogy ─▶ streamed answer + explanation
```

- **Stage 5 re-runs the Stage 4 pipeline with better input**: the device name removed and the synonyms added. Two options, `expandSynonyms` and `applyConstraints`, switch its steps on and off so each effect can be shown on its own.
- **Retrieve deep, page late.** Retrievers fetch `candidateDepth` products (50 by default). Fusion and rule checks run over all of them, and paging happens **once, at the end**, in the endpoint. Stage 1 is the exception: it has no ranking to protect, so it pages in SQL.
- **Trace steps accumulate.** A composed stage appends its own step after the steps of the services it called.
- **Endpoints stay thin:** validate, call the stage's top-level service, page, map.

## Consequences

- The call graph mirrors the talk. A learner can follow one request from the endpoint through every technique.
- RRF is a pure function, so it is tested exhaustively with hand-calculated examples.
- Later stages re-run earlier ones on every request: Stage 7 runs keyword, vector, fusion, ontology, RAG and pedagogy. At this scale that costs tens of milliseconds before the LLM, and there is no cache to hide what happens.
- Changing keyword ranking changes Stages 4–7 as well. The golden-query tests catch the surprises ([ADR-0005](0005-curated-dataset-and-golden-queries.md)).

## Alternatives considered

| Option | Why not (for this repository) |
|---|---|
| Fully independent endpoints | Duplicates logic and hides the pipeline |
| A generic step-chain framework (`IPipelineStep`) | Elegant, but learners must understand the framework before the search |
| Composing stages in the browser | Moves search logic into the UI and splits the trace |
| A configurable retriever for Stage 5 | Another variable in the demo, with little to teach |

## What to take away

- Retrieval finds candidates, fusion combines opinions, evaluation applies rules, generation explains. Give each layer one job.
- Retrieve deep, page late: ranking quality depends on fusing and checking *enough* candidates, not just the first page.
- **Page late, and read every page.** When the catalog grew from 60 to 300 products, Stage 5 started returning more than one page of 50: fusion keeps the union of two lists, and flagged items sort last. The UI and the tests had quietly assumed one page was everything; both now read every page.

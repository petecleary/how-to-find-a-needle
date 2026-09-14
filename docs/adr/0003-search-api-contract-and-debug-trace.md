# ADR-0003: Search API contract & debug trace

- **Status:** Accepted (Phase 2, 2026-09-14). Amended by [ADR-0018](0018-scope-and-going-further.md), which removed the `bge-m3` route and BGE signals and added `options.applyPedagogy`; re-accepted after the Phase 2 rework was verified (2026-09-14). Amended 2026-09-14 to add `GET /api/vocabularies` (built and verified).
- **Date:** 2026-09-13
- **Related:** ADR-0002, ADR-0004, ADR-0014, ADR-0017, ADR-0018; roadmap Phase 2

## Context

The talk shows the **same query** changing across 7 stages. That only works if every stage accepts the same request and returns the same response shape, so the UI can switch stages without special cases.

Search requests carry nested data: filters, spec constraints and tuning options. These are awkward to express as GET query strings.

The **debug trace** is the main way learners see how each stage works: SQL, lexemes, distances, RRF maths, SPARQL, prompts. It is a core feature, not an optional diagnostic.

## Decision

### Routes

| Stage | Route |
|---|---|
| 1 Structured | `POST /api/search/structured` |
| 2 Keyword | `POST /api/search/keyword` |
| 3 Vector | `POST /api/search/vector` |
| 4 Hybrid | `POST /api/search/hybrid` |
| 5 Ontology | `POST /api/search/ontology` |
| 6 RAG | `POST /api/search/rag` |
| 7 Pedagogy | `POST /api/search/pedagogy` |

### LLM answer streams (Stages 6–7)

Stages 6 and 7 use **two requests**, sent by the UI at the same time with the same body:

| Request | Returns |
|---|---|
| `POST /api/search/rag` · `POST /api/search/pedagogy` | The normal JSON `SearchResponse`: the Stage 5 pipeline's results and trace, including an `evidence` step listing the products the summary will use. Renders immediately |
| `POST /api/search/rag/answer` · `POST /api/search/pedagogy/answer` | The LLM's **markdown** summary as **Server-Sent Events** (`text/event-stream`), shown above the results like an AI overview on a search page |

- **Answer endpoints are stateless.** They re-run the Stage 5 pipeline with the same request to build the evidence set. Retrieval is deterministic, so both requests see the same products, for a cost of a few tens of milliseconds.
- **Events:**
  - `meta`: evidence IDs, provider, model.
  - `delta`: markdown text, with `section` = `answer` | `explanation`.
  - `final`: per section, the full markdown, citations, warnings, and (Stage 7) the parsed structure.
  - `done`: time to first token, total time, trace.
  - `error`: a ProblemDetails body, e.g. `503` when the LLM is unavailable. The results request is unaffected.

  Payloads are specified in [ADR-0016](0016-rag-grounding-and-citations.md) and [ADR-0017](0017-pedagogy-engine.md).
- **`Accept: application/json`** on an answer endpoint waits for completion and returns the same content as one JSON object. Integration tests and Scalar use this form.
- **Cancellation:** closing the connection cancels generation, because the request's `CancellationToken` flows into `IChatClient`.
- **OpenAPI** describes the JSON form. OpenAPI 3.1 has no first-class way to describe event-stream payloads, so the UI hand-types the event shapes in `src/api/answerEvents.ts`.

Supporting read-only endpoints for the UI:
- `GET /api/demo/queries` returns the golden queries ([ADR-0005](0005-curated-dataset-and-golden-queries.md)) as presets.
- `GET /api/demo/devices` returns products in device categories (per the taxonomy) that can be a *target device*.
- `GET /api/taxonomy` returns the SKOS concept tree read from `domain-ontology.ttl`: notations, language-tagged labels, synonyms, definitions, icons and narrower concepts. The UI builds its category filter from it ([ADR-0013](0013-domain-ontology-and-compatibility.md)).
- `GET /api/vocabularies` returns the value vocabularies (connectors, storage interfaces, memory types, battery platforms): each value's notation, labels and synonyms, and the spec keys that use the vocabulary. The UI builds its spec filters from it ([ADR-0013](0013-domain-ontology-and-compatibility.md)).

The legacy `GET /api/products` endpoint is removed.

### Request (`Contracts/SearchRequest.cs`)

```json
{
  "query": "charger for my Blackbird Aerobook 14",
  "page": 1,
  "pageSize": 10,
  "filters": {
    "brand": "Voltline",
    "categories": ["laptop-chargers"],
    "minPrice": 20.00,
    "maxPrice": 150.00,
    "specs": { "connector": "usb-c" }
  },
  "context": {
    "targetProductId": "PROD-0001"
  },
  "options": {
    "candidateDepth": 50,
    "rrfK": 60,
    "keywordWeight": 1.0,
    "vectorWeight": 1.0,
    "expandSynonyms": true,
    "applyConstraints": true,
    "audience": "novice",
    "applyPedagogy": true,
    "explain": false
  }
}
```

- `filters` apply as **pre-filters** in every stage, and are the *only* input to Stage 1.
- `context.targetProductId` is the device the user owns. It is optional, and Stages 5–7 use it ([ADR-0013](0013-domain-ontology-and-compatibility.md)).
- `options` are stage-specific tuning values. Stages ignore options that don't apply to them, and the trace lists the options each stage actually used.
- `options.explain` (default `false`) adds the Postgres `EXPLAIN` plan to vector-search trace steps, to show whether the planner used the HNSW index ([ADR-0010](0010-vector-search-pgvector.md)).
- `options.audience` is a lower-case string (`novice | enthusiast | expert`), not an enum, matching the audience vocabulary used by the UI content and prompts ([ADR-0017](0017-pedagogy-engine.md)).
- `options.applyPedagogy` (default `true`) is Stage 7's before/after toggle, like Stage 5's `expandSynonyms` and `applyConstraints`. With `false`, Stage 7 explains the same answer and evidence, for the same audience, with a plain baseline prompt instead of the pedagogy prompt ([ADR-0017](0017-pedagogy-engine.md)).

**Validation (FluentValidation, one validator per endpoint):**
- `page` ≥ 1 and `pageSize` between 1 and 50.
- `query` is required, 1–500 characters, for stages 2–7. It is ignored by Stage 1.
- `candidateDepth` is between 10 and 200, and `rrfK` between 1 and 1000.
- Weights are between 0 and 10.
- `audience` is one of `novice | enthusiast | expert`.
- `filters.categories` must be known taxonomy notations.

### Response (`Contracts/SearchResponse.cs`)

```json
{
  "stage": "hybrid",
  "query": "charger for my Blackbird Aerobook 14",
  "page": 1,
  "pageSize": 10,
  "totalResults": 37,
  "executionTimeMs": 14.2,
  "results": [
    {
      "id": "PROD-0012",
      "name": "Voltline 65W USB-C GaN Charger",
      "brand": "Voltline",
      "categories": ["laptop-chargers", "usb-c-pd-chargers"],
      "price": 49.99,
      "specs": { "connector": "usb-c", "wattage": 65 },
      "score": 0.0325,
      "signals": {
        "structuredMatch": null,
        "keywordRank": 2, "keywordScore": 0.41,
        "vectorRank": 1, "vectorDistance": 0.124,
        "fusedRank": 1,
        "conceptMatch": null
      },
      "compatibility": { "status": "NotEvaluated", "reasons": [] }
    }
  ],
  "debugTrace": { "steps": [] }
}
```

- `score` means different things per stage; the trace explains it. `signals` keeps each technique's raw rank and score, so the UI can show badges.
- `compatibility.status` is one of `NotEvaluated | Compatible | Incompatible | Unknown`. `reasons` holds human-readable strings that quote the domain rule and the spec values compared.
- `signals.conceptMatch` is `InConcept | OutOfConcept | NoConcept`, set by Stage 5 ([ADR-0013](0013-domain-ontology-and-compatibility.md)); `null` in other stages.
- `totalResults` for ranked stages is the number of candidates retrieved, which is bounded by `candidateDepth`. It is **not** a count of the whole catalog. The trace says so.
- `SearchResponse` never contains LLM text. Stages 6–7 stream it from their answer endpoints (see *LLM answer streams* above).

### Debug trace (`Contracts/DebugTrace.cs`)

The trace is an **ordered list of steps**, so composed stages show their whole pipeline. For example, Stage 5 shows Keyword → Vector → RRF → Ontology.

```json
{
  "steps": [
    {
      "stage": "keyword",
      "title": "Postgres full-text search (BM25-style)",
      "durationMs": 3.1,
      "sql": "SELECT ... ts_rank_cd(...) ...",
      "parameters": { "query": "charger aerobook 14" },
      "details": { "tsquery": "'charger' & 'aerobook' & '14'" },
      "notes": ["ts_rank_cd is not true BM25 — no IDF or term-frequency saturation."]
    }
  ]
}
```

- Common fields: `stage`, `title`, `durationMs`, `notes[]`.
- Optional typed sections, each filled in by the stage that uses it:
  - `sql` + `parameters`
  - `details` for stage-specific structured data: lexemes, distances, per-item RRF formula strings, matched concepts, expanded terms, SPARQL text, rule checks, flagged items, model name, prompts, raw LLM output
- **Parameter values are shown next to the SQL, never interpolated into it.** The SQL shown is the exact parameterised statement that ran.
- The trace is always populated. This is a local teaching app, so there is no production switch to hide it. The ADR records that a real system would gate it.

### Errors and observability

- Validation and unexpected errors return RFC 9457 ProblemDetails (`AddProblemDetails()` already exists).
- Missing ONNX models or an unreachable Ollama return `503` with a ProblemDetails `detail` explaining how to fix it.
- Every stage starts an OpenTelemetry `Activity` (`ActivitySource` named after the app), so the Aspire dashboard shows the same pipeline the trace shows.
- `executionTimeMs` covers the endpoint's total time. Per-step `durationMs` comes from `Stopwatch`.

## Consequences

- The UI works with one TypeScript type ([ADR-0014](0014-web-ui-architecture.md)), and stages can be compared side by side.
- POST for reads is less cacheable and less RESTful. That is acceptable for a demo API, and explained to learners.
- **QUERY is the planned upgrade.** HTTP QUERY is safe and idempotent like GET, but carries a body like POST, which is exactly what a search request is. .NET 10 already supports it (`HttpMethods.Query`). **Revisit when `Microsoft.OpenApi` supports OpenAPI 3.2**, the first version that can describe a QUERY operation. The switch is then one line per endpoint, plus regenerating the UI types.
- `details` is loosely typed (a dictionary of JSON values) to avoid 7 response subtypes. The UI renders known keys and shows unknown ones as raw JSON.

## Alternatives considered

| Option | Why not (for this repo) |
|---|---|
| GET with query strings | Nested filters and options become awkward and unreadable |
| HTTP QUERY method | The best semantic fit, and ASP.NET Core 10 supports it. But `Microsoft.OpenApi` 2.x only emits OpenAPI 3.0/3.1, which can't describe QUERY, so the endpoints would be missing from Scalar and from the generated UI types ([ADR-0014](0014-web-ui-architecture.md)). Adopt once OpenAPI 3.2 is supported |
| A different response type per stage | Breaks the "same query, switch stage" demo and doubles UI code |
| Strongly typed trace per stage | Precise, but 7 polymorphic types add noise; revisit if the UI suffers |
| Trace only in the Aspire dashboard | Hidden from the audience; the trace must appear alongside the results |

## Teaching notes

- A stable contract is what makes an experiment fair: same input, same output shape, different technique.
- "Explainability by design": if you can't show *why* a result ranked where it did, you can't debug search.
- POST for search is a long-standing workaround for "a read that needs a body". HTTP QUERY is the proper answer. Tooling support (here, OpenAPI) often decides when you can adopt a standard, not just the server framework.

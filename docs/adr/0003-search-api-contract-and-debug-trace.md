# ADR-0003: Search API contract & debug trace

- **Status:** Proposed
- **Date:** 2026-09-13
- **Related:** ADR-0002, ADR-0004, ADR-0014; roadmap Phase 2

## Context

The talk shows the **same query** changing across 8 stages. That only works if every stage accepts the same request and returns the same response shape, so the UI can switch stages without special cases.

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
| 5 BGE-M3 | `POST /api/search/bge-m3` |
| 6 Ontology | `POST /api/search/ontology` |
| 7 RAG | `POST /api/search/rag` |
| 8 Pedagogy | `POST /api/search/pedagogy` |

Supporting read-only endpoints for the UI:
- `GET /api/demo/queries` returns the golden queries ([ADR-0005](0005-curated-dataset-and-golden-queries.md)) as presets.
- `GET /api/demo/devices` returns products in device categories (per the taxonomy) that can be a *target device*.
- `GET /api/taxonomy` returns the SKOS concept tree read from `domain-ontology.ttl`: notations, language-tagged labels, synonyms, definitions, icons and narrower concepts. The UI builds its category filter from it ([ADR-0013](0013-domain-ontology-and-compatibility.md)).

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
    "audience": "novice"
  }
}
```

- `filters` apply as **pre-filters** in every stage, and are the *only* input to Stage 1.
- `context.targetProductId` is the device the user owns. It is optional, and Stages 6–8 use it ([ADR-0013](0013-domain-ontology-and-compatibility.md)).
- `options` are stage-specific tuning values. Stages ignore options that don't apply to them, and the trace lists the options each stage actually used.

**Validation (FluentValidation, one validator per endpoint):**
- `page` ≥ 1 and `pageSize` between 1 and 50.
- `query` is required, 1–500 characters, for stages 2–8. It is ignored by Stage 1.
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
        "bgeDenseRank": null, "bgeSparseRank": null,
        "fusedRank": 1,
        "conceptMatch": null
      },
      "compatibility": { "status": "NotEvaluated", "reasons": [] }
    }
  ],
  "answer": null,
  "explanation": null,
  "debugTrace": { "steps": [] }
}
```

- `score` means different things per stage; the trace explains it. `signals` keeps each technique's raw rank and score, so the UI can show badges.
- `compatibility.status` is one of `NotEvaluated | Compatible | Incompatible | Unknown`. `reasons` holds human-readable strings that quote the domain rule and the spec values compared.
- `signals.conceptMatch` is `InConcept | OutOfConcept | NoConcept`, set by Stage 6 ([ADR-0013](0013-domain-ontology-and-compatibility.md)); `null` in other stages.
- `totalResults` for ranked stages is the number of candidates retrieved, which is bounded by `candidateDepth`. It is **not** a count of the whole catalog. The trace says so.
- `answer` is populated by Stage 7 ([ADR-0016](0016-rag-grounding-and-citations.md)). `explanation` is populated by Stage 8 ([ADR-0017](0017-pedagogy-engine.md)).

### Debug trace (`Contracts/DebugTrace.cs`)

The trace is an **ordered list of steps**, so composed stages show their whole pipeline. For example, Stage 6 shows Keyword → Vector → RRF → Ontology.

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
- `details` is loosely typed (a dictionary of JSON values) to avoid 8 response subtypes. The UI renders known keys and shows unknown ones as raw JSON.

## Alternatives considered

| Option | Why not (for this repo) |
|---|---|
| GET with query strings | Nested filters and options become awkward and unreadable |
| HTTP QUERY method | The best semantic fit, and ASP.NET Core 10 supports it. But `Microsoft.OpenApi` 2.x only emits OpenAPI 3.0/3.1, which can't describe QUERY, so the endpoints would be missing from Scalar and from the generated UI types ([ADR-0014](0014-web-ui-architecture.md)). Adopt once OpenAPI 3.2 is supported |
| A different response type per stage | Breaks the "same query, switch stage" demo and doubles UI code |
| Strongly typed trace per stage | Precise, but 8 polymorphic types add noise; revisit if the UI suffers |
| Trace only in the Aspire dashboard | Hidden from the audience; the trace must appear alongside the results |

## Teaching notes

- A stable contract is what makes an experiment fair: same input, same output shape, different technique.
- "Explainability by design": if you can't show *why* a result ranked where it did, you can't debug search.
- POST for search is a long-standing workaround for "a read that needs a body". HTTP QUERY is the proper answer. Tooling support (here, OpenAPI) often decides when you can adopt a standard, not just the server framework.

# ADR-0003: Search API contract & debug trace

- **Status:** Accepted
- **Area:** Foundation
- **Related:** [ADR-0004](0004-pipeline-composition.md), [ADR-0014](0014-web-ui-architecture.md), [ADR-0016](0016-rag-grounding-and-citations.md), [ADR-0017](0017-pedagogy-engine.md)

## Context

The talk runs the **same query** through seven stages and shows how the results change. That only works if every stage takes the same request and returns the same response, so the UI can switch stage without special cases.

Learners also need to see *why* each stage ranked what it did: the SQL, the parsed query, the distances, the fusion maths, the matched concepts, the rule checks, the prompts. That explanation is a feature, not a debugging aid.

## Decision

### One route per stage, all POST

`POST /api/search/{stage}`, where stage is `structured`, `keyword`, `vector`, `hybrid`, `ontology`, `rag` or `pedagogy`. Search requests carry nested filters and options, which don't fit a query string.

Supporting read-only endpoints feed the UI: `GET /api/demo/queries` (the golden queries as presets), `GET /api/demo/devices` (products that can be a target device), `GET /api/taxonomy` and `GET /api/vocabularies` (categories and spec values from the ontology, [ADR-0013](0013-domain-ontology-and-compatibility.md)) and `GET /api/brands`.

### The request

```json
{
  "query": "power adapter for my laptop",
  "page": 1,
  "pageSize": 10,
  "filters": { "brand": null, "categories": ["laptop-chargers"], "minPrice": 20, "maxPrice": 150, "specs": { "connector": "usb-c" } },
  "context": { "targetProductId": "PROD-0001" },
  "options": {
    "candidateDepth": 50, "rrfK": 60, "keywordWeight": 1.0, "vectorWeight": 1.0,
    "expandSynonyms": true, "applyConstraints": true,
    "audience": "novice", "applyPedagogy": true, "explain": false
  }
}
```

- `filters` apply in **every** stage, and are the only input to Stage 1.
- `context.targetProductId` is the device the shopper owns ("my laptop"). Stages 5–7 check compatibility against it.
- `options` hold each stage's tuning knobs. A stage ignores the options that don't apply to it, and its trace lists the ones it used.
- Requests are validated (page size 1–50, candidate depth 10–200, known categories and audiences), and a failure returns a `400` ProblemDetails naming each broken rule.

### The response

```json
{
  "stage": "hybrid",
  "totalResults": 58,
  "results": [{
    "id": "PROD-0012", "name": "Voltline 65W USB-C GaN Charger", "price": 49.99,
    "score": 0.03279,
    "signals": { "keywordRank": 1, "vectorRank": 1, "vectorDistance": 0.241, "fusedRank": 1, "conceptMatch": null },
    "compatibility": { "status": "NotEvaluated", "reasons": [] }
  }],
  "debugTrace": { "steps": [] }
}
```

- **`score` means something different in each stage**, and the trace says what. `signals` keeps each technique's own rank and score, so you can see where a result came from.
- `compatibility.status` is `NotEvaluated`, `Compatible`, `Incompatible` or `Unknown`, with human-readable `reasons`. `compatibility.source` says what the rules were checked against (`Device`, `Query` or `None`), and with no target device, `compatibility.fits` lists the catalog devices the product fits ([ADR-0013](0013-domain-ontology-and-compatibility.md)).
- **`totalResults` for ranked stages counts what was retrieved, not the catalog.** Each retriever returns at most `candidateDepth` products, and hybrid fusion keeps the union of both lists, so a stage can return more than `candidateDepth`. Only Stage 1 returns a real count.
- The response never contains LLM text. Stages 6 and 7 stream that separately (below).

### The debug trace

An ordered list of steps, so a composed stage shows its whole pipeline: Stage 5's trace reads understand → expand → keyword → vector → fuse → classify → constrain.

```json
{
  "stage": "keyword",
  "title": "Postgres full-text search (BM25-style)",
  "durationMs": 3.1,
  "sql": "SELECT ... ts_rank_cd(search_vector, q) ...",
  "parameters": { "query": "power adapter for my laptop" },
  "details": { "tsquery": "'power' & 'adapt' & 'laptop'" },
  "notes": ["ts_rank_cd is not true BM25: no IDF and no term-frequency saturation."]
}
```

- **The SQL shown is the exact parameterised statement that ran.** Values sit next to it, never pasted into it.
- `details` is a loose dictionary of stage-specific data. The UI has purpose-built views for the keys it knows and shows anything else as JSON.
- The trace is always on. This is a local teaching app; a production system would put it behind a switch.

### LLM answers stream separately (Stages 6–7)

The UI sends two requests at once, with the same body:

| Request | Returns |
|---|---|
| `POST /api/search/rag` (or `pedagogy`) | The normal JSON response: results and trace, in milliseconds |
| `POST /api/search/rag/answer` (or `pedagogy/answer`) | The LLM's markdown as **Server-Sent Events**: `meta`, `delta` text chunks, `final` (validated text, citations, warnings), `done` (timings) or `error` |

The answer endpoint is stateless: it re-runs retrieval, which is deterministic, so both requests see the same products. With `Accept: application/json` it waits and returns one JSON object instead, which is what the tests use.

### Errors and observability

- A missing embedding model or an unreachable LLM returns `503` with a `detail` that says how to fix it. Stages that don't need the missing piece keep working.
- Every stage starts an OpenTelemetry activity, so the Aspire dashboard shows the same pipeline as the trace.

## Consequences

- The UI works with one response type, so switching stage is a single request.
- POST for a read is less cacheable and less RESTful. For a teaching API that is an acceptable, explained trade.
- A loosely typed `details` avoids seven response subtypes, at the cost of some type safety in the UI.

## Alternatives considered

| Option | Why not (for this repository) |
|---|---|
| GET with query strings | Nested filters and options become unreadable |
| HTTP QUERY | The best fit: safe like GET, with a body like POST, and .NET 10 supports it. But OpenAPI 3.1 can't describe it, so the endpoints would vanish from Scalar and the generated UI types. Worth adopting once OpenAPI 3.2 tooling arrives |
| A different response type per stage | Breaks "same query, switch stage" and doubles the UI code |
| One response streaming results and answer together | Makes every stage's contract depend on the slowest one |
| Trace only in the Aspire dashboard | Hidden from the audience; the explanation belongs next to the results |

## What to take away

- A stable contract is what makes a comparison fair: same input, same output shape, different technique.
- Explainability by design: if you can't show *why* something ranked where it did, you can't debug search.
- Don't make the fast part wait for the slow part. Return results immediately and stream the generated text.
- POST for search is a long-standing workaround for "a read that needs a body". Tooling support often decides when you can adopt the proper standard.

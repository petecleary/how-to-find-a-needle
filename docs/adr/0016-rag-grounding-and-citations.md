# ADR-0016: Stage 7 — RAG: streamed, grounded summary with citations

- **Status:** Proposed
- **Date:** 2026-09-13
- **Related:** ADR-0003, ADR-0004, ADR-0013, ADR-0014, ADR-0015, ADR-0017; golden queries GQ-01, GQ-05, GQ-06; roadmap Phase 4

## Context

After Stage 6 we have relevant candidates, their concept matches *and* domain rule checks with reasons. Stage 7 lets an LLM answer the user's question in natural language, **using only that evidence**, and cite it.

Without grounding, an LLM will happily recommend the 45W barrel charger, or invent a product. Grounding and citations make the answer checkable, and the UI can link each claim back to a product card.

Retrieval takes milliseconds; generation takes seconds. Users shouldn't wait for the slow part to see the fast part. The experience we want is familiar from web search: **the result list appears immediately, and a written summary streams in above it**. A JSON schema output can't be shown progressively, so the summary is **markdown**, validated once it's complete.

## Decision

### Two requests ([ADR-0003](0003-search-api-contract-and-debug-trace.md))

| Request | Returns |
|---|---|
| `POST /api/search/rag` | The normal JSON `SearchResponse`: the Stage 6 pipeline's results and trace, with an `evidence` trace step listing the product IDs the summary will use. Renders immediately |
| `POST /api/search/rag/answer` | The summary as **Server-Sent Events**, or as one JSON object when `Accept: application/json` (tests, Scalar) |

The UI sends both at once with the same body ([ADR-0014](0014-web-ui-architecture.md)). The answer endpoint is **stateless**: it re-runs the Stage 6 pipeline with the same request and builds the same evidence set, because retrieval is deterministic.

### Evidence set (`IAnswerGenerator`)

- **Bounded**, for predictability and small-model context:
  - The target device, if resolved.
  - Up to **5 Compatible** products (by rank).
  - Up to **3 Incompatible** products **with their reasons**, so the model can warn about them.
  - Up to **2 Unknown** products.
- **Each item** is rendered as a compact, labelled block: `[PROD-0012] Voltline 65W USB-C GaN Charger — £49.99 — connector: USB-C, 65W — Compatibility: Compatible (connector USB-C matches; 65W ≥ 65W)`.
- Descriptions are truncated to about 300 characters, and reviews are excluded (to save tokens and reduce noise).
- The matched concepts and the domain rules that fired are included once, with their `skos:definition` text, so the answer explains constraints in the domain's own words ([ADR-0013](0013-domain-ontology-and-compatibility.md)).

### Prompt (versioned files in `assets/prompts/rag-system.md` and `rag-user.md`)

The system prompt rules, in plain language:
1. Answer **only** from the evidence.
2. Write **short markdown**: at most 120 words, short paragraphs or bullets, no headings.
3. **Every product claim must cite** its ID in square brackets, e.g. `[PROD-0012]`.
4. Never recommend a product marked Incompatible. Mention it only to warn, citing its reason.
5. Do not mention products that aren't in the evidence.
6. If the evidence doesn't answer the question, make the **first line exactly `INSUFFICIENT_EVIDENCE`**, then say briefly what is missing.

### Streaming

- The server calls `IChatClient.GetStreamingResponseAsync` ([ADR-0015](0015-llm-hosting-and-client.md)), forwards each text chunk as a `delta` event, and accumulates the full text.
- Event sequence:

```text
event: meta
data: {"stage":"rag","provider":"ollama","model":"<model>","evidence":["PROD-0012","PROD-0014","PROD-0019"]}

event: delta
data: {"section":"answer","text":"The Voltline 65W USB-C charger [PROD-0012] "}

event: final
data: {"section":"answer","markdown":"…full text…","citations":["PROD-0012","PROD-0014"],"insufficientEvidence":false,"warnings":[]}

event: done
data: {"timeToFirstTokenMs":640,"totalMs":5210,"trace":[…]}
```

- Failures send `event: error` with a ProblemDetails body (e.g. `503` "Is Ollama running?"). The results request is unaffected.
- Closing the connection cancels generation; the request's `CancellationToken` flows into `IChatClient`.
- The endpoint disables response buffering and compression, so chunks reach the browser as they're produced.

### Validation after generation (on the full text, reported in `final`)

- **Citations:**
  - Extract every `[PROD-nnnn]`. Each must be in the evidence set.
  - Unknown IDs produce a warning (`Cited PROD-0099, which was not in the evidence`). The UI marks that chip as invalid.
  - The text has already been shown, so nothing is silently stripped: **the failure stays visible**.
- **Insufficient evidence:** detect the sentinel first line. The UI hides the sentinel and shows an "insufficient evidence" badge.
- **No citations** while `insufficientEvidence` is false → warning.
- **Incompatible-recommendation heuristic:** a sentence citing an Incompatible product without warning language ("avoid", "not compatible", "won't fit", …) → warning. This is a heuristic, and the trace labels it as one.
- **No retries.** The text has already streamed, and a visible warning is more honest than a silent regeneration.

### Trace

- The evidence set, and why each item was included.
- The full system and user prompts.
- The provider, model and parameters ([ADR-0015](0015-llm-hosting-and-client.md)).
- The raw output, the validation results and **timings** (time to first token, total).

### Tests

- **Unit:**
  - Evidence-set selection limits; prompt rendering (snapshot).
  - Citation extraction and validation (valid, unknown ID, none cited); the sentinel; the incompatible-recommendation heuristic.
  - SSE event formatting.
- **Integration (structural only):**
  - GQ-01 via JSON mode: the answer cites the compatible charger, every citation is in the evidence, and there are no citation warnings.
  - One test reads the SSE stream through to `done`.

## Consequences

- The UI feels fast: results in milliseconds, summary text within about a second, and both are visible together.
- Without a schema, output format isn't guaranteed. Validation after the fact, with visible warnings, covers correctness, and markdown suits small local models better than strict JSON.
- The stateless answer endpoint repeats retrieval (tens of milliseconds) to avoid server-side state.
- The evidence set is bounded, so a relevant product beyond the limits can be omitted. The trace shows exactly what the model saw.

## Alternatives considered

| Option | Why not (for this repo) |
|---|---|
| JSON-schema answer, not streamed (previous draft) | The audience watches a spinner; the schema can't render progressively |
| One response streaming results and answer together | Breaks the shared JSON contract for every stage, plus tests, Scalar and generated types |
| Streaming partial JSON | Fragile parsing on the client for little gain |
| Server-side evidence cache + generation ID | Adds state and expiry to avoid ~20 ms of deterministic retrieval |
| WebSockets / SignalR | Two-way channels for a one-way stream; SSE is simpler |
| Silently strip invalid citations | Hides the lesson that LLM output must be verified |
| Feed all candidates | Blows up small-model context; worse grounding |
| Tool/function calling for retrieval | The model deciding what to retrieve hides the pipeline we just built |

## Teaching notes

- RAG = retrieval quality + context construction + output verification. The LLM is the smallest part.
- Don't make users wait for the slow part: show retrieved results at once and stream the generated text.
- Stream the text, validate the finished text, and keep failures visible.
- "Cite or it didn't happen", and give the model the *negative* evidence (why not) as well as the positive.

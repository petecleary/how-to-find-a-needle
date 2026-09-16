# ADR-0016: Stage 6 — RAG: streamed, grounded summary with citations

- **Status:** Accepted
- **Area:** AI
- **Related:** [ADR-0003](0003-search-api-contract-and-debug-trace.md), [ADR-0004](0004-pipeline-composition.md), [ADR-0013](0013-domain-ontology-and-compatibility.md), [ADR-0014](0014-web-ui-architecture.md), [ADR-0015](0015-llm-hosting-and-client.md), [ADR-0017](0017-pedagogy-engine.md); golden queries GQ-01, GQ-05, GQ-06

## Context

After Stage 5 we have relevant candidates, their concept matches and their rule checks with reasons. Stage 6 lets an LLM answer the shopper's question in plain language, **using only that evidence**, and cite it.

Without grounding, an LLM will happily recommend the 45W barrel charger, or invent a product. Citations make the answer checkable, and let the UI link each claim to a product.

Retrieval takes milliseconds; generation takes seconds. The experience should be familiar from web search: **the results appear at once, and a written summary streams in above them.**

## Decision

### Two requests, sent together

| Request | Returns |
|---|---|
| `POST /api/search/rag` | The normal JSON response: Stage 5's results and trace, plus an `evidence` step listing the products the summary will use |
| `POST /api/search/rag/answer` | The summary as Server-Sent Events, or one JSON object with `Accept: application/json` |

The answer endpoint is **stateless**: it re-runs the Stage 5 pipeline with the same request. Retrieval is deterministic, so both requests see the same evidence.

### The evidence set: bounded and explained

- **What goes in**, in Stage 5's order:
  - the target device, if there is one;
  - up to **5 Compatible** products;
  - up to **3 Incompatible** products, **with their reasons**, so the model can warn about them;
  - up to **2 Unknown** products;
  - with no target device, what the query asked for ("65W", "USB-C") in the device's place, and a "fits which devices" line on each product, so the answer can still recommend;
  - up to **5 not-checked** products, so a query with no rules still has something to answer from.
- **Out-of-concept products are left out.** They aren't what the shopper asked for, and Stage 5 has already said so.
- **Each item is a compact labelled block:** ID, name, price, key specs, compatibility and its reasons, and a description cut to about 300 characters. Reviews are left out to save tokens and noise.
- **The matched concepts and the rules that fired go in once**, with their ontology definitions and labels, so the answer explains constraints in the domain's own words ([ADR-0013](0013-domain-ontology-and-compatibility.md)).
- The trace records every item and *why* it was included.

### The prompt

Prompts are versioned files, `assets/prompts/rag-system.md` and `rag-user.md`, not strings in C#. The system prompt's rules:

1. Answer **only** from the evidence.
2. Write **short markdown**: at most 120 words, no headings.
3. **Cite every product claim** with its ID in square brackets: `[PROD-0012]`.
4. Never recommend an Incompatible product. Mention one only to warn, with its reason.
5. Don't mention products that aren't in the evidence.
6. If the evidence doesn't answer the question, make the **first line exactly `INSUFFICIENT_EVIDENCE`**, then say what is missing.

### Streaming

```text
event: meta     data: {"stage":"rag","provider":"ollama","model":"qwen3.6:35b","evidence":["PROD-0012","PROD-0014"]}
event: delta    data: {"section":"answer","text":"The Voltline 65W USB-C charger [PROD-0012] "}
event: final    data: {"section":"answer","markdown":"…","citations":["PROD-0012","PROD-0014"],"invalidCitations":[],"insufficientEvidence":false,"warnings":[]}
event: done     data: {"timeToFirstTokenMs":73,"totalMs":2300,"trace":[…]}
```

- A failure sends `event: error` with a ProblemDetails body. `meta` is sent as soon as retrieval finishes, so an unavailable LLM usually shows up as `meta` followed by `error`. The results request is unaffected.
- Closing the connection cancels generation.
- Response buffering and compression are off, so chunks reach the browser as they are produced.

### Validation of the finished text

- **Citations:** every `[PROD-nnnn]` must be in the evidence. An unknown ID is a warning and appears in `invalidCitations`, so the UI marks exactly that chip. **The text has already been shown, so nothing is stripped: the failure stays visible.**
- **Insufficient evidence:** the sentinel line is detected, and the UI shows a badge instead.
- **No citations at all** (when evidence was sufficient) is a warning.
- **Recommending an incompatible product:** a sentence citing one without warning language ("avoid", "not compatible", "won't fit") is a warning. It is a heuristic, and the trace labels it as one.
- **No retries.** The text has already streamed; a visible warning is more honest than a silent regeneration.

## Consequences

- The UI feels fast: results in milliseconds, the first words in well under a second, both visible together.
- Without a schema, the format isn't guaranteed. Validation after the fact, with visible warnings, covers correctness, and small local models handle markdown better than strict JSON.
- The bounded evidence set can leave out a relevant product. The trace shows exactly what the model saw.
- In the model bake-off, across 210 requests, no citation ever fell outside the evidence set ([ADR-0015](0015-llm-hosting-and-client.md)).

## Alternatives considered

| Option | Why not (for this repository) |
|---|---|
| A JSON-schema answer, not streamed | The audience watches a spinner; a schema can't render as it arrives |
| One response streaming results and answer together | Breaks the shared JSON contract for every stage |
| A server-side evidence cache | Adds state and expiry to save ~20 ms of deterministic retrieval |
| WebSockets or SignalR | Two-way channels for a one-way stream |
| Silently strip invalid citations | Hides the lesson that LLM output must be checked |
| Give the model every candidate | Overflows small-model context and weakens grounding |
| Let the model call a search tool | The model deciding what to retrieve hides the pipeline we just built |

## What to take away

- **RAG is retrieval quality, context construction and output verification.** The LLM is the smallest part.
- **Don't make people wait for the slow part.** Show retrieved results at once, and stream the generated text.
- **Stream the text, validate the finished text, and keep failures visible.**
- **"Cite or it didn't happen."** And give the model the *negative* evidence (why not) as well as the positive.
- **An "I don't know" path needs a sentinel you can detect.** A fixed first line is easier to check than a sentiment.

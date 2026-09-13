# ADR-0016: Stage 7 — RAG grounding & citations

- **Status:** Proposed
- **Date:** 2026-09-13
- **Related:** ADR-0004, ADR-0013, ADR-0015, ADR-0017; golden queries GQ-01, GQ-05, GQ-06; roadmap Phase 4

## Context

After Stage 6 we have relevant candidates, their concept matches *and* domain rule checks with reasons. Stage 7 lets an LLM answer the user's question in natural language, **using only that evidence**, and cite it.

Without grounding, an LLM will happily recommend the 45W barrel charger, or invent a product. Grounding and citations make the answer checkable, and the UI can link each claim back to a product card.

## Decision

### Context packing (`IAnswerGenerator`)

- **Input:** the Stage 6 result (the whole Hybrid → Ontology pipeline, per [ADR-0004](0004-pipeline-composition.md)).
- **Evidence set** (bounded, for predictability and small-model context):
  - The target device, if resolved.
  - Up to **5 Compatible** products (by rank).
  - Up to **3 Incompatible** products **with their reasons**, so the model can warn about them.
  - Up to **2 Unknown** products.
- **Each evidence item** is rendered as a compact, labelled block: `[PROD-0012] Voltline 65W USB-C GaN Charger — £49.99 — connector: USB-C, 65W — Compatibility: Compatible (connector USB-C matches; 65W ≥ 65W)`.
- Descriptions are truncated to about 300 characters, and reviews are excluded (to save tokens and reduce noise).
- The matched concepts and the domain rules that fired are included once, with their `skos:definition` text, so the answer explains constraints in the domain's own words ([ADR-0013](0013-domain-ontology-and-compatibility.md)).

### Prompt (versioned files in `assets/prompts/rag-system.md` and `rag-user.md`)

The system prompt rules, in plain language:
1. Answer **only** from the evidence. If the evidence doesn't answer the question, set `insufficientEvidence: true` and say what is missing.
2. **Every product claim must cite** the product ID in square brackets, e.g. `[PROD-0012]`.
3. Never recommend a product marked Incompatible. Mention it only to warn, citing its reason.
4. Do not mention products that aren't in the evidence.
5. Be concise: at most 120 words.

### Output contract (JSON schema, validated server-side)

```json
{
  "answer": "The Voltline 65W USB-C charger [PROD-0012] fits your Aerobook 14 ... Avoid the 45W barrel charger [PROD-0014]: wrong connector and too little power.",
  "citations": [
    { "productId": "PROD-0012", "claim": "fits the Aerobook 14 (USB-C, 65W)" },
    { "productId": "PROD-0014", "claim": "incompatible: barrel connector, 45W" }
  ],
  "insufficientEvidence": false
}
```

### Server-side guardrails

- Parse the JSON. **On invalid JSON, retry once** with a "respond with valid JSON only" nudge, then return `502` ProblemDetails with the raw output in the trace.
- **Citation validation:**
  - Every cited ID must be in the evidence set, and every `[PROD-…]` in `answer` must appear in `citations`.
  - Invalid citations are **removed and flagged** in `answer.validation.warnings`. The response still returns, because the audience should *see* the model's failure.
- **Incompatible-recommendation check:** if a citation targets an Incompatible product and its `claim` has no warning language, flag a warning. This is a heuristic, and the trace labels it as one.
- The response's `answer` field gets `{ text, citations[], insufficientEvidence, warnings[] }`. The UI renders citation chips that scroll to the matching result card ([ADR-0014](0014-web-ui-architecture.md)).

### Trace

- The evidence set (the IDs, and why each was included).
- The full system prompt and user prompt.
- The model and parameters ([ADR-0015](0015-llm-hosting-and-client.md)).
- The raw model output, the validation results and token counts.

### Tests

- **Unit:** evidence-set selection limits; prompt rendering (snapshot); citation validator (valid, unknown ID, uncited mention, malformed JSON).
- **Integration (structural only):** for GQ-01, the answer is valid, cites only evidence IDs, cites the compatible charger, and does not recommend the incompatible one (checked via citations and `insufficientEvidence = false`).

## Consequences

- Answers are checkable: every claim links to a product card and a compatibility reason.
- Bounded evidence can omit a relevant product beyond the limits. The trace shows exactly what the model saw.
- Small models will sometimes break the rules. Visible warnings turn that into a teaching moment rather than a hidden bug.

## Alternatives considered

| Option | Why not (for this repo) |
|---|---|
| Feed all candidates | Blows up small-model context; worse grounding |
| Free-text answer, no JSON | Can't validate citations reliably |
| Tool/function calling for retrieval | The model deciding what to retrieve hides the pipeline we just built |
| Hide or strip invalid citations silently | Loses the lesson that LLM output must be verified |
| Prompts as C# strings | Harder to read and diff; prompt files are artefacts learners should inspect |

## Teaching notes

- RAG = retrieval quality + context construction + output verification. The LLM is the smallest part.
- "Cite or it didn't happen": citations make an answer auditable.
- Give the model the *negative* evidence (why not) as well as the positive.

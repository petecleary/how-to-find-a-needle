# ADR-0017: Stage 8 — Pedagogy engine

- **Status:** Proposed
- **Date:** 2026-09-13
- **Related:** ADR-0013, ADR-0015, ADR-0016; golden queries GQ-01, GQ-05, GQ-06; roadmap Phase 4

## Context

A grounded answer is *correct*, but not necessarily *understood*. The triad's third question is "how should I explain it?". Users choosing a charger need to understand the *concept* behind the constraint: why connector type matters, what "65W minimum" means. Then they can make this decision, and the next one, confidently.

Stage 8 turns grounded facts into an explanation shaped by teaching principles. It is a separate layer from RAG, so the talk can show that **explanation is a design choice, not a side effect of the model**.

## Decision

### Position in the pipeline

`IPedagogyEngine` consumes the **Stage 7 result**: the validated answer and citations, plus the evaluated candidates and compatibility reasons ([ADR-0004](0004-pipeline-composition.md)).
- It may **not** introduce new products or facts. Its input is the RAG output, the evidence set and the ontology reasons only.
- This means a second LLM call. The latency is visible in the trace and accepted.

### Audience

`options.audience` (`novice` by default, then `enthusiast`, `expert`) changes vocabulary and depth:
- **novice:** plain words, define terms, one analogy allowed.
- **enthusiast:** name the standards (USB-C PD), fewer definitions.
- **expert:** terse and spec-first.

The presenter can switch audience live to show the same facts explained three ways.

### Pedagogical framing (in `assets/prompts/pedagogy-system.md`)

The prompt encodes a small, explicit set of principles:
1. **Lead with the decision.** One sentence: what to choose.
2. **Explain the constraint, not just the verdict.** Name the underlying concept (connector, power budget, platform, SSD interface) and why it matters.
3. **Contrast with the near miss.** Use the incompatible look-alike as a worked counter-example ("it looks the same, but…").
4. **Make it transferable.** One rule of thumb the user can apply next time ("check the watts on your laptop's original charger and match or exceed them").
5. **Next step.** A concrete action or question to check.
6. **Stay grounded.** Every product mention cites `[PROD-…]`; no new facts.

### Output contract (JSON schema, validated like Stage 7)

```json
{
  "decision": { "productId": "PROD-0012", "summary": "Get the Voltline 65W USB-C charger." },
  "concepts": [
    { "term": "Connector type", "explanation": "The plug has to physically match your laptop's port..." },
    { "term": "Wattage", "explanation": "A charger must supply at least the power the laptop asks for..." }
  ],
  "nearMiss": { "productId": "PROD-0014", "whyItLooksRight": "Same size and colour, also a laptop charger.", "whyItIsNot": "Barrel plug and only 45W." },
  "ruleOfThumb": "Match the connector, then match or exceed the wattage.",
  "nextStep": "Check the 'W' rating printed on your current charger.",
  "citations": [ { "productId": "PROD-0012", "claim": "..." } ]
}
```

- Validation reuses the Stage 7 citation validator ([ADR-0016](0016-rag-grounding-and-citations.md)). In addition, `decision.productId` must be a Compatible product, or null when `insufficientEvidence` came through from Stage 7.
- The response's `explanation` field carries this object plus `warnings[]`. The UI's `ExplanationCard` renders it as a small, scannable lesson: decision → concepts → near miss → rule of thumb → next step.

### Trace

- The pedagogy system prompt, highlighting the active audience section.
- The input facts passed from Stage 7, the raw output, validation, and the model and timing.
- A note contrasting this output with Stage 7's answer: same facts, different framing.

### Tests

- **Unit:** prompt rendering per audience; validator rules (decision must be Compatible, near miss must be Incompatible, no uncited IDs).
- **Integration (structural):** GQ-01 produces a decision on the compatible charger, a `nearMiss` pointing at the incompatible charger, and at least one concept.

## Consequences

- The talk ends on its thesis: search finds, the ontology constrains, and pedagogy explains. Each layer is visible and swappable.
- A second model call adds latency (a few seconds locally). A warm model and small outputs keep it acceptable for the demo.
- The pedagogy principles are an opinionated, short list, deliberately simple enough to explain on one slide.

## Alternatives considered

| Option | Why not (for this repo) |
|---|---|
| Merge into the Stage 7 prompt | Hides the distinction between *correct* and *understood*; the talk needs to show both |
| Free-form explanation text | Can't validate, can't render as a structured lesson |
| Pedagogy without RAG (straight from ontology facts) | Faster, but loses the "builds on the previous stage" pipeline story |
| Adaptive tutoring (multi-turn quizzes) | Beyond the scope of a 30-minute talk; a "going further" idea |

## Teaching notes

- Explanation is information architecture for the user's mind: decision, concept, counter-example, rule, action.
- Using the near miss as a counter-example turns the system's hardest case into the user's clearest lesson.

# ADR-0017: Stage 8 — Pedagogy engine (streamed)

- **Status:** Proposed
- **Date:** 2026-09-13
- **Related:** ADR-0003, ADR-0013, ADR-0014, ADR-0015, ADR-0016; golden queries GQ-01, GQ-05, GQ-06; roadmap Phase 4

## Context

A grounded answer is *correct*, but not necessarily *understood*. The triad's third question is "how should I explain it?". Users choosing a charger need to understand the *concept* behind the constraint: why connector type matters, what "65W minimum" means. Then they can make this decision, and the next one, confidently.

Stage 8 turns grounded facts into an explanation shaped by teaching principles. It is a separate layer from RAG, so the talk can show that **explanation is a design choice, not a side effect of the model**.

Like Stage 7, it streams: results appear immediately, and the explanation writes itself in the summary panel ([ADR-0016](0016-rag-grounding-and-citations.md)).

## Decision

### Requests and position in the pipeline

| Request | Returns |
|---|---|
| `POST /api/search/pedagogy` | The normal JSON `SearchResponse`: the Stage 6 pipeline's results, trace and evidence step. Renders immediately |
| `POST /api/search/pedagogy/answer` | SSE (or JSON when `Accept: application/json`) with **two sections in order**: `answer` (the Stage 7 summary, generated and validated exactly as in ADR-0016), then `explanation` |

- The explanation is generated **after the answer's `final` event**. `IPedagogyEngine` receives:
  - the validated answer;
  - the evidence set and ontology reasons;
  - the `skos:definition` text of each matched concept and fired rule ([ADR-0013](0013-domain-ontology-and-compatibility.md)).
- It may **not** introduce new products or facts. **Concept explanations must build on the ontology definitions**, so the lesson rests on the domain model rather than on what the model happens to believe.
- There are two LLM calls per request. Streaming keeps the panel alive throughout, and the trace shows the timings of each.

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
4. **Make it transferable.** One rule of thumb the user can apply next time.
5. **Next step.** A concrete action or check.
6. **Stay grounded.** Every product mention cites `[PROD-…]`; no new facts.

### Output: markdown with fixed headings, in this order

```markdown
## Decision
Get the Voltline 65W USB-C charger [PROD-0012].

## Concepts
- **Connector type**: the plug has to physically match your laptop's charging port…
- **Wattage**: a charger must supply at least the power the laptop asks for…

## Near miss
The Voltline 45W barrel adapter [PROD-0014] looks almost identical, but it has the wrong plug and too little power.

## Rule of thumb
Match the connector, then match or exceed the wattage.

## Next step
Check the "W" rating printed on your current charger.
```

- Chunks stream as `delta` events with `section: "explanation"`. The UI renders headings and bullets as they arrive.
- **Validation on completion** (reported in the explanation's `final` event):
  - All five headings are present, in order; missing or reordered headings produce a warning.
  - Citations are checked against the evidence set (the shared validator from ADR-0016).
  - **Decision** cites exactly one product, and it must be **Compatible**. The exception: if the answer reported insufficient evidence, Decision must say no suitable product was found.
  - **Near miss** cites an **Incompatible** or out-of-concept product, or says "None" when the evidence has none.
  - **Concepts:** each bolded term should match a matched concept's or fired rule's label. Anything else gets a heuristic "concept not from the ontology" warning.
- `final` also carries the **parsed structure** for tests and the UI: `{ decision: { productId }, concepts: [term], nearMiss: { productId }, ruleOfThumb, nextStep }`.

### Trace

- The pedagogy system prompt, highlighting the active audience section.
- The inputs passed from Stage 7 and the ontology.
- The raw output and validation.
- **Per-section timings:** time to first token for the answer and for the explanation, and total time.
- A note contrasting the two sections: same facts, different framing.

### Tests

- **Unit:** heading parser (complete, missing and out-of-order cases); prompt rendering per audience; validator rules (Decision Compatible, Near miss Incompatible, uncited IDs, concept-label heuristic).
- **Integration (structural, via JSON mode):** GQ-01 produces a Decision on the compatible charger, a Near miss on the 45W barrel charger, and at least one concept.

## Consequences

- The talk ends on its thesis: search finds, the ontology constrains, and pedagogy explains. Each layer is visible and swappable.
- Streaming keeps a two-call stage responsive: the audience reads the answer while the explanation starts.
- Parsing headings is more fragile than a schema. Warnings make any drift visible, and fixed markdown headings are easy for small local models to follow.
- The pedagogy principles are an opinionated, short list, deliberately simple enough to explain in one talk step.

## Alternatives considered

| Option | Why not (for this repo) |
|---|---|
| JSON-schema explanation (previous draft) | Can't render progressively; a spinner for several seconds |
| Merge into the Stage 7 prompt | Hides the distinction between *correct* and *understood*; the talk needs to show both |
| Free-form markdown without fixed headings | Can't validate the decision or near miss; inconsistent across runs |
| Pedagogy without the RAG answer section | Faster, but loses the "builds on the previous stage" story in the panel |
| Adaptive tutoring (multi-turn quizzes) | Beyond the scope of a 30-minute talk; a "going further" idea |

## Teaching notes

- Explanation is information architecture for the user's mind: decision, concept, counter-example, rule, action.
- Using the near miss as a counter-example turns the system's hardest case into the user's clearest lesson.
- A visible structure (fixed headings) is a contract you can stream *and* validate.

# ADR-0017: Stage 7 — Pedagogy engine (streamed)

- **Status:** Proposed
- **Date:** 2026-09-13 (amended 2026-09-14 by [ADR-0018](0018-scope-and-going-further.md): renumbered from Stage 8; adds the baseline toggle and audience-aware labels)
- **Related:** ADR-0003, ADR-0013, ADR-0014, ADR-0015, ADR-0016, ADR-0018; golden queries GQ-01, GQ-05, GQ-06; roadmap Phase 4

## Context

A grounded answer is *correct*, but not necessarily *understood*. The triad's third question is "how should I explain it?". Users choosing a charger need to understand the *concept* behind the constraint: why connector type matters, what "65W minimum" means. Then they can make this decision, and the next one, confidently.

Stage 7 turns grounded facts into an explanation shaped by teaching principles. It is a separate layer from RAG, so the talk can show that **explanation is a design choice, not a side effect of the model**.

To *show* that, the talk needs a fair comparison.
- Comparing Stage 6's answer with Stage 7's explanation changes several things at once: a second call, an audience and a different prompt. The audience can't tell which one made the difference.
- A plain LLM asked to "explain this for a beginner" also changes its tone. The question is what pedagogical design adds *on top of* that.

Like Stage 6, it streams: results appear immediately, and the explanation writes itself in the summary panel ([ADR-0016](0016-rag-grounding-and-citations.md)).

## Decision

### Requests and position in the pipeline

| Request | Returns |
|---|---|
| `POST /api/search/pedagogy` | The normal JSON `SearchResponse`: the Stage 5 pipeline's results, trace and evidence step. Renders immediately |
| `POST /api/search/pedagogy/answer` | SSE (or JSON when `Accept: application/json`) with **two sections in order**: `answer` (the Stage 6 summary, generated and validated exactly as in ADR-0016), then `explanation` |

- The explanation is generated **after the answer's `final` event**. `IPedagogyEngine` receives:
  - the validated answer;
  - the evidence set and ontology reasons;
  - the `skos:definition` text of each matched concept and fired rule, and each concept's English `prefLabel` and `altLabel`s ([ADR-0013](0013-domain-ontology-and-compatibility.md));
  - `options.audience` and `options.applyPedagogy`.
- It may **not** introduce new products or facts. **Concept explanations must build on the ontology definitions**, so the lesson rests on the domain model rather than on what the model happens to believe.
- There are two LLM calls per request. Streaming keeps the panel alive throughout, and the trace shows the timings of each.

### Audience

`options.audience` (`novice` by default, then `enthusiast`, `expert`) changes vocabulary and depth. Part of that vocabulary comes straight from the ontology:

| Audience | Style | Words for concepts |
|---|---|---|
| `novice` | Plain words, define terms, one analogy allowed | An everyday `altLabel` where one exists ("power brick"), with the preferred label given once ("a laptop charger") |
| `enthusiast` | Name the standards (USB-C PD), fewer definitions | Preferred labels |
| `expert` | Terse and spec-first | Preferred labels and spec terms (connector, wattage, interface) |

- `hiddenLabel` values (misspellings) are never given to the model.
- The audience is chosen once, in the search bar, and sent with every request like any other option. Only Stage 7 reads it, and each stage's trace lists the options it used, so this is visible ([ADR-0003](0003-search-api-contract-and-debug-trace.md), [ADR-0014](0014-web-ui-architecture.md)).
- The presenter can switch audience live to show the same facts explained three ways.

### The baseline toggle: `options.applyPedagogy` (default `true`)

This is Stage 7's before/after switch, like Stage 5's `expandSynonyms` and `applyConstraints`. **Only the prompt changes.**

| | `applyPedagogy: false` (baseline) | `applyPedagogy: true` |
|---|---|---|
| Inputs | Validated answer, evidence set, ontology definitions and labels, audience | Identical |
| Prompt | `assets/prompts/pedagogy-baseline.md`: the reasonable prompt a developer would write first: explain the answer to a *{audience}*, use only the evidence, cite products as `[PROD-…]` | `assets/prompts/pedagogy-system.md`: the principles and fixed headings below |
| Output | Free-form markdown | Five fixed headings |
| Validation | Citations only | Citations and structure checks |

- The grounding rules (evidence only, cite every product, no new products) are in **both** prompts. The comparison isolates teaching design and doesn't re-run the RAG lesson from Stage 6.
- **The baseline must not be a straw man.** It's a sensible, typical prompt, and the trace shows it in full, so the audience can judge the comparison for themselves.
- The talk sequence for GQ-01:
  1. `novice` with the toggle off;
  2. the same with the toggle on;
  3. switch audience with the toggle on.

### Pedagogical framing (in `assets/prompts/pedagogy-system.md`)

The prompt encodes a small, explicit set of principles:
1. **Lead with the decision.** One sentence: what to choose.
2. **Explain the constraint, not just the verdict.** Name the underlying concept (connector, power budget, platform, SSD interface) and why it matters.
3. **Contrast with the near miss.** Use the incompatible look-alike as a worked counter-example ("it looks the same, but…").
4. **Make it transferable.** One rule of thumb the user can apply next time.
5. **Next step.** A concrete action or check.
6. **Stay grounded.** Every product mention cites `[PROD-…]`; no new facts.

### Output (pedagogy on): markdown with fixed headings, in this order

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

- Chunks stream as `delta` events with `section: "explanation"`, whichever prompt is used. The UI renders headings and bullets as they arrive.
- **Validation on completion** (reported in the explanation's `final` event):
  - **Always:** citations are checked against the evidence set (the shared validator from ADR-0016).
  - **Pedagogy on only:**
    - All five headings are present, in order; missing or reordered headings produce a warning.
    - **Decision** cites exactly one product, and it must be **Compatible**. The exception: if the answer reported insufficient evidence, Decision must say no suitable product was found.
    - **Near miss** cites an **Incompatible** or out-of-concept product, or says "None" when the evidence has none.
    - **Concepts:** each bolded term should match a matched concept's or fired rule's label. Anything else gets a heuristic "concept not from the ontology" warning.
  - **Baseline:** the structure checks are listed in the trace as *not applied (baseline prompt)*.
- `final` also carries the **parsed structure** for tests and the UI: `{ decision: { productId }, concepts: [term], nearMiss: { productId }, ruleOfThumb, nextStep }`. It is `null` for the baseline.

### Trace

- Whether pedagogy was applied, and the prompt file used, shown in full. With pedagogy on, the active audience section is highlighted.
- The inputs passed from Stage 6 and the ontology, including the labels offered for the audience.
- The raw output and validation.
- **Per-section timings:** time to first token for the answer and for the explanation, and total time.
- A note contrasting the two sections, and for the baseline: *"Same facts, same audience, no pedagogical structure."*

### Tests

- **Unit:**
  - Heading parser: complete, missing and out-of-order cases.
  - Prompt selection by `applyPedagogy`; prompt rendering per audience.
  - Label choice: the novice prompt includes alternative labels, and no prompt includes hidden labels.
  - Validator rules: Decision Compatible, Near miss Incompatible, uncited IDs, the concept-label heuristic. The baseline skips structure checks.
- **Integration (structural, via JSON mode):**
  - GQ-01 with pedagogy on produces a Decision on the compatible charger, a Near miss on the 45W barrel charger, and at least one concept.
  - GQ-01 with pedagogy off completes with no citation warnings and `structure: null`.

## Consequences

- The talk ends on its thesis: search finds, the ontology constrains, and pedagogy explains. Each layer is visible and swappable.
- The talk can show *what pedagogy adds*, not just that the explanation is different: same model, same facts, same audience, different design.
- The ontology does double duty: its labels shape the explanation's vocabulary as well as the search.
- Streaming keeps a two-call stage responsive: the audience reads the answer while the explanation starts.
- Parsing headings is more fragile than a schema. Warnings make any drift visible, and fixed markdown headings are easy for small local models to follow.
- The pedagogy principles are an opinionated, short list, deliberately simple enough to explain in one talk step.
- Two prompt files to maintain, and the baseline must stay a fair one.

## Alternatives considered

| Option | Why not (for this repo) |
|---|---|
| JSON-schema explanation (previous draft) | Can't render progressively; a spinner for several seconds |
| Merge into the Stage 6 prompt | Hides the distinction between *correct* and *understood*; the talk needs to show both |
| Free-form markdown without fixed headings | Can't validate the decision or near miss; inconsistent across runs |
| Pedagogy without the RAG answer section | Faster, but loses the "builds on the previous stage" story in the panel |
| Compare Stage 6's answer with Stage 7's explanation, with no toggle | Changes the prompt, the audience and the number of calls at once; the audience can't tell what made the difference |
| A baseline that ignores the audience | Mixes up "adapting to the reader" with "teaching design"; a plain prompt can already adapt tone |
| An ungrounded baseline (no evidence) | Brings the RAG lesson back in; hallucination is Stage 6's story |
| Audience from a request header (e.g. `X-User-Role`) | Breaks the same-request-body contract (ADR-0003) |
| Comparison tables in the output | Harder for small local models to stream and to validate than fixed headings |
| Adaptive tutoring (multi-turn quizzes) | Beyond the scope of a 30-minute talk; a going-further topic ([ADR-0018](0018-scope-and-going-further.md)) |

## Teaching notes

- Explanation is information architecture for the user's mind: decision, concept, counter-example, rule, action.
- Using the near miss as a counter-example turns the system's hardest case into the user's clearest lesson.
- A visible structure (fixed headings) is a contract you can stream *and* validate.
- A fair demo changes one thing. The baseline gets the same facts and the same audience; only the teaching design differs.
- Adapting to an audience is partly vocabulary, and the ontology already holds it: alternative labels for novices, preferred labels and spec terms for experts.

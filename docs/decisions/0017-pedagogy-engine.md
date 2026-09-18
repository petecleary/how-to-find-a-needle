# ADR-0017: Stage 7 — Pedagogy engine, with a baseline toggle

- **Status:** Accepted
- **Area:** AI
- **Related:** [ADR-0003](0003-search-api-contract-and-debug-trace.md), [ADR-0013](0013-domain-ontology-and-compatibility.md), [ADR-0014](0014-web-ui-architecture.md), [ADR-0015](0015-llm-hosting-and-client.md), [ADR-0016](0016-rag-grounding-and-citations.md), [ADR-0018](0018-scope-and-going-further.md); golden queries GQ-03, GQ-05, GQ-06

## Context

A grounded answer is *correct*, but not necessarily *understood*. The talk's third question is *how should I explain it?* Someone choosing a charger needs the concept behind the constraint (why the plug matters, what "65W minimum" means) to make this decision, and the next one, with confidence.

Stage 7 turns grounded facts into an explanation shaped by teaching principles. It is a separate layer from RAG, to show that **explanation is a design choice, not a side effect of the model**.

Showing that needs a **fair comparison**. Comparing Stage 6's answer with Stage 7's explanation changes several things at once: a second call, an audience and a different prompt. And a plain "explain this for a beginner" prompt already changes tone. The question is what teaching design adds *on top of that*.

## Decision

### Position in the pipeline

`POST /api/search/pedagogy` returns results at once, like Stage 6. `POST /api/search/pedagogy/answer` streams **two sections in order**: `answer` (Stage 6's summary, generated and validated exactly as in [ADR-0016](0016-rag-grounding-and-citations.md)), then `explanation`.

The explanation is generated after the answer is validated. It receives the answer, the evidence set, the rule reasons, the ontology definitions and labels of the matched concepts, and the audience. **It may not introduce products or facts**, and concept explanations must build on the ontology's definitions.

### Audience

`options.audience` is `novice` (the default), `enthusiast` or `expert`. Part of the vocabulary comes straight from the ontology:

| Audience | Style | Words for concepts |
|---|---|---|
| `novice` | Plain words, terms defined, one analogy allowed | An everyday alternative label where one exists ("power brick"), with the preferred label given once |
| `enthusiast` | Names the standards (USB-C PD), fewer definitions | Preferred labels |
| `expert` | Terse and spec-first | Preferred labels and spec terms (connector, wattage, interface) |

Misspellings (`skos:hiddenLabel`) are never given to the model. The audience is chosen once in the UI and sent with every request; only Stage 7 reads it, and the trace says so.

### The baseline toggle: `options.applyPedagogy`

Stage 7's before-and-after switch. **Only the system prompt changes.**

| | Baseline (`applyPedagogy: false`) | Pedagogy (`applyPedagogy: true`) |
|---|---|---|
| User message | `pedagogy-user.md`: question, validated answer, evidence, concepts, rules and the audience's words | **Identical** |
| System prompt | `pedagogy-baseline.md`: the sensible prompt a developer writes first. Explain the answer to a *{audience}*, use only the evidence, cite products | `pedagogy-system.md`: the principles and fixed headings below, plus the audience's section from `pedagogy-audiences.md` |
| Output | Free-form markdown | Five fixed headings |
| Validation | Citations | Citations and structure |

- **The grounding rules are in both prompts**, so the comparison isolates teaching design rather than re-running Stage 6's lesson.
- **The baseline must not be a straw man.** It is a reasonable first prompt, and the trace shows it in full so the audience can judge the comparison themselves.
- The talk runs GQ-03 three ways: novice with pedagogy off; novice with pedagogy on; expert with pedagogy on.

### Teaching principles (in `pedagogy-system.md`)

1. **Lead with the decision:** one sentence, what to choose.
2. **Explain the constraint, not just the verdict:** name the concept and why it matters.
3. **Contrast with the near miss:** the incompatible look-alike is a worked counter-example.
4. **Make it transferable:** one rule of thumb for next time.
5. **Give a next step:** one concrete action or check.
6. **Stay grounded:** cite every product; add no facts.

### Output: markdown with fixed headings

```markdown
## Decision
Get the Voltline 65W USB-C charger [PROD-0012].

## Concepts
- **Connector type**: the plug has to match your laptop's charging port…
- **Wattage**: a charger must supply at least the power the laptop asks for…

## Near miss
The Voltline 45W barrel charger [PROD-0014] looks almost identical, but it has the wrong plug and too little power.

## Rule of thumb
Match the connector, then match or exceed the wattage.

## Next step
Check the "W" rating printed on your current charger.
```

- **The heading parser is tolerant about form and strict about content.** `## Decision`, `### Decision` and a line that is only `**Decision**` all count, because small models vary the markup. Missing or out-of-order headings are warnings.
- **Checks when pedagogy is on:**
  - All five headings, in order.
  - **Decision** cites exactly one product, and it is **Compatible**, unless the answer reported insufficient evidence, when it must say nothing suitable was found.
  - **Near miss** cites an **Incompatible** product, or says "None" when there isn't one.
  - **Concepts:** a bolded term that isn't a matched concept's or rule's label gets a heuristic "concept not from the ontology" warning.
- With the baseline, the structure checks are listed in the trace as *not applied*.
- The `final` event carries the parsed structure (decision, concepts, near miss, rule of thumb, next step) for the UI and tests.

## Consequences

- The talk ends on its thesis: search finds, the ontology constrains, pedagogy explains. Each layer is visible and can be switched.
- It can show *what* pedagogy adds, not just that the text is different: same model, same facts, same audience, different design.
- The ontology does double duty: its labels choose the explanation's words as well as widening the search.
- Two LLM calls per request. Streaming keeps it responsive: the audience reads the answer while the explanation starts. In the bake-off, both sections finished in about 5 seconds (median) with the structure checks passing in 65 of 70 runs ([ADR-0015](0015-llm-hosting-and-client.md)).
- Parsing headings is more fragile than a schema; warnings make drift visible.
- Two system prompts to maintain, and the baseline has to stay fair.

## Alternatives considered

| Option | Why not (for this repository) |
|---|---|
| A JSON-schema explanation | Can't render as it arrives; several seconds of spinner |
| Merge into Stage 6's prompt | Hides the difference between *correct* and *understood* |
| Free-form markdown, no fixed headings | Can't check the decision or the near miss; inconsistent between runs |
| Compare Stage 6 with Stage 7, no toggle | Changes prompt, audience and number of calls at once |
| A baseline that ignores the audience | Confuses adapting tone with teaching design |
| An ungrounded baseline | Brings hallucination back in, which is Stage 6's story |
| Audience from a request header | Breaks the same-request contract ([ADR-0003](0003-search-api-contract-and-debug-trace.md)) |
| Multi-turn adaptive tutoring | Beyond a 30-minute talk; part of the going-further step ([ADR-0018](0018-scope-and-going-further.md)) |

## What to take away

- **Explanation is information architecture for the reader's mind:** decision, concept, counter-example, rule, action.
- **The near miss is the best teaching example.** The system's hardest case becomes the reader's clearest lesson.
- **A visible structure is a contract you can stream and validate.**
- **A fair demo changes one thing.** Same facts, same audience, same words; only the teaching design differs.
- **Adapting to an audience is partly vocabulary, and the ontology already holds it:** everyday labels for novices, preferred labels and spec terms for experts.

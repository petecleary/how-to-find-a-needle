Stage 6 checks that every [citation](term:citation) names a product in the [evidence set](term:evidence-set). That is the floor, not the ceiling.

### Measuring answers, not just rankings

[Precision](term:precision) and [recall](term:recall) grade a ranking. An answer needs its own measures, and the field has settled on three ([RAG metrics](term:rag-metrics)):

- **Context recall** — did retrieval actually fetch the facts the answer needed? A wrong answer is usually a retrieval failure wearing a language model's voice.
- **Faithfulness** — is every claim supported by the evidence given? This is the measurable version of "no hallucinations".
- **Answer relevance** — does it address the question that was asked, rather than a nearby one?

RAGAS and TruLens — open-source Python libraries built to compute exactly these three — do it by asking a second, usually larger, model to judge each answer against the evidence, which means your evaluation inherits a model's opinions too. Our nine [golden queries](term:golden-query) are the small, honest version: a fixed set of questions whose right answers a human decided in advance ([ADR-0005 · Golden queries](adr:0005-curated-dataset-and-golden-queries)).

Whichever you use, the rule holds: **evaluation data is part of the search system.** Without it, "better" is an opinion.

### Verifying more than the identifiers

We check that `[PROD-0042]` exists in the evidence. A stricter system checks that the _claim_ attached to the citation matches the evidence — that "65W" appears in the row cited, that a product described as compatible was not flagged. Beyond that sit guardrails: refusing to answer when the evidence is thin, saying "I don't know" as a first-class outcome, and showing confidence honestly rather than uniformly.

### Letting the model choose what to retrieve

Agentic retrieval gives the model tools and lets it decide what to search for, and how many times. It reaches answers a single retrieval pass cannot.

We reject it here, deliberately. This talk's argument is that retrieval quality is the thing that matters, and a model that chooses its own evidence makes retrieval unobservable — you can no longer point at a trace and say _this_ is why that product appeared ([ADR-0016 · RAG, grounding and citations](adr:0016-rag-grounding-and-citations)). Build the retrieval you can inspect first; then decide whether a model should be allowed to drive it.

### If a model does drive: every edge is a contract

In an [agent loop](term:agent-loop) the model doesn't only answer; it proposes the next step. Each step is a tool call: JSON naming a tool and its arguments. Some tools read — run a search. Some act — add to a basket, raise a refund. Code runs the call, the result goes back to the model, and the loop goes round until the model says it is done.

Turn the loop on its side and it is a chain: plan → call → result → plan → call. Our pipeline is already one — retrieval → ontology → evidence set → LLM → citation check — and every arrow is a hand-off. Each hand-off is a place to check. Instead of one guardrail around the whole model, put a contract on every edge.

The ontology is the language those contracts are written in. In Stage 5 it guards what the model _sees_: a charger that fails a [domain rule](term:domain-rule) reaches the evidence set flagged, never as a match. In an agent loop the same rule guards what the model _does_:

```text
Model proposes:   addToBasket { product: PROD-0014, forDevice: PROD-0001 }
Ontology checks:  connector  has: barrel-5.5mm  needs: usb-c   ✗
                  wattageW   has: 45            needs: ≥ 65    ✗
Result:           rejected, with reasons — sent back to the model, which plans again
```

Write the rule once, in the ontology's vocabulary, and any engine can enforce it: our C# checks today, a [SHACL](term:shacl) shape on the call's JSON tomorrow. The model proposes; the rules decide. A rejected call isn't hidden either: its reasons become the model's next observation, and they belong in the trace, just as Stage 5's flagged candidates stay on screen.

Pairing a probabilistic model with explicit rules like this is often called _neuro-symbolic_ AI. The framing of "every edge is a contract" is from Dr. C's [Why agentic systems need ontologies](https://www.youtube.com/watch?v=4Z4ie_MQOTw). It doesn't change the decision above — build the retrieval you can inspect first — but if a model is ever allowed to act, the ontology is what keeps each step inspectable ([ADR-0018 · Scope](adr:0018-scope-and-going-further)).

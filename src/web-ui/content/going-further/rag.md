Stage 6 checks that every [citation](term:citation) names a product in the [evidence set](term:evidence-set). That is the floor, not the ceiling.

### Measuring answers, not just rankings

[Precision](term:precision) and [recall](term:recall) grade a ranking. An answer needs its own measures, and the field has settled on three ([RAG metrics](term:rag-metrics)):

- **Context recall** — did retrieval actually fetch the facts the answer needed? A wrong answer is usually a retrieval failure wearing a language model's voice.
- **Faithfulness** — is every claim supported by the evidence given? This is the measurable version of "no hallucinations".
- **Answer relevance** — does it address the question that was asked, rather than a nearby one?

RAGAS and TruLens compute these, usually by asking a second model to judge, which means your evaluation inherits a model's opinions too. Our nine [golden queries](term:golden-query) are the small, honest version: a fixed set of questions whose right answers a human decided in advance ([ADR-0005 · Golden queries](adr:0005-curated-dataset-and-golden-queries)).

Whichever you use, the rule holds: **evaluation data is part of the search system.** Without it, "better" is an opinion.

### Verifying more than the identifiers

We check that `[PROD-0042]` exists in the evidence. A stricter system checks that the _claim_ attached to the citation matches the evidence — that "65W" appears in the row cited, that a product described as compatible was not flagged. Beyond that sit guardrails: refusing to answer when the evidence is thin, saying "I don't know" as a first-class outcome, and showing confidence honestly rather than uniformly.

### Letting the model choose what to retrieve

Agentic retrieval gives the model tools and lets it decide what to search for, and how many times. It reaches answers a single retrieval pass cannot.

We reject it here, deliberately. This talk's argument is that retrieval quality is the thing that matters, and a model that chooses its own evidence makes retrieval unobservable — you can no longer point at a trace and say _this_ is why that product appeared ([ADR-0016 · RAG, grounding and citations](adr:0016-rag-grounding-and-citations)). Build the retrieval you can inspect first; then decide whether a model should be allowed to drive it.

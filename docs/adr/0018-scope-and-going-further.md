# ADR-0018: Scope — seven stages, what the talk discusses, and what it leaves out

- **Status:** Proposed
- **Date:** 2026-09-14
- **Related:** ADR-0001, ADR-0003, ADR-0004, ADR-0006, ADR-0010, ADR-0011, ADR-0012, ADR-0013, ADR-0014, ADR-0016, ADR-0017; roadmap Phases 0–5

## Context

This is a 30-minute talk with a 4-week build. After Phase 2 we reviewed the ontology, pedagogy and "beyond the talk" topics. Three things came out of it:

1. **BGE-M3 doesn't earn its build cost.** It overlaps Stage 4 (dense + sparse fused with RRF), GQ-07's multilingual moment is already handled by ontology labels, and its unique lessons can be explained without code ([ADR-0012](0012-bge-m3-dense-and-sparse.md)).
2. **The ontology stage lands best as taxonomy and vocabulary.** It works when framed as SKOS first, with OWL and graphs as the horizon, not as "knowledge graphs" ([ADR-0013](0013-domain-ontology-and-compatibility.md)).
3. **The pedagogy stage proves the thesis only with a fair comparison.** The fair comparison is the same facts and the same audience, with and without pedagogical design ([ADR-0017](0017-pedagogy-engine.md)).

There are also many adjacent topics a developer will meet next: chunking, re-ranking, query rewriting, knowledge graphs, evaluation. Building them would blur the pipeline. Naming none of them would leave learners without a map.

## Decision

### 1. Seven built stages, numbered without a gap

| Stage | Slug | Decision |
|---|---|---|
| 1 Structured | `structured` | [ADR-0007](0007-structured-search.md) |
| 2 Keyword | `keyword` | [ADR-0008](0008-keyword-search-bm25-style.md) |
| 3 Vector | `vector` | [ADR-0009](0009-local-embeddings-onnx-runtime.md), [ADR-0010](0010-vector-search-pgvector.md) |
| 4 Hybrid | `hybrid` | [ADR-0011](0011-hybrid-search-rrf.md) |
| 5 Ontology | `ontology` | [ADR-0013](0013-domain-ontology-and-compatibility.md) |
| 6 RAG | `rag` | [ADR-0016](0016-rag-grounding-and-citations.md) |
| 7 Pedagogy | `pedagogy` | [ADR-0017](0017-pedagogy-engine.md) |

- **BGE-M3 is not built.** [ADR-0012](0012-bge-m3-dense-and-sparse.md) is **Rejected**. The following are removed:
  - the `bge-m3` route and stage slug;
  - the `bgeDenseRank` / `bgeSparseRank` signals ([ADR-0003](0003-search-api-contract-and-debug-trace.md));
  - the BGE-M3 columns and indexes ([ADR-0006](0006-database-schema-and-seeding.md));
  - the BGE-M3 model download instructions.
- **Stages are renumbered:** Ontology 6 → 5, RAG 7 → 6, Pedagogy 8 → 7. Slugs and routes don't change. ADR numbers don't change either, because they number decisions, not stages.
- Following [ADR-0001](0001-record-architecture-decisions.md), the affected ADRs (0002–0011, 0013–0017) are amended in place. ADR-0003 and ADR-0006 go back to **Proposed** because their code must change.

### 2. Changes to built and planned stages

- **Stage 5, Ontology:** framed SKOS-first. Taxonomy, synonyms, language-tagged labels and value vocabularies are standard SKOS. The compatibility rules are presented as the smallest step *beyond* SKOS ([ADR-0013](0013-domain-ontology-and-compatibility.md)). No behaviour change.
- **Stage 7, Pedagogy:**
  - `options.applyPedagogy` adds a baseline toggle.
  - The explanation's wording uses the ontology's labels chosen for the audience ([ADR-0017](0017-pedagogy-engine.md)).
  - The audience picker sits in the search bar ([ADR-0014](0014-web-ui-architecture.md)).
  - Stage 7 moves from **Should** to **Must** in the roadmap.

### 3. Discussed, not built: the "Going further" step

Topics fall into three tiers: **built**, **discussed** and **out of scope**. A discussed topic gets talk content: one row in the going-further talk step, a glossary entry, and at most a line in the relevant stage explanation or ADR teaching notes. It gets **no code, packages, endpoints or data**. Promoting a topic to "built" needs an ADR change first.

| Where in the pipeline | Topic | Why a developer meets it | Mentioned at |
|---|---|---|---|
| Before retrieval | Query understanding and intent routing | Conversational queries carry noise and several intents; routing and stripping them first stops irrelevant matches | Stage 5 (our label matcher is the simplest version) |
| Before retrieval | Language detection | Choose language-specific analysers or labels before searching | Stage 2 (English-only stemming), Stage 5 (GQ-07) |
| Before retrieval | LLM query rewriting (question → search intents) | Turns exploratory questions ("I have an iPad and want to make films") into concrete searches | Stage 5, as a trade-off: more recall, less inspectable ([ADR-0013](0013-domain-ontology-and-compatibility.md) rejects it for this repo) |
| Retrieval | Chunking: fixed-size, sliding window, structure- and layout-aware | Documents such as manuals and PDFs aren't product rows; how you split them decides what can be found | Stage 3 |
| Retrieval | Multilingual and learned-sparse single-model retrieval (BGE-M3) | Dense and sparse vectors in one pass; full-sentence cross-language search | Stage 4 ([ADR-0012](0012-bge-m3-dense-and-sparse.md)) |
| Retrieval | The vector landscape: dedicated vector databases, index choices, filtering at scale | Where pgvector stops being enough | Stage 3 ([ADR-0010](0010-vector-search-pgvector.md)) |
| Ranking | Re-ranking: cross-encoders and late interaction (ColBERT) | Precision on the top candidates before they reach a user or an LLM | Stage 4 ([ADR-0011](0011-hybrid-search-rrf.md)) |
| Knowledge | OWL and reasoners, SHACL, knowledge graphs and graph databases | Formal inference, validating instance data, multi-hop relationships | Stage 5, at the rules boundary |
| Evaluation | RAG metrics: context recall, faithfulness, answer relevance (e.g. RAGAS, TruLens) | Measuring answers as well as rankings | Stage 6; golden queries are the small version |
| Explanation | Adaptive, multi-turn tutoring | Teaching over a conversation instead of one answer | Stage 7 ([ADR-0017](0017-pedagogy-engine.md)) |

- In talk mode the step comes after Stage 7 and before the summary ([ADR-0014](0014-web-ui-architecture.md)).
- It is a map, not a second talk: one step, one table, a sentence per row.

### 4. Out of scope, and not mentioned

- **Agent protocols (MCP, A2A, AG-UI / A2UI).** They standardise how agents, tools and user interfaces talk to each other. That is system integration. It doesn't answer any of the triad's questions (what is relevant, how is it related, how should I explain it). The repo already rejects letting the model decide what to retrieve ([ADR-0016](0016-rag-grounding-and-citations.md)). They appear nowhere in the code, content, glossary or talk.

## Consequences

- The stepper shows 7 stages with no gap, and the repo has one embedding model instead of two.
- Finished phases need rework, and the roadmap reopens them with the tasks listed:
  - Phase 0: models README.
  - Phase 1: schema and seeder.
  - Phase 2: contract, renumbered comments, README.
- Stage 7 becomes a Must, so Phase 4 carries more weight in week 3.
- The going-further step is content work in Phase 5. It must stay short.
- Learners who want BGE-M3 code won't find it. ADR-0012 keeps the design as a starting point.

## Alternatives considered

| Option | Why not (for this repo) |
|---|---|
| Keep BGE-M3 as an optional stage, built last | Leaves a conditional tab and a second model in the docs; the time is better spent on the pedagogy comparison |
| Remove BGE-M3 but keep the old numbers (a gap at 5) | In a finished repo, learners would see Stage 6 follow Stage 4 with no explanation |
| Build a cross-encoder re-ranking stage instead | Another model, and it improves relevance, which Stage 4 already teaches; it doesn't advance the triad |
| Include agent protocols in the going-further step | Off-thesis; spends talk time on integration standards rather than search, structure or explanation |
| No going-further step | Learners leave without knowing where the techniques they didn't see fit |
| A separate "Taxonomy" stage before "Ontology" | A stage split for framing only; SKOS-first framing inside Stage 5 does the same with no extra endpoint |

## Teaching notes

- Knowing where a technique fits in the pipeline is more useful than having seen every one run.
- Scope is a design decision. Each stage in the talk should answer one of the triad's questions.
- A before/after is only honest when one thing changes: Stage 5's toggles, and Stage 7's pedagogy toggle.

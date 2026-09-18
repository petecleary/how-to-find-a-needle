# ADR-0018: Scope — seven stages, what the talk discusses, and what it leaves out

- **Status:** Proposed
- **Date:** 2026-09-14 (amended 2026-09-18: a going-further tab per stage, and the closing step keeps only the cross-cutting topics, agreed with Pete)
- **Related:** ADR-0001, ADR-0003, ADR-0004, ADR-0005, ADR-0006, ADR-0008, ADR-0010, ADR-0011, ADR-0012, ADR-0013, ADR-0014, ADR-0016, ADR-0017; roadmap Phases 0–5

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

### 3. Discussed, not built: a "Going further" tab per stage, and a closing page

Topics fall into three tiers: **built**, **discussed** and **out of scope**. A discussed topic gets talk content: a section in its stage's going-further tab, a glossary entry, and a row on the closing going-further page when it belongs to no single stage. It gets **no code, packages, endpoints or data**. Promoting a topic to "built" needs an ADR change first.

**Per stage (amended 2026-09-18).** A discussed topic belongs where the question about it gets asked. Someone asks about chunking during Stage 3, not twenty minutes later, so each stage carries a fifth tab, *Going further*: prose and glossary links, no demo, no request ([ADR-0014](0014-web-ui-architecture.md)). Stage 1 has no tab; nothing beyond SQL earns a panel.

| Stage | Topics | Anchored on |
|---|---|---|
| 2 Keyword | True BM25 (IDF, term-frequency saturation, `k1`/`b`) against `ts_rank_cd`; how other stores search text (Lucene, SQL Server, MongoDB, `LIKE` and regex); analysers, stemming and language detection | [ADR-0008](0008-keyword-search-bm25-style.md) |
| 3 Vector | Choosing an embedding model; chunking: fixed-size, sliding window, structure- and layout-aware; multilingual and learned-sparse models such as BGE-M3; the vector landscape — dedicated databases, index choices, filtering at scale | [ADR-0010](0010-vector-search-pgvector.md), [ADR-0012](0012-bge-m3-dense-and-sparse.md) |
| 4 Hybrid | Re-ranking: cross-encoders and late interaction (ColBERT); normalising scores instead of fusing ranks; off-the-shelf hybrid search | [ADR-0011](0011-hybrid-search-rrf.md) |
| 5 Ontology | OWL and reasoners; SHACL; knowledge graphs and graph databases | [ADR-0013](0013-domain-ontology-and-compatibility.md) |
| 6 RAG | RAG metrics: context recall, faithfulness, answer relevance (e.g. RAGAS, TruLens); verifying citations; letting a model choose what to retrieve | [ADR-0016](0016-rag-grounding-and-citations.md) |
| 7 Pedagogy | Adaptive, multi-turn tutoring; personalising with chat and user history; judging an explanation | [ADR-0017](0017-pedagogy-engine.md) |

**The closing page.** What sits *around* the pipeline rather than inside one stage of it.

| Where | Topic | Why a developer meets it |
|---|---|---|
| Before retrieval | Query understanding and intent routing | Conversational queries carry noise and several intents; routing and stripping them first stops irrelevant matches. Stage 5's label matcher is the simplest version |
| Before retrieval | Language detection | Choose language-specific analysers or labels before searching. Stage 2 stems English only; Stage 5 carries GQ-07 on labels |
| Before retrieval | LLM query rewriting (question → search intents) | Turns exploratory questions into concrete searches: more recall, less inspectable ([ADR-0013](0013-domain-ontology-and-compatibility.md) rejects it for this repo) |
| Watching it work | Search telemetry: zero-result, click and abandonment logs | The queries that failed are the ones no golden query thought to ask |
| Proving a change | A/B tests and interleaving | Golden queries say a change is correct; only real traffic says it helped ([ADR-0005](0005-curated-dataset-and-golden-queries.md)) |
| Keeping it fresh | Indexing pipelines, re-embedding, ontology versioning | Every structure the talk builds has to be rebuilt when the model or the catalogue moves ([ADR-0006](0006-database-schema-and-seeding.md)) |
| Who is asking | Personalisation and permission-aware search | Relevance depends on the person, and results must never include what they may not see |

- In talk mode the page comes after Stage 7 and before the summary ([ADR-0014](0014-web-ui-architecture.md)).
- It is a map, not a second talk: one page, one table, a sentence per row.

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
- A question from the floor during a stage can be answered on the stage's own screen, with the topic named and defined, in the time it takes to press one key.
- The going-further tabs are bundled prose and glossary links, so they cost no request and can't drift from the demo.
- Seven of the ten original rows leave the closing page for a stage tab. The page is not shorter, because the cross-cutting topics take their place.

## Alternatives considered

| Option | Why not (for this repo) |
|---|---|
| Keep BGE-M3 as an optional stage, built last | Leaves a conditional tab and a second model in the docs; the time is better spent on the pedagogy comparison |
| Remove BGE-M3 but keep the old numbers (a gap at 5) | In a finished repo, learners would see Stage 6 follow Stage 4 with no explanation |
| Build a cross-encoder re-ranking stage instead | Another model, and it improves relevance, which Stage 4 already teaches; it doesn't advance the triad |
| Include agent protocols in the going-further step | Off-thesis; spends talk time on integration standards rather than search, structure or explanation |
| No going-further step | Learners leave without knowing where the techniques they didn't see fit |
| One flat going-further table, after Stage 7 (the original decision) | A question asked during Stage 3 can't be answered by a page that comes twenty minutes later |
| A going-further tab on every stage, Stage 1 included | Padding. Nothing beyond SQL earns a panel, and an empty tab teaches that the tab is empty |
| A going-further section inside each stage explanation instead of a tab | The explanation's seven headings are the shape the audience learns to read; an eighth about what was *not* built competes with the six that were |
| A separate "Taxonomy" stage before "Ontology" | A stage split for framing only; SKOS-first framing inside Stage 5 does the same with no extra endpoint |

## Teaching notes

- Knowing where a technique fits in the pipeline is more useful than having seen every one run.
- Scope is a design decision. Each stage in the talk should answer one of the triad's questions.
- A before/after is only honest when one thing changes: Stage 5's toggles, and Stage 7's pedagogy toggle.

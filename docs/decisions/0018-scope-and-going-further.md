# ADR-0018: Scope — seven stages, what the talk discusses, and what it leaves out

- **Status:** Accepted
- **Area:** Scope
- **Related:** [ADR-0001](0001-record-architecture-decisions.md), [ADR-0004](0004-pipeline-composition.md), [ADR-0010](0010-vector-search-pgvector.md), [ADR-0011](0011-hybrid-search-rrf.md), [ADR-0012](0012-bge-m3-dense-and-sparse.md), [ADR-0013](0013-domain-ontology-and-compatibility.md), [ADR-0014](0014-web-ui-architecture.md), [ADR-0016](0016-rag-grounding-and-citations.md), [ADR-0017](0017-pedagogy-engine.md)

## Context

This is a 30-minute talk, built in four weeks. Search has far more techniques than either allows: chunking, re-ranking, query rewriting, knowledge graphs, evaluation. Building them all would blur the pipeline. Naming none would leave learners without a map of what comes next.

## Decision

### Seven built stages

| Stage | Slug | Talk question | Decision |
|---|---|---|---|
| 1 Structured | `structured` | What is relevant? | [ADR-0007](0007-structured-search.md) |
| 2 Keyword | `keyword` | What is relevant? | [ADR-0008](0008-keyword-search-bm25-style.md) |
| 3 Vector | `vector` | What is relevant? | [ADR-0009](0009-local-embeddings-onnx-runtime.md), [ADR-0010](0010-vector-search-pgvector.md) |
| 4 Hybrid | `hybrid` | What is relevant? | [ADR-0011](0011-hybrid-search-rrf.md) |
| 5 Ontology | `ontology` | How is it related and constrained? | [ADR-0013](0013-domain-ontology-and-compatibility.md) |
| 6 RAG | `rag` | How should I explain it? (what to say) | [ADR-0016](0016-rag-grounding-and-citations.md) |
| 7 Pedagogy | `pedagogy` | How should I explain it? (how to say it) | [ADR-0017](0017-pedagogy-engine.md) |

- **BGE-M3 is not built** ([ADR-0012](0012-bge-m3-dense-and-sparse.md)): it overlaps hybrid search, and its lessons fit the going-further step.
- **Stage 5 is framed SKOS-first:** taxonomy, synonyms, multilingual labels and value vocabularies are standard, and the compatibility rules are the smallest step beyond them.
- **Stage 7 has a baseline toggle**, so its before-and-after changes only the teaching design.

### Three tiers for every topic

| Tier | What it gets |
|---|---|
| **Built** | Code, data, tests, a stage explanation and an ADR |
| **Discussed** | A section in its stage's "Going further" tab, a glossary entry, and a row on the closing "Going further" page when it belongs to no single stage. **No code, packages, endpoints or data** |
| **Out of scope** | Not mentioned |

### Discussed, not built: each stage's "Going further" tab

A discussed topic belongs where the question about it gets asked. Someone asks about chunking during Stage 3, not twenty minutes later, so each stage carries its own going-further tab: prose and glossary links, no demo. Stage 1 has none — there is nothing beyond SQL that earns a panel.

| Stage | Topics |
|---|---|
| 2 Keyword | True BM25 against `ts_rank_cd`; how other stores search text (Lucene, SQL Server, MongoDB, `LIKE` and regex); analysers, stemming and language detection |
| 3 Vector | Choosing an embedding model; chunking: fixed-size, sliding window, structure- and layout-aware; multilingual and learned-sparse models such as BGE-M3; the vector landscape — dedicated databases, index choices, filtering at scale |
| 4 Hybrid | Re-ranking with cross-encoders and late interaction (ColBERT); normalising scores instead of fusing ranks; off-the-shelf hybrid search |
| 5 Ontology | OWL and reasoners; SHACL; knowledge graphs and graph databases |
| 6 RAG | RAG metrics: context recall, faithfulness, answer relevance; verifying citations; letting a model choose what to retrieve |
| 7 Pedagogy | Adaptive, multi-turn tutoring; carrying a learner model between turns; judging an explanation |

### Discussed, not built: the closing "Going further" page

The closing page carries what sits *around* the pipeline rather than inside one stage of it.

| Where | Topic | Why a developer meets it |
|---|---|---|
| Before retrieval | Query understanding and intent routing | Conversational queries carry noise and several intents; our label matcher is the simplest version |
| Before retrieval | Language detection | Choose language-specific analysers or labels before searching |
| Before retrieval | LLM query rewriting | Turns an exploratory question into concrete searches: more recall, less inspectable |
| Watching it work | Search telemetry: zero-result, click and abandonment logs | The queries that failed are the ones no golden query thought to ask |
| Proving a change | A/B tests and interleaving | Golden queries say a change is correct; only real traffic says it helped |
| Keeping it fresh | Indexing pipelines, re-embedding, ontology versioning | Every structure the talk builds has to be rebuilt when the model or the catalogue moves |
| Who is asking | Personalisation and permission-aware search | Relevance depends on the person, and results must never include what they may not see |

In talk mode this page comes after Stage 7 and before the summary. It is a map, not a second talk: one table, a sentence per row.

### Out of scope: agent protocols

MCP, A2A and AG-UI standardise how agents, tools and user interfaces talk to each other. That is system integration, and it answers none of the talk's three questions. The repository also rejects letting a model decide what to retrieve ([ADR-0016](0016-rag-grounding-and-citations.md)). They appear nowhere in the code, content or glossary.

## Consequences

- The stepper shows seven stages with no gap, and the repository has one embedding model.
- Learners get a map of the adjacent techniques without a second codebase to read.
- A question from the floor during a stage can be answered on the stage's own screen, with the topic named and defined, in the time it takes to press one key.
- The going-further tabs are prose and glossary links only, so they cost no request and can't drift from the demo.
- Anyone promoting a discussed topic to "built" should write a new ADR first.

## Alternatives considered

| Option | Why not (for this repository) |
|---|---|
| Keep BGE-M3 as an optional, last stage | A conditional tab and a second model, for lessons the talk tells in a sentence |
| A cross-encoder re-ranking stage | Another model, improving relevance, which Stage 4 already teaches |
| Include agent protocols in the going-further step | Off-thesis; spends talk time on integration standards rather than search, structure or explanation |
| No going-further step | Learners leave without knowing where the techniques they didn't see fit |
| One flat going-further table, after Stage 7 | A question asked during Stage 3 can't be answered by a page that comes twenty minutes later |
| A going-further tab on every stage, Stage 1 included | Padding. Nothing beyond SQL earns a panel, and an empty tab teaches that the tab is empty |
| A separate "Taxonomy" stage before "Ontology" | A stage split only for framing; SKOS-first framing inside Stage 5 does the same |

## What to take away

- **Knowing where a technique fits in the pipeline is more useful than having seen every one run.**
- **Scope is a design decision.** Each stage should answer one of the talk's questions.
- **A before-and-after is only honest when one thing changes:** Stage 5's toggles, and Stage 7's pedagogy toggle.

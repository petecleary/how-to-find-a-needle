# CLAUDE.md — How to Find a Needle

The supporting repo for the talk **"How to Find a Needle"**: a search pipeline built on one dataset, one stage at a time (structured → keyword → vector → hybrid → ontology → RAG → pedagogy).

- **The code is teaching material.** Learners clone it and read it alongside the talk. Write every file as if a developer new to search will study it.
- **One finished codebase** on `main`. Stages are features in one solution, not branches, tags or commits for learners to step through.
- **Thesis:** AI does not replace good search, data structures or information architecture; it makes them more important.
- **The triad:** Search (*what is relevant?*) → Ontology (*how is it related and constrained?*) → Pedagogy (*how should I explain it?*).

Area-specific standards (loaded when you work in that folder):
- [src/PI.SearchApi/CLAUDE.md](src/PI.SearchApi/CLAUDE.md): C#, API, pipeline, SQL, LLM
- [src/web-ui/CLAUDE.md](src/web-ui/CLAUDE.md): React, TypeScript, content
- [tests/CLAUDE.md](tests/CLAUDE.md): unit, integration and golden-query tests

## Read before you build

1. [docs/adr/README.md](docs/adr/README.md): the ADR index (the "why").
2. [docs/adr/architecture.md](docs/adr/architecture.md): system overview (the "what").
3. [docs/adr/roadmap.md](docs/adr/roadmap.md): phases, tasks, acceptance criteria (the "when").

- **Read the ADR for the stage or area before writing code.** Follow it. If the code needs to deviate, update the ADR first, then the code.
- If `architecture.md` and an ADR disagree, the ADR wins; fix `architecture.md`.
- Update an ADR only when a decision actually changes. An ADR moves to **Accepted** only when its acceptance criteria are met and verified.

## Build order and scope

- Follow the roadmap order: Data → Search APIs (stages 1–5) → Frontend → AI stages (6–7) → Finish & publish.
- When time is short, cut from the bottom of the Must / Should / Could table in the roadmap.
- **Build only the seven stages.** BGE-M3, chunking, re-ranking, OWL/SHACL/knowledge graphs and RAG evaluation are *discussed* in the talk's going-further step, not built. Agent protocols (MCP, A2A, AG-UI) are out of scope entirely. Don't add code for a discussed topic without an ADR change ([ADR-0018](docs/decisions/0018-scope-and-going-further.md)).
- Working ADRs, `architecture.md` and `roadmap.md` live in `docs/adr/` on the `build` branch. The public, learner-facing ADRs are in `docs/decisions/` (same numbers and file names); code comments, content and the UI link to those. Keep both in step when a decision changes.
- Don't add a package, service or framework the ADRs don't mention. If one is needed, propose an ADR change.

## Commands

| Task | Command | Available from |
|---|---|---|
| Run everything (Postgres, API, UI) | `aspire run` (or F5 "Aspire: Launch default AppHost") | now |
| Build | `dotnet build` | now |
| Unit tests (fast, no Docker) | `dotnet test tests/PI.SearchApi.Tests` | Phase 1 |
| Integration tests (Docker + models) | `dotnet test tests/PI.SearchApi.IntegrationTests` | Phase 1 |
| UI checks | `npm run typecheck`, `npm run lint`, `npm run build`, `npm test` (in `src/web-ui`) | Phase 3 |
| Regenerate UI API types | `npm run gen:api` (API running) | Phase 3 |
| Reset data | delete the Postgres data volume in Docker, then `aspire run` | now |

The API is not supported standalone; always start it through `PI.AppHost`.

## Definition of done (every change)

- The build passes with **0 warnings** (`TreatWarningsAsErrors`).
- Unit tests pass. Integration tests pass for every stage built so far.
- UI `typecheck`, `lint` and `build` pass (once `web-ui` exists).
- The README "Getting started" still works from a clean clone for everything built so far.
- `architecture.md` is updated if what you built differs from the plan.
- Comments meet the commenting standard below.

## Teaching-quality principles

- **Clarity over cleverness.** Use named steps and intermediate variables when a dense LINQ chain or one-liner would hide the idea being taught.
- **Be honest in naming.** Postgres FTS is "BM25-style", never "BM25". Similarity is not compatibility. A score means what the trace says it means.
- **Never hide rejected items.** Flagged or incompatible candidates stay visible, with reasons. Show warnings; don't hide failures.
- **The trace is a feature.** Every stage populates `debugTrace` with the exact parameterised SQL, its parameters and the stage-specific details ([ADR-0003](docs/decisions/0003-search-api-contract-and-debug-trace.md)).
- **No hidden magic.** No ORM, no caching, no global state library, no retries that hide failures. Visible behaviour beats convenience; the "Alternatives considered" tables in the ADRs explain each case.
- **Same contract for every stage.** POST, same request, same response, so the UI can switch stages with the same query.
- **Retrieve deep, page late.** Retrieve, fuse and evaluate over `candidateDepth`; page once, in the endpoint.
- **Never make results wait for the LLM.** Results return as JSON; LLM text streams separately from `/answer`.
- **LLM output is untrusted.** Validate finished text and citations; surface warnings.

## Commenting standard

Readers are working developers who know C#, TypeScript and web APIs but are **new to search**. Comments teach search, ML and ontology concepts and explain non-obvious choices. They don't explain the language or the framework.

**1. Technique header.** Every pipeline service opens with this block (UI trace renderers get a one-line version: what the view reveals):

```csharp
// Stage 4 — Hybrid search (Reciprocal Rank Fusion)
//
// What:     Runs keyword and vector search, then fuses their rankings:
//           RRF(d) = Σ wᵢ / (k + rᵢ(d)), with k = 60.
// Strength: Keeps exact-term hits (keyword) and meaning hits (vector) without
//           comparing their incompatible raw scores; only ranks are fused.
// Failure:  Still has no idea what "compatible" means: a near miss that both
//           retrievers like ranks near the top.
// Decision: docs/decisions/0011-hybrid-search-rrf.md
```

**2. Inline comments explain *why*, and teach the concept at that line.**

```csharp
// Good: teaches the concept and the choice
// ts_rank_cd rewards query terms that appear close together (cover density).
// It has no IDF or term-frequency saturation, which is why we call it "BM25-style".

// Good: formula with a worked example
// 1/(60+2) + 1/(60+1) = 0.03252 — being 1st vs 2nd barely matters; appearing in both lists does.

// Bad: narrates the code
// Loop over the candidates and add them to the list
```

**3. Other rules**
- Write formulas as plain text (`Σ`, `≥`, `ᵢ` are fine); add a worked example when the numbers teach something.
- Use the glossary's terms and acronyms (BM25, FTS, tsvector, embedding, cosine distance, HNSW, RRF, dense/sparse, SKOS, RAG, ONNX), so code, UI content and talk share one vocabulary.
- Put doc comments (`///` in C#, TSDoc in TS) on public interfaces, contracts and pipeline services. They're optional on private helpers.
- Link the ADR when a comment summarises a decision, instead of repeating the ADR.
- Keep comments true: update them in the same change as the code.
- No `TODO`s in finished code. A temporary one names its roadmap phase: `// TODO(Phase 4): …`.

## Language and tone

- **British English** in comments, docs, trace notes and UI text: *parameterised, normalisation, tokeniser, colour, behaviour*.
- **Identifiers** follow the framework's spelling where an API requires it (`Normalize`, `Tokenizer`, `Color`).
- Prices in **GBP**, formatted `en-GB`.
- Plain, direct sentences. No marketing language ("blazing fast", "magic").

## Domain vocabulary

Use these names consistently in code, API, UI, content and tests.

| Term | Meaning / form |
|---|---|
| Stage slugs | `structured`, `keyword`, `vector`, `hybrid`, `ontology`, `rag`, `pedagogy` |
| Stage | One numbered technique in the talk (Stage 1–7) with its own endpoint |
| Candidate | A retrieved product with a score and per-technique signals |
| `StageResult` | Candidates plus the trace steps that produced them |
| Trace step | One entry in `debugTrace.steps` |
| Golden query | A talk moment with per-stage expectations, `GQ-03` … |
| Product ID | `PROD-0001` |
| Concept / notation | A SKOS concept in the ontology / its string ID (e.g. `laptop-chargers`) |
| Target device | The product the user owns (`context.targetProductId`) |
| Near miss | Semantically similar but incompatible |
| Compatibility status | `NotEvaluated`, `Compatible`, `Incompatible`, `Unknown` |
| Concept match | `InConcept`, `OutOfConcept`, `NoConcept` |
| Evidence set | The bounded candidates given to the LLM in Stages 6–7 |
| Audience | `novice`, `enthusiast`, `expert` |
| Baseline explanation | Stage 7 with `options.applyPedagogy: false`: same facts and audience, plain prompt, no pedagogical structure |
| Going further | A topic the talk discusses but doesn't build ([ADR-0018](docs/decisions/0018-scope-and-going-further.md)) |

## Data rules

- **Catalog and ontology are separate.** Product records live in `assets/data/products.json`. The domain model (taxonomy, synonyms, value vocabularies, class-level rules) lives in `assets/data/domain-ontology.ttl` and **never names a product** ([ADR-0013](docs/decisions/0013-domain-ontology-and-compatibility.md)).
- Categories and vocabulary-backed spec values use ontology notations; validation tests enforce it.
- Committed embeddings (`assets/data/embeddings/*.jsonl`) are regenerated only with `Embeddings:Rebuild = true`. Never hand-edit them.
- Prompts live in `assets/prompts/*.md`, not in C# string literals. SPARQL lives in `assets/data/queries/*.rq`.
- If a golden query doesn't produce its moment, change product **wording**, not the algorithm ([ADR-0005](docs/decisions/0005-curated-dataset-and-golden-queries.md)).

## Git and secrets

- Work on the `build` branch. Commit only when asked.
- Small, focused commits. Imperative subject that names the phase or stage, e.g. `Stage 2: add keyword search with tsquery trace`.
- Never commit ONNX models, API keys or connection strings. Keys go in `dotnet user-secrets`.

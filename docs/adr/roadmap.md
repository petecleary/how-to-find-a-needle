# Roadmap: How to Find a Needle

From today's cleaned-up scaffold to a finished, teachable demo. Each phase lists its tasks, the ADRs they implement, the acceptance criteria that mark it done, and the open questions to resolve along the way.

> Working document on the build branch (see [ADR-0001](0001-record-architecture-decisions.md)). The repo is one finished codebase on `main`; phases are a build order, not branches learners check out.

**Order:** Data → Search APIs (stages 1–5) → Frontend → AI stages (6–7) with their UI → Finish & publish.

> **Scope change, 2026-09-14 ([ADR-0018](0018-scope-and-going-further.md)).** BGE-M3 is no longer built, and the stages are renumbered to seven: Ontology is Stage 5, RAG Stage 6, Pedagogy Stage 7. Stage 7 gains a baseline toggle and becomes a Must. Phases 0, 1 and 2 were reopened for small rework; it is done and verified (see each phase).

**Rules for every phase**
- Build passes with **0 warnings** (`TreatWarningsAsErrors`).
- Unit tests pass. Integration tests pass for every stage implemented so far.
- The README's "Getting started" works from a clean clone for everything built so far.
- An ADR moves to **Accepted** only when its acceptance criteria are met and verified.
- Update [architecture.md](architecture.md) if anything built differs from the plan, and update the ADR first.
- Treat the ADRs as reference during the build: update one only when a decision actually changes.

## Priorities (4-week build and rehearsal)

If time runs short, cut from the bottom up. Anyone building, human or agent, follows this order.

| Priority | Scope |
|---|---|
| **Must** (the talk works) | Data and ontology; Stages 1–5; Stage 6 with streaming; Stage 7 pedagogy with audience and the baseline toggle; demo screen; talk mode with stage explanations; golden-query tests for stages 1–5; README "Getting started" |
| **Should** | "Going further" talk step; glossary with hover terms; ADR pages; CI |
| **Could** | ~500-product growth; OpenAI providers; public ADR rewrite; OpenAPI drift check in CI |

Not built at any priority: BGE-M3 and the other going-further topics ([ADR-0018](0018-scope-and-going-further.md)).

**Weekly shape**

| Week | Focus |
|---|---|
| 1 | Phase 1 (data, ontology, seeding) and Phase 2 through Stage 3 (vector) |
| 2 | Stage 4 (hybrid) and Stage 5 (ontology); ADR-0018 rework of Phases 0–2; Phase 3 UI scaffold with the demo screen |
| 3 | Phase 4 (Stages 6–7 streaming, including the pedagogy baseline) and talk mode with content |
| 4 | **Protected:** rehearsals, polish, and fixing what rehearsal exposes |

---

## Phase 0 — Clean-up ✅ (done)

- Removed unused packages (Redis output caching, `Aspire.Hosting.JavaScript`, `Aspire.Npgsql` in the AppHost); patched the `Microsoft.OpenApi` vulnerability via `Microsoft.AspNetCore.OpenApi` 10.0.12.
- `.gitignore`: keep `appsettings.Development.json`; ignore downloaded models except their README; `docs/adr/` was initially ignored and is now tracked on the build branch.
- Removed the dead Datafiniti CSV import; the API fails fast without its connection string; removed `UseFileServer`; tidied template comments.
- Fixed the models README (correct files and paths for Nomic and BGE-M3).

### ADR-0018 rework ✅ (done 2026-09-14)

1. **Models README** (`src/PI.SearchApi/assets/models/README.md`)
   - Remove section 2 (BGE-M3) and the `bge-m3/` folder from the expected layout.
   - Correct the Nomic section's "Used by" line to the **Vector, Hybrid and Ontology** stages (it currently omits Ontology).

### Acceptance criteria (rework) ✅
- The models README describes only Nomic, and following it from a clean clone still produces a working `nomic/` folder.

---

## Phase 1 — Data ✅ (done)

**ADRs:** [0002](0002-solution-structure-and-orchestration.md), [0005](0005-curated-dataset-and-golden-queries.md), [0006](0006-database-schema-and-seeding.md), [0013](0013-domain-ontology-and-compatibility.md) (vocabulary and facts only)

### ADR-0018 rework ✅ (done 2026-09-14)

1. **Schema** (`assets/data/init.sql`, [ADR-0006](0006-database-schema-and-seeding.md))
   - Remove `embedding_bge_dense`, `embedding_bge_sparse` and their comment from `CREATE TABLE`.
   - Remove the `ix_products_bge_dense` and `ix_products_bge_sparse` indexes.
   - Add an idempotent in-place migration: `DROP INDEX IF EXISTS` for both indexes, then `ALTER TABLE products DROP COLUMN IF EXISTS` for both columns, with a comment linking ADR-0018. Existing data volumes must update without a reset.
   - Renumber the stage comments ("Stages 3, 4, 5" for the dense embedding; HNSW comment without Stage 5's sparse vectors).
2. **Seeder** (`Data/DatabaseSeeder.cs`)
   - Remove the two BGE columns from the upsert's "hash changed → NULL the embeddings" clause.
3. **Composition root** (`Program.cs`)
   - The `UseVector()` comment names `SparseVector`, which is no longer used; name `Vector` only.

### Acceptance criteria (rework) ✅
- On an **existing** data volume, `aspire run` starts without a reset, `\d products` shows no BGE columns or indexes, and the log shows `0 inserted, 0 updated, 0 deleted`.
- On a **fresh** volume, the log shows `Seeded 60 products (60 inserted, 0 updated, 0 deleted)`.
- `dotnet test` (unit) passes, including catalog validation.
- ADR-0006 → **Accepted** again.

**Verified (2026-09-14):** on the existing `pgvector-data-search` volume the AppHost started without a reset. `products` has no BGE columns or indexes, and all 60 rows kept their Nomic vector (the migration dropped only the BGE columns). Unit tests: 161 pass. The optional fresh-volume run was not repeated.

### Original tasks ✅ (done)

1. **Solution conventions** (0002)
   - Add `Directory.Build.props` (nullable, warnings as errors, analysers), `Directory.Packages.props` (central versions) and `.editorconfig`.
   - Pin the Postgres image tag in `AppHost.cs`.
2. **Test projects** (0002)
   - `tests/PI.SearchApi.Tests` (xUnit) and `tests/PI.SearchApi.IntegrationTests` (xUnit + `Aspire.Hosting.Testing`), added to the `.slnx`.
   - One smoke test each (a health check for integration).
3. **CI** (0002)
   - GitHub Actions workflow that builds and runs unit tests on every push.
4. **Golden queries first** (0005)
   - Draft `assets/data/golden-queries.json` (GQ-01 to GQ-07) with the talk moment each demonstrates. ✅ Reviewed and approved.
5. **Catalog** (0005)
   - Author `assets/data/products.json` (~60 items) to create each golden-query moment: targets, correct answers, near misses, keyword traps, filler.
   - Write a JSON schema file (`products.schema.json`) to catch typos.
6. **Ontology** (0013)
   - `assets/data/domain-ontology.ttl`: SKOS taxonomy (the categories, with icons and definitions), synonyms and multilingual (English + Spanish) labels, value vocabularies (connectors, storage interfaces, memory types, platforms) and class-level domain rules. **No product IDs.**
   - Authored alongside the catalog, because product categories and constrained spec values use its notations.
   - `Pipeline/Ontology/DomainOntology.cs`: a minimal dotNetRDF loader (concepts, narrower-concept checks, vocabulary values, rules) so catalog validation tests read the real TTL. Not registered in DI yet — Phase 2 extends it into the full `IOntologySearch` pipeline.
7. **Database** (0006)
   - Replace `init.sql` with the idempotent schema (generated `search_vector`, JSONB, vector columns, indexes).
   - Add `Aspire.Npgsql` + `Pgvector` and register `NpgsqlDataSource` with `UseVector()`.
8. **Seeder** (0006)
   - `Data/DatabaseSeeder.cs` replaces `DatabaseManager`: schema, then upsert with `content_hash`, then delete removed products. (Embedding backfill hooks are added in Phase 2.)
9. **Legacy removal** (0006)
   - Delete `GET /api/products`, `Models/Products/*` and `ProductRepository`.
10. **Catalog validation tests**
    - Every product ID is unique.
    - Every golden-query product ID exists.
    - Every product category is a taxonomy notation; every vocabulary-backed spec value is a known notation or label; every device and accessory type has the specs its domain rules compare.
    - Units are numeric.
11. **README "Getting started" (minimal)**
    - Prerequisites so far, `aspire run`, and how to reset the data volume.
    - Extend it at the end of every phase, and verify each phase from a clean clone.

### Original acceptance criteria ✅
- `aspire run` starts Postgres and the API; the log shows `Seeded 60 products (60 inserted, 0 updated, 0 deleted)`.
- A second run logs `0 inserted, 0 updated`, and startup is noticeably faster.
- Editing one product's description updates exactly that row and nulls its embeddings.
- `dotnet test` passes, including the catalog validation tests.
- A clean clone runs Phase 1 by following the README alone.

### Resolved
- ✅ Fictional brands and golden queries GQ-01 to GQ-07 (GQ-06 is an SSD interface mismatch rather than camera lenses). Reviewed and approved.
- ✅ Category icons, no product images.
- ✅ GBP, with `en-GB` price formatting.
- ✅ The repo is on GitHub, so the CI task applies.
- ✅ ADR-0002, 0005 and 0006 stay **Proposed**: each has more scope landing in a later phase (0002: web-ui CI in Phase 3; 0005: dataset growth in Phase 5; 0006: embedding backfill in Phase 2), so none moves to Accepted until its whole decision is built — the same pattern Phase 2 uses for ADR-0013. *(ADR-0006 was accepted when Phase 2 completed, and reopened by ADR-0018.)*

---

## Phase 2 — Search APIs (stages 1–5) ✅ (done)

**ADRs:** [0003](0003-search-api-contract-and-debug-trace.md), [0004](0004-pipeline-composition.md), [0007](0007-structured-search.md), [0008](0008-keyword-search-bm25-style.md), [0009](0009-local-embeddings-onnx-runtime.md), [0010](0010-vector-search-pgvector.md), [0011](0011-hybrid-search-rrf.md), [0013](0013-domain-ontology-and-compatibility.md)

> Stage numbers in this phase use the seven-stage numbering. The ontology stage was built as "Stage 6" and renumbered in the ADR-0018 rework.

### ADR-0018 rework ✅ (done 2026-09-14)

1. **Contract** ([ADR-0003](0003-search-api-contract-and-debug-trace.md))
   - Remove `BgeDenseRank` and `BgeSparseRank` from `Contracts/CandidateSignals.cs`.
   - Add `ApplyPedagogy` (bool, default `true`) to `Contracts/SearchOptions.cs`, with a doc comment linking ADR-0017. No stage reads it until Phase 4; the validator needs no rule.
   - Update doc comments: `SearchRequest.Query` "Required for Stages 2–7"; `SearchOptions.Audience` "Stage 7"; `SearchResponse` "Stages 6–7 stream that"; `ProductResult.Score` "the RRF sum in Stages 4 and 5".
2. **Renumber the ontology stage 6 → 5 in code and data.** No behaviour changes:
   - Technique headers and doc comments in `Pipeline/Ontology/*` (`OntologySearch`, `IOntologySearch`, `LabelMatcher`, `QueryExpander`, `ConceptClassifier`, `CompatibilityEvaluator`, `TargetDeviceResolver`, `DomainOntology`, `IOntology`, `OntologyModels`).
   - References in `Pipeline/Keyword/*`, `Pipeline/Hybrid/IHybridSearch.cs`, `Pipeline/Vector/IVectorSearch.cs`, `Pipeline/SqlFilterBuilder.cs`, `Pipeline/Candidate.cs`, `Pipeline/ProductLookup.cs`, `Pipeline/ProductSummary.cs`, `Contracts/*`, `Data/ProductDocument.cs`.
   - `Pipeline/Fusion/IRankFusion.cs`: "Stages 4 and 5", dropping "and 5 if built".
   - `Program.cs` DI block comment: "Stage 5: Ontology".
   - `Endpoints/Search/Ontology/OntologySearchEndpoint.cs`: doc comment and OpenAPI `Summary` ("Stage 5 — Ontology: …").
   - The `Activity` name: `"Stage 5: ontology search"`.
   - `assets/data/golden-queries.json` moment text for GQ-02, GQ-07 and GQ-08.
   - Test comments in `OntologyStageGoldenQueryTests.cs` and `CatalogValidationTests.cs`.
3. **README** (repo root)
   - "The pipeline": seven stages. Describe Stage 5 as a SKOS taxonomy, synonyms and value vocabularies plus class-level rules. Remove the "knowledge graphs … `compatibleWith`, `requires`, `partOf`" wording, which the ontology doesn't do.
   - "What's in this repo": local embeddings are Nomic only.
   - Stage table "5 Ontology"; "Stages 3, 4 and 5" in the model section; the status line's stage sequence.

### Acceptance criteria (rework) ✅
- `dotnet build`: 0 warnings. Unit tests pass. Integration tests: all golden-query tests pass (28 of 28 before the rework).
- `/openapi/v1.json` has no `bgeDenseRank` / `bgeSparseRank` and does include `options.applyPedagogy`.
- `grep -rniE "bge|Stage 8|Stages? 7–8" src tests` finds nothing, and no remaining "Stage 6" in `src` or `tests` refers to the ontology.
- The README matches the seven stages.
- ADR-0003 → **Accepted** again. ADRs 0004, 0007, 0008, 0010, 0011 and 0013 stay **Accepted** (amended).

**Verified (2026-09-14):**
- `dotnet build`: 0 warnings. Unit tests: 161 pass. Integration tests: **28 of 28** pass.
- `/openapi/v1.json`: `CandidateSignals` has no BGE ranks; `SearchOptions.applyPedagogy` is a boolean.
- GQ-01 on `/api/search/ontology` is unchanged: the compatible chargers rank first and the 45W barrel charger is still flagged Incompatible.
- The leftover grep over `src`, `tests` and `README.md` is clean.
- **Found while verifying:** `/openapi/v1.json` has no operation summaries at all, so FastEndpoints' `Summary(...)` text (for example "Stage 5 — Ontology") doesn't reach Scalar or the generated UI types. This was already true before the rework. Look at it in Phase 3, when the UI types are generated.

### Added after the rework: `GET /api/vocabularies` ✅ (2026-09-14)

Lets the UI build its spec filters from the ontology, the same way it builds the category filter ([ADR-0013](0013-domain-ontology-and-compatibility.md), [ADR-0014](0014-web-ui-architecture.md)).
- Built: `assets/data/queries/vocabularies.rq`; `IOntology.Vocabularies`; `ValueVocabularyBuilder` (spec keys read from the rules' `ex:valueScheme` checks); `VocabulariesEndpoint`; contracts `ValueVocabulary` and `ValueVocabularyEntry`.
- README "After editing the ontology": re-run `aspire run` to load TTL edits.
- **Verified:** `dotnet build` 0 warnings; unit tests 166 pass (5 new); integration tests 29 of 29 pass (1 new).

### Original build order ✅ (done)

Build strictly in this order. Each step ends with its golden-query integration tests passing.

1. **Contract** (0003, 0004)
   - `Contracts/` types, `Pipeline/` shared types (`Candidate`, `StageResult`, `TraceStep`), `SqlFilterBuilder`, ProblemDetails mapping for 503.
   - `IOntology` basics: load the TTL; label, taxonomy and narrower-concept lookups (needed by Stage 1 category filters).
   - `GET /api/demo/queries`, `GET /api/demo/devices` and `GET /api/taxonomy`.
   - Integration test harness that runs golden-query expectations per stage.
   - **Verify OpenAPI output first:** FastEndpoints request and response schemas (including nested `filters`/`options` and enums) must appear correctly in `/openapi/v1.json` via `Microsoft.AspNetCore.OpenApi`. If they don't, switch to FastEndpoints' own OpenAPI support before building more endpoints, because the UI's generated types depend on it ([ADR-0014](0014-web-ui-architecture.md)).
2. **Stage 1 — Structured** (0007)
   - `POST /api/search/structured`; `categories &&` filter with narrower-concept expansion; JSONB `@>` spec filters; `COUNT(*)` total; SQL in trace. ✅ GQ-04.
3. **Stage 2 — Keyword** (0008)
   - `websearch_to_tsquery` + `ts_rank_cd`; parsed tsquery and matched lexemes in trace; "BM25-style" notes. ✅ GQ-02 (misses), GQ-03 (trap ranks high).
4. **Embeddings** (0009, 0006)
   - `NomicOnnxEmbeddingGenerator : IEmbeddingGenerator` + `ISearchEmbedder` (tokenizer, prefixes, mean pooling, L2); verify the tokenizer file requirement and update the models README; unit tests for pooling and normalisation.
   - `Embeddings` config (`Provider`, `Rebuild`). The seeder loads `assets/data/embeddings/nomic.jsonl` when hashes match, embeds stale or missing products live, and `Rebuild: true` regenerates and overwrites the file. Commit `nomic.jsonl`; add the file-consistency unit test.
   - The OpenAI provider (`text-embedding-3-small`, `dimensions: 768`) is shaped for the same interface but **built and tested in Phase 5**.
5. **Stage 3 — Vector** (0010)
   - pgvector cosine; `SET LOCAL hnsw.ef_search`; distances in trace. ✅ GQ-02 (hit), GQ-01 (near miss ranks high).
   - ⚠️ If the embeddings don't produce the expected moments, iterate on product *wording* ([ADR-0005](0005-curated-dataset-and-golden-queries.md) workflow).
6. **Stage 4 — Hybrid** (0011)
   - Pure `ReciprocalRankFusion` with exhaustive unit tests; concurrent Keyword + Vector; per-item formula strings in trace. ✅ GQ-03 drill battery lifted above the keyword trap; GQ-01 near miss still present.
7. **Stage 5 — Ontology** (0013)
   - `IOntologySearch`: understand (label matcher) → expand (keyword OR-groups + expanded embedding text) → Hybrid → classify (in/out of concept) → constrain (class-level rules vs target-device specs).
   - Toggles `expandSynonyms` / `applyConstraints`; flagged items kept with reasons; one trace step per step.
   - Unit tests for label matching, expansion, the tsquery builder, classification and each rule operator. ✅ GQ-01, GQ-02 (keyword side rescued), GQ-03 (phone battery out of concept), GQ-05, GQ-06, GQ-07 (Spanish label → concept expansion).

### Original acceptance criteria ✅
- ✅ All 5 endpoints (stages 1–5) appear in Scalar and return the shared contract with a populated `debugTrace`.
- ✅ The integration suite shows the talk's story as passing tests: synonym miss → vector hit; keyword trap → hybrid lifts the right answer → ontology flags the trap; near miss → ontology flag with reason; Spanish query → chargers via ontology labels; device name → context, not intent.
- ✅ Missing ONNX models give a `503` with fix-it guidance, not a stack trace.
- ✅ ADRs 0003, 0004, 0007, 0008, 0010, 0011 and 0013 → **Accepted**. ADR-0006 → **Accepted** too, now that its embedding step is built. *(ADR-0003 and ADR-0006 reopened by ADR-0018.)*
  - ADR-0009 stays **Proposed**: its Nomic provider is built and verified, but its OpenAI provider is Phase 5 scope (same rule as Phase 1).

### Build status (2026-09-14) ✅ complete before ADR-0018

**Built:** the shared contract and trace; `SqlFilterBuilder`; the three demo GETs; Stages 1–5 with thin endpoints and validators; the Nomic ONNX embedder; the seeder's embedding step, with a committed `nomic.jsonl`; 503 handling; the golden-query integration harness.

**Verified:**
- `dotnet build`: 0 warnings. Unit tests: 161 pass.
- Integration tests: **28 of 28 pass**, covering golden queries GQ-01 to GQ-08 across every stage built, plus the demo endpoints.
- The `init.sql` migration (reviews added to `search_vector`) ran in place on an existing data volume.
- OpenAPI: FastEndpoints works with `Microsoft.AspNetCore.OpenApi`, so no switch was needed. Nested request types, string enums and strict numbers all appear in `/openapi/v1.json`.
- The seeder loads 60 vectors from `nomic.jsonl` into empty rows, reports "60 already current" on restart, and rewrites the file with `Embeddings__Rebuild=true`.
- With the model moved away, Stages 3, 4 and 5 return 503 with guidance, while Stages 1–2 and the demo GETs still work.

**Decisions recorded while building** (ADRs updated first):
- **0003:** adds `options.explain`; `audience` is a lower-case string.
- **0004:** `Candidate` has a nullable score and a compatibility result; `StageResult` has an optional total; Stage 1 pages in SQL.
- **0009:** `tokenizer.json` is the only tokenizer file needed. Embed **one text per inference call**: padding shifts int8 vectors (cosine 0.989 against 1.000).
- **0013:** label normalisation (case, accents, simple plurals), term order before the cap, and the scope of `applyConstraints`.
- **0013 (after GQ-08):** the target device is resolved in the Understand step. A device name in the query is removed from retrieval text; the device's own type ("laptop", "drill") is a context concept, not expanded or classified; the device itself is demoted with a reason.
- **0013 (ontology data):** "Charger fits laptop" now applies to every `chargers` concept, not only `laptop-chargers`, so a 20W phone charger is Incompatible with a laptop instead of unflagged.
- **0005:** GQ-08 "The device name trap" added. Talk notes are recorded in the Teaching notes of ADRs 0005, 0008, 0009, 0010, 0011 and 0013.
- **0006 / 0008:** `reviews` added to `search_vector` at weight D, with an in-place migration in `init.sql`.
- **0005 / 0011:** GQ-03's hybrid expectation changed from "phone battery not in the top 3" to "ranked below the drill battery". Under RRF, a keyword #1 stays in the top 3; removing it is Stage 5's job.

**Golden-query checkpoint — resolved with Pete:**

| Issue | Resolution |
|---|---|
| Vector GQ-01, hybrid GQ-01, vector GQ-05, vector GQ-06, ontology GQ-05: device and brand names in the query text pulled brand products above the accessories | Queries no longer name the device: GQ-01 "power adapter for my laptop", GQ-05 "18V battery", GQ-06 "SSD upgrade for my laptop"; the device comes from `targetProductId`. The device-name failure became GQ-08, and Stage 5 now treats a device name as context ([ADR-0005](0005-curated-dataset-and-golden-queries.md), [ADR-0013](0013-domain-ontology-and-compatibility.md)) |
| Keyword GQ-03: phone battery ranked 5th, and wording fixes also moved it up in vector search | Reviews indexed at weight D (it becomes keyword #1); hybrid expectation corrected to match what RRF honestly does ([ADR-0011](0011-hybrid-search-rrf.md)) |

### Open questions
- ✅ Keep `reviews` out of `search_vector`? No: indexed at the lowest weight (D). GQ-02 is unaffected; GQ-03's keyword trap depends on it (ADR-0006, ADR-0008).
- ✅ Language for GQ-07: Spanish ("cargador USB-C para portátil"). Passes in Stage 5.
- ✅ Tokenizer file: `tokenizer.json` only (ADR-0009).
- ✅ OpenAPI: FastEndpoints + `Microsoft.AspNetCore.OpenApi` is sufficient (ADR-0014).

---

## Phase 3 — Frontend (stages 1–5)

**ADRs:** [0014](0014-web-ui-architecture.md)

The ADR-0018 rework of Phases 0–2 is done, so the generated API types contain no BGE signals.

1. Scaffold `src/web-ui` (Vite + React + TS strict, Tailwind, shadcn/ui init, Lucide, ESLint, Prettier, Vitest, `.nvmrc`).
2. Re-add `Aspire.Hosting.JavaScript`; `AddViteApp` with `WithReference(searchApi)`; Vite `/api` proxy from the service-discovery environment variable. Spike a dummy SSE endpoint through the proxy to confirm streaming isn't buffered, since Phase 4 depends on it.
3. `npm run gen:api` with `openapi-typescript` → committed `src/api/schema.d.ts`; typed `fetch` client.
4. `usePipelineSearch` hook (same request across stages, AbortController, URL state) + Vitest tests.
5. Layout: `SearchBar` (golden-query presets, device picker), `FilterBar` (category tree from `/api/taxonomy`, spec filters from `/api/vocabularies`; no hard-coded values), `PipelineStepper` (seven tabs, keyboard ←/→, Stage 5 toggles), `ResultCard` with `SignalBadges` and `CompatibilityBadge`.
6. `DebugDrawer` renderers: `SqlBlock`, `TsQueryView`, `DistanceTable`, `RrfTable`, `ConceptMatches`/`ExpansionView`/`RuleChecks`, JSON fallback.
7. **Pages & content** (0014)
   - React Router routes `/`, `/talk/:step`, `/demo`, `/glossary`, `/decisions`, `/decisions/:id`.
   - `src/web-ui/content/`: placeholder `speaker.md`; `talk.json` with intro, hook, stages 1–5, and summary steps; `stages/*.md` explanations (Stage 5's explanation presents SKOS first and the rules as the step beyond it, [ADR-0013](0013-domain-ontology-and-compatibility.md)); `glossary.json`.
   - Inline `term:` links with hover cards; ADRs rendered from `docs/adr/*.md` via `import.meta.glob`.
   - Vitest content-integrity tests.
   - ❓ Review the talk/demo split with Pete in the running UI before writing full content.
8. Presentation mode (large type, on by default in talk mode); accessibility pass.
9. CI: add `typecheck`, `lint` and `build` for `web-ui`.

### Acceptance criteria
- `aspire run` opens the UI. Choosing GQ-01 and stepping 1 → 5 shows the results changing and the near miss flagged in Stage 5 with its reason.
- Every trace step renders with a purpose-built view (no raw JSON for stages 1–5).
- Readable on a 1280×720 projector in presentation mode; keyboard-only operable.
- Talk mode walks from Home through the intro and stages 1–5 using only the keyboard. Each stage step shows its explanation and live results; glossary terms show definitions on hover and focus; ADR pages render with working cross-links.
- ADR-0014 → **Accepted** (for stages 1–5).

### Open questions
- ❓ Visual style and branding for the talk (colours, logo), if any.
- ❓ Speaker details for `speaker.md` (name, role, bio, photo, links). Placeholders until supplied.
- ❓ `docs/adr/thoughts.md` (untracked walk notes) would be picked up by the `docs/adr/*.md` glob. Move or delete it before the Decisions pages are built.

---

## Phase 4 — AI stages (6–7) with UI

**ADRs:** [0015](0015-llm-hosting-and-client.md), [0016](0016-rag-grounding-and-citations.md), [0017](0017-pedagogy-engine.md)

1. **LLM provider** (0015)
   - `Llm` settings (`Provider` = `ollama` | `openai` | `anthropic`, `Model`, `Endpoint`); API keys in user secrets; no containers.
   - `LlmClientFactory` builds one `IChatClient` (`Microsoft.Extensions.AI.OpenAI` for Ollama and OpenAI; the official `Anthropic` SDK for Claude) with OpenTelemetry middleware. Sampling parameters are provider-specific (no `temperature` for Claude).
   - 503 guidance; optional warm-up.
2. **Model bake-off** (0015)
   - Run the golden queries 10× each against the candidate models; record JSON validity, citation correctness and latency in ADR-0015; choose the default.
3. **Stage 6 — RAG** (0016)
   - `POST /api/search/rag` (JSON results + evidence trace step) and `POST /api/search/rag/answer` (SSE stream, plus JSON mode for tests).
   - Evidence-set builder, including each matched concept's definition, `prefLabel` and `altLabel`s; prompt files in `assets/prompts/`; `GetStreamingResponseAsync` → `meta`/`delta`/`final`/`done`/`error` events.
   - Validation after generation: citations, `INSUFFICIENT_EVIDENCE` sentinel, incompatible-recommendation heuristic.
   - Cancellation; unbuffered response; trace with prompts, raw output and timings (time to first token, total); unit + structural integration tests.
4. **Stage 7 — Pedagogy** (0017)
   - `POST /api/search/pedagogy` + `/answer` streaming the `answer` section, then the `explanation` section.
   - Two prompts: `pedagogy-system.md` (principles, fixed markdown headings, audience sections with label choice) and `pedagogy-baseline.md` (a fair, plain prompt with the same grounding rules), selected by `options.applyPedagogy`.
   - Heading parser; validator (Decision Compatible, Near miss Incompatible, concept-label heuristic), skipped for the baseline apart from citations; parsed structure in `final` (`null` for the baseline); per-section timings and the prompt used in the trace; tests.
5. **UI** (0014)
   - `useAnswerStream` (fetch + SSE parser, in parallel with the results request).
   - `SummaryPanel` rendering streamed markdown with `[PROD-…]` chips that scroll to result cards, warning badges and time to first token.
   - **Audience picker in the `SearchBar`** (part of URL state); **"Apply pedagogy" toggle** for Stage 7 in the `PipelineStepper`.
   - `PromptView` trace renderer; loading states for multi-second calls.
   - Talk-mode steps, stage explanations and glossary entries for Stages 6–7, including the baseline-then-pedagogy sequence for GQ-01.

### Acceptance criteria
- GQ-01 in Stage 6 gives a grounded answer citing the compatible charger and warning about the near miss.
- **GQ-01 in Stage 7, `novice`, pedagogy off:** a free-form explanation with no citation warnings, and a trace that shows the baseline prompt and "structure checks not applied".
- **The same with pedagogy on:** all five headings, a Decision on the compatible charger, and connector + wattage explained with the near miss as a counter-example.
- Switching audience visibly changes the explanation's wording (novice uses everyday labels; expert is spec-first) without changing the facts.
- In Stages 6–7 the results list renders before any LLM text, and the summary's first token appears within ~1.5 s on the presenter laptop (local Ollama). Stage 7's answer and explanation complete in under ~15 s combined.
- Switching stage mid-stream cancels generation (visible in Ollama/the trace).
- With Ollama stopped, Stages 6–7 still show results, the summary panel shows the 503 guidance, and Stages 1–5 are unaffected.
- ADRs 0015–0017 → **Accepted**.

### Open questions
- ❓ Default model (from the bake-off).
- ❓ Default hosted model IDs for learners (OpenAI and Anthropic). Confirm when Phase 4 starts, since model versions move quickly.
- ❓ Wording of `pedagogy-baseline.md`: review it with Pete so the comparison is fair, not a straw man.

---

## Phase 5 — Finish & publish

1. **Dataset growth:** a generator script (in `tools/`, language to be decided) adds distractors to reach ~500 products without disturbing the curated core; golden-query tests still pass.
2. **README (final pass; kept current since Phase 1):** prerequisites (.NET 10, Docker, Node LTS, Aspire CLI, Hugging Face CLI, and either Ollama or an OpenAI/Anthropic API key), model download, `aspire run`, a tour of the 7 stages, how to reset the data volume, troubleshooting.
3. **CI:** OpenAPI → TypeScript drift check; optional manual integration-test workflow.
4. **Talk content & rehearsal:**
   - Finalise the talk-mode steps, speaker details and summary.
   - Add the **"Going further" step** ([ADR-0018](0018-scope-and-going-further.md)): one table of the discussed-not-built topics by pipeline position, plus glossary entries (chunking, re-ranking, cross-encoder, learned sparse, OWL, SHACL, knowledge graph). No agent protocols.
   - Rehearse the full talk end to end in the UI (there are no slides). Rebuild `nomic.jsonl` after dataset growth.
5. **Public ADRs:** write learner-facing ADRs from each ADR's *Teaching notes*; choose their public location; decide what happens to these working ADRs; point the UI's `/decisions` pages at the public versions.
6. **Final review:** code comments read as teaching material; every stage file opens with its technique / strength / failure-mode comment; all ADRs **Accepted**, **Rejected** or explicitly superseded.
7. **OpenAI providers (when credits allow)** (0009, 0015): test OpenAI embeddings (`Embeddings:Provider = openai`, `Rebuild: true` → commit `openai.jsonl`) and OpenAI chat; adjust golden-query expectations if needed; document the one-key setup in the README.

### Acceptance criteria
- A fresh clone on a clean machine runs end to end by following the README alone.
- The whole golden-query suite passes at ~500 products.
- Public ADRs are published; the full talk, including the going-further step, is rehearsed end to end in the UI's talk mode.
- ADR-0018 → **Accepted**.

### Open questions
- ✅ No slides: the talk lives in the web UI ([ADR-0014](0014-web-ui-architecture.md)).
- ❓ Public ADR location (`docs/decisions/`?) and whether the working ADRs stay, are archived, or are replaced by the public versions before merging to `main`.
- ❓ Generator script language (C# console in `tools/` proposed, to avoid adding Python).

---

## Traceability

| ADR | Phase(s) |
|---|---|
| 0001 | all (process) |
| 0002 | 1 (+ CI in 3, 5) |
| 0003, 0004 | 2 (+ ADR-0018 rework) |
| 0005 | 1 (+ growth in 5) |
| 0006 | 1 (+ backfill in 2, ADR-0018 rework) |
| 0007–0011 | 2 |
| 0012 | — (Rejected by ADR-0018) |
| 0013 | 1 (vocabulary), 2 (stage) |
| 0014 | 3 (+ AI panels in 4, going-further step in 5) |
| 0015–0017 | 4 |
| 0018 | 0–2 (rework), 4 (pedagogy baseline), 5 (going-further content) |

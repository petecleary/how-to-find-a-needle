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
- **Found while verifying:** `/openapi/v1.json` has no operation summaries at all, so FastEndpoints' `Summary(...)` text (for example "Stage 5 — Ontology") doesn't reach Scalar or the generated UI types. This was already true before the rework. Look at it in Phase 3, when the UI types are generated. *(Fixed in Phase 3 step 3, 2026-09-15.)*

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

**ADRs:** [0014](0014-web-ui-architecture.md) (visual design and stage tabs amended 2026-09-14), [0002](0002-solution-structure-and-orchestration.md) (web-ui CI)
**Design reference:** [docs/design](../design/README.md): the agreed screens, light and dark.

The ADR-0018 rework of Phases 0–2 is done, so the generated API types contain no BGE signals.

**Build order.** Risky plumbing first (hosting, streaming, types), then the stage screen from the outside in, then content and polish. Every step ends with `npm run typecheck`, `npm run lint`, `npm run build` and its own Vitest tests passing, and `dotnet build` still at 0 warnings.

1. **Scaffold and theme** (0014 § Stack, § Visual design)
   - Vite + React + TypeScript strict **into the existing `src/web-ui` folder** (keep `CLAUDE.md` and `assets/images/`). Tailwind; shadcn/ui init; Lucide; ESLint (typescript-eslint, react-hooks); Prettier; Vitest; `.nvmrc` (Node LTS).
   - shadcn primitives, copied into `src/components/ui`: button, input, tabs, switch, toggle-group, checkbox, collapsible, select, hover-card, sheet, badge, tooltip. Add others only when a component needs them.
   - Theme tokens in `src/index.css`: light on `:root`, dark on `.dark`; triad fills, inks and tints; status colours; neutrals; mapped into Tailwind and shadcn's variables.
   - Self-hosted fonts in `src/assets/fonts/` (Dosis, Atkinson Hyperlegible, JetBrains Mono `.woff2` + each `OFL.txt`), `@font-face` rules, and font utilities (`font-brand` for logo and title only).
   - `ThemeProvider` (system default, header override stored in `localStorage`) and the logo component (filled on light, outline on dark).
   - `stageGroup(stage)` → `search | ontology | pedagogy` and its colour, with a unit test, so every component colours stages the same way.
   - ✅ **Done 2026-09-15.** typecheck, lint, build, Prettier and 10 Vitest tests pass. A temporary theme-check page (`App.tsx`) was compared with the design screens in both themes at 1280×720; step 10 replaces it. Findings:
     - **TypeScript is pinned to `~6.0`.** TypeScript 7 (the native compiler) is out, but `typescript-eslint` supports `<6.1` only. Revisit when it adds 7.
     - **Node:** `.nvmrc` pins 24 (the current LTS); `engines` asks for ≥ 24, so newer local versions work.
     - **shadcn CLI quirk:** `shadcn add` wrote `import { cn } from "cn"` and installed an unrelated `cn` package instead of using `@/lib/utils`. Fixed by hand; check the imports after any future `shadcn add`.
     - Fonts are the Latin subsets, only in the weights the design uses (Dosis 700/800, Atkinson 400/700 with italics, JetBrains Mono 400/700), 188 KB in total, taken from the Fontsource packages without adding them as dependencies.
     - `Tooltip` needs a `TooltipProvider` at the root; add it with the first tooltip (step 5).
2. **Aspire hosting and the SSE spike** (0014 § Stack)
   - Re-add `Aspire.Hosting.JavaScript`; `AddViteApp("web-ui", "../web-ui")` with `WithReference(searchApi)`, `WaitFor`, `WithExternalHttpEndpoints`.
   - Vite `/api` proxy from `services__searchapi__https__0`.
   - Spike a throwaway SSE endpoint through the proxy and confirm chunks arrive unbuffered (Phase 4 depends on it). Record the result here, then delete the spike.
   - ✅ **Hosting and proxy done 2026-09-15** (brought forward so step 1 could be seen under `aspire run`). `Aspire.Hosting.JavaScript` 13.4.6 (same version as the SDK). Aspire starts Vite after the API is healthy and injects `PORT`, `services__searchapi__https__0` and `SEARCHAPI_HTTPS`. `GET /api/demo/queries` through the Vite origin returns the golden queries. The proxy sets `secure: false` because Node doesn't trust the ASP.NET Core development certificate.
   - ✅ **SSE spike done 2026-09-15: the proxy does not buffer.** A throwaway `POST /api/spike/sse` streamed six `delta` events 500 ms apart, each flushed. Timed with `curl -N`:
     - Direct to the API: each event arrived within 1 ms of being sent.
     - Through the Vite origin: each event arrived 1–2 ms after being sent, still 500 ms apart. None were held back until the end.
     - Proxied headers: `content-type: text/event-stream`, `Transfer-Encoding: chunked`, no `content-encoding`. No proxy option was needed.
     - Phase 4 can stream `/answer` through `/api` as planned. The spike endpoint is deleted.
     - **Not tested:** whether a client disconnect through the proxy cancels the API's `CancellationToken`. Phase 4's "switching stage mid-stream cancels generation" depends on it; check it when `/answer` exists.
3. **API types and client** (0014 § API types)
   - `npm run gen:api` (`openapi-typescript`) → committed `src/api/schema.d.ts`; `src/api/client.ts` typed `fetch` wrapper that surfaces ProblemDetails (503 guidance) instead of throwing opaque errors.
   - Fix the Phase 2 finding first: FastEndpoints `Summary(...)` text doesn't reach `/openapi/v1.json`. Then regenerate.
   - ✅ **Done 2026-09-15.** `dotnet build` 0 warnings; unit tests 166 pass; integration tests **39 of 39** (10 new). Web UI: typecheck, lint, build, Prettier; Vitest 17 pass (7 new). Findings:
     - **Why summaries were missing:** FastEndpoints writes `Summary(...)` for its own Swagger package (NSwag); this API uses `Microsoft.AspNetCore.OpenApi`, which never reads it. `FastEndpointsSummaryTransformer` copies it into each operation. No new package.
     - **The 400 wasn't documented at all.** `ProducesProblemFE<ProblemDetails>` was silently dropped: FastEndpoints' ProblemDetails is an `IResult`, which the generator skips. A `ValidationProblem` contract record now describes the real body (ProblemDetails fields plus `errors: [{ name, reason }]`), and an integration test checks a real 400 against it. `503` is documented as `ProblemDetails` on Stages 3–5 (ADR-0003 amended).
     - **`openapi-typescript` 7.13 declares `typescript ^5`**, and the UI pins 6.0 (step 1). `package.json` `overrides` makes it use the project's TypeScript; generation works. Revisit when it supports 6.
     - **`--empty-objects-unknown`:** without it, open dictionaries (`specs`, trace `parameters` and `details`) were generated as `Record<string, never>`, which nothing can read. They are now `Record<string, unknown>`, narrowed by the renderers.
     - **`gen:api` reads `http://localhost:5377`** (the API's fixed http address from its launch settings), because Node doesn't trust the ASP.NET Core development certificate. Run it while `aspire run` is running.
     - **`client.ts`:** one function per endpoint, return types read from `paths`, and `SearchStage` derived from the `/api/search/*` paths, so Stages 6–7 become callable only once their endpoints exist. Errors are `ApiError` (`status`, `problem`, `validationErrors`; `message` is the 503's fix-it `detail`). No retries.
     - Operation IDs are long generated names (`PISearchApiEndpoints…`). The client uses paths, not operation IDs, so they are left as they are.
4. **State: `usePipelineSearch` and URL state** (0014 § State)
   - Same request across stages; `AbortController` per request; `pageSize: 50`.
   - URL state: `stage`, `tab`, `q`, `gq`, device, filters, toggles, audience. Talk position in `/talk/:step/:tab?`.
   - Vitest: re-run on stage change, cancellation, URL round-trip, golden-query preset fills the request.
   - ✅ **Done 2026-09-15.** Typecheck, lint, build and Prettier pass; Vitest **39 pass (22 new)**. `dotnet` untouched. Built:
     - `src/lib/searchState.ts` (pure, no React): `parseSearchState` / `serializeSearchState` (defaults are left out of the URL; unknown values fall back to defaults), `toSearchRequest` (`pageSize: 50`), `applyGoldenQuery` (fills query, device and filters; keeps stage, tab and toggles).
     - `src/lib/talkRoute.ts`: `talkPath(step, tab?)` and `parseTalkTab`. The routes themselves arrive with React Router in step 10.
     - `src/hooks/usePipelineSearch.ts`: keyed on the request's JSON, so an equal object doesn't re-search; aborts the in-flight search on every change; a late answer to an old search is never shown; `idle` when a stage that needs a query has none; `rerun()`.
     - **URL parameters:** `stage`, `tab`, `q`, `gq`, `device`, `brand`, `category` (repeated), `minPrice`, `maxPrice`, `spec.{key}`, `expandSynonyms`, `applyConstraints`, `audience`, `applyPedagogy`.
     - **Checked against the live API:** the exact JSON `toSearchRequest` builds (asserted in the tests) was posted. GQ-01 on `/ontology` returns 50 of 50 with all 7 Incompatible items in the one response (PROD-0014 to 0016 among them); GQ-04 on `/structured` matches the numeric spec `voltageV: 18` (6 Brakk products ≤ £100); an empty query on `/keyword` is a 400, which is why the hook stays `idle`.
   - Findings:
     - **Spec values in the URL:** a URL carries text only, so numeric values are turned back into numbers. JSON containment matches 18 but not "18" (ADR-0007). Vocabulary values are notations (`usb-c`), never bare numbers, so nothing is converted by mistake.
     - **Test dependencies:** hook tests need a DOM renderer. `@testing-library/react` and `jsdom` were added as dev dependencies, jsdom only for files that opt in; recorded in ADR-0014 § Quality bar.
     - The hook shows no response while a new search is loading: nothing stale is labelled with the new stage. Revisit in step 7 if the flash is distracting on the projector.
5. **Stage screen shell** (0014 § Stage screen)
   - `AppHeader`, `SearchBar` (golden-query picker from `/api/demo/queries`, device picker from `/api/demo/devices`, Filters button with count), `PipelineStepper` (three triad groups, ARIA tablist), `StageTabs` (How it works · Results · Answer · Under the hood; Answer disabled before Stage 6; H / R / A / U), `StageOptions` on the tab row (Stage 5 toggles; the audience picker is added in Phase 4).
   - Loading, empty and 503 states for the tab content.
   - ✅ **Done 2026-09-15.** Typecheck, lint, build and Prettier pass; Vitest **55 pass (16 new)**. Checked in headless Chrome at 1280×720, light and dark, with GQ-01 on Stage 5 against the live API (Results and Under the hood). Built:
     - `/demo` (`DemoPage`), with React Router; every other path redirects to it until step 10 adds the pages. State comes from the URL (`useSearchState`); golden queries and devices load with `useApiData`.
     - `AppHeader` (logo, title, theme toggle), `SearchBar` (golden-query picker, query submitted on Enter, "I own" device picker, Filters button with the active count), `PipelineStepper` (tablist in three triad groups; ←/→/Home/End), `StageTabs` (Radix tablist; H / R / A / U from anywhere except while typing; Answer disabled before Stage 6), `StageOptions` (Stage 5 switches).
     - `SearchOutcome`: idle ("needs a query"), loading, and errors with the ProblemDetails title and `detail`, validation failures and **Try again**. An unreachable API asks whether `aspire run` is still running.
     - `lib/stageTabs.ts` (tab order, shortcuts, availability), `lib/format.ts` (GBP en-GB, ms), stage labels and triad captions in `stageGroup.ts`, `countActiveFilters`, and a runtime `searchStages` list type-checked against the generated `SearchStage`.
   - Placeholders, each with a `TODO(Phase 3)`: `ResultsTab` rows (step 7), `UnderTheHoodTab` list (step 8), `HowItWorksTab` text (step 9), the disabled Filters button (step 6). Stages 6–7 are shown but not selectable (`TODO(Phase 4)`).
   - Findings:
     - **Tab order:** the tabs follow ADR-0014 and `web-ui/CLAUDE.md` (How it works · Results · Answer · Under the hood), which is also the order → walks in talk mode. The design screens show Results first. ✅ Confirmed with Pete (2026-09-15): the ADR order stands, and the design screens are out of date on this point.
     - shadcn's `TabsTrigger` styles fight the design's underline (a dark-theme active border rule wins), so `StageTabs` uses the Radix `Tabs` primitive directly with its own classes.
6. **Filters** (0014, 0013)
   - `FilterPanel`: brand, price range, category checkbox tree from `/api/taxonomy` (a parent includes its narrower concepts), spec vocabularies from `/api/vocabularies` with synonyms as hints. No hard-coded values.
   - Demo: collapsible sidebar. Talk: `Sheet` drawer from the Filters button.
   - Vitest: selecting values builds the right `filters` object.
   - ✅ **Done 2026-09-15.** Typecheck, lint, build and Prettier pass; Vitest **77 pass (22 new)**. `dotnet` untouched. Checked in headless Chrome at 1280×900 against the live API. Built:
     - `FilterPanel` (brand, price, category tree, one filter per vocabulary, other specs, Clear), `CategoryTree`, `SpecVocabularyFilter`, `CommittedInput` (applies on Enter or blur, so typing doesn't search on every keystroke), `FilterDrawer` (`Sheet`), `useMediaQuery`, and the shadcn `radio-group` primitive (from `radix-ui`, already a dependency).
     - Pure rules in `src/lib/filters.ts`: `toggleCategory`, `specSelection`, `setVocabularyValue`, `changeSpecKey`, `otherSpecs`, `setBrand`, `parsePriceInput`, `clearFilters`. `matchesGoldenQuery` in `searchState.ts`.
     - `/demo`: the sidebar from `lg` (64rem) up, toggled by the Filters button; below `lg` the same panel opens as a drawer. The talk page reuses `FilterDrawer` in step 10.
     - **Checked against the live API:** `categories: ["chargers"]` + `connector: "usb-c"` returns the 6 USB-C chargers; `chargingPort: "usb-c"` returns the 4 USB-C laptops; brand `voltline` (lower case) + `maxPrice: 40` returns 7.
   - Findings:
     - **A vocabulary can back several spec keys.** Connectors are `connector` on chargers and `chargingPort` on laptops (from the rules, ADR-0013), and `specs` is JSON containment on one key. Each vocabulary filter matches one key at a time, with a small key picker when there is more than one. It teaches the point: the concept is shared, the field name isn't.
     - **One value per vocabulary.** Containment can't express "USB-C or barrel" on one key, so values are radio buttons, not the design's checkboxes.
     - **Ticking a parent category ticks its narrower concepts** (shown checked and disabled, "included by Chargers" for screen readers) and drops any narrower selection, because the API expands it anyway.
     - **Other specs:** a spec with no vocabulary (GQ-04's `voltageV: 18`) is listed as a removable chip, showing `18` vs `"18"`, rather than hidden.
     - **Changing any input away from a golden query's preset** (query, device or filters) clears `gq`. Before this step only a query edit did.
     - ✅ **Brand is a dropdown** (decided with Pete at the step 7 checkpoint, 2026-09-15). It was a text box because no endpoint listed brands. ADR-0003 was amended to add `GET /api/brands` (`SELECT DISTINCT brand … ORDER BY brand`, from Postgres: brands are catalogue data, and the ontology never names a maker), with an integration test. The picker is single-choice: `filters.brand` stays one exact, case-insensitive brand, because a multi-select would change the request for every stage. A brand in the URL that the catalogue lacks is kept as an option, so the applied filter is always the one shown.
7. **Results tab** (0014, 0003, 0013)
   - `ResultRow` with `SignalBadges` (keyword rank, vector rank, RRF), `CompatibilityBadge`, `ConceptBadge`, category icon from the taxonomy.
   - Stages 1–4: one list. Stage 5: in concept · out of concept (collapsed) · Flagged column of `FlaggedCard` (every check with has / needs values; "#n before rules" from `signals.fusedRank`).
   - Vitest: Stage 5 grouping, with GQ-01's response as a fixture.
   - ✅ **Built 2026-09-15** (awaiting the checkpoint below). Typecheck, lint, build and Prettier pass; Vitest **95 pass (18 new)**. `dotnet` untouched. Checked in headless Chrome at 1280×720 with GQ-01 on Hybrid and Stage 5 against the live API. Built:
     - `ResultsTab`, `ResultRow`, `SignalBadges`, `CompatibilityBadge`, `ConceptBadge`, `CategoryIcon`, `FlaggedCard`, `ConceptGroupedResults`.
     - Pure helpers: `lib/signals.ts` (which signals each stage shows, and what its score means), `lib/resultGroups.ts` (splits Stage 5's order where the groups change; never re-ranks), `lib/traceDetails.ts` (reads the Constrain step's `checks` and the Classify step's `wantedConcepts` with runtime checks), `lib/taxonomy.ts` (`findConcept` moved here from `filters.ts`; labels and icons).
     - Fixture: `src/test/fixtures/gq-01-ontology.json`, a real response (`resolveJsonModule` turned on for it). Step 8's renderer tests can add GQ-02 to GQ-08 beside it.
     - **Signals by stage:** Structured none (no score, ordered by price then ID); Keyword `KW` ts_rank_cd; Vector `SIM` cosine similarity; Hybrid `KW #n · VEC #n · RRF`, with `–` for a retriever that missed. Each badge has a hover and screen-reader description. The summary line says what `score` means on that stage.
     - **Stage 5, rules on:** in concept, then out of concept (first 4 shown, "Show 36 more"; the target device's reason is shown on its row), then the Flagged column: one card per Incompatible item with every check (`connector has 5.5mm barrel · needs USB-C`, the definition on hover) and "#2 before rules" from `signals.fusedRank`. **Stage 5, rules off:** one list with concept badges and Hybrid's signals.
     - GQ-01 now flags **7**, not the design's 3: the four other near-miss chargers plus two DDR4 modules the memory rule catches from the 50 retrieved.
   - Findings:
     - **Icons load by name** with `DynamicIcon` from `lucide-react/dynamic` (no new package), so a TTL icon edit needs no UI change. Cost: the build emits one small chunk per Lucide icon (1,829 files, 7.9 MB in `dist`, of which the app loads only what it uses) and the main bundle grows from 450 kB to 599 kB (the icon name list). Acceptable for a local teaching app; the alternative is a hand-kept map of the ~20 names the TTL uses.
     - ⏸ **The chargers show a laptop icon.** The TTL gives `laptop-chargers` the icon `laptop`, and a row uses its first category's icon (ADR-0014). Suggest `plug` in the TTL, a data change; not made. Deferred by Pete at the checkpoint (2026-09-15), to review later.
     - ✅ **Density at 1280×720 with the filter sidebar open:** Stage 5's two columns wrap long names, and the flagged column shows about 2½ cards. Decided at the checkpoint (2026-09-15): the demo starts with the sidebar closed; the Filters button opens it.
   - ✋ **Checkpoint with Pete:** review the running talk/demo split and the stage screen before writing full content.
8. **Under the hood tab** (0014, 0003)
   - `TraceFlow` (one chip per trace step, coloured by the step's stage) and the selected step's renderer: `SqlBlock` (SQL + parameters), `TsQueryView`, `EmbeddingView`, `DistanceTable`, `RrfTable`, `ConceptMatches`, `ExpansionView`, `ClassificationTable`, `RuleChecks`, JSON fallback.
   - Vitest: renderer selection by `details` keys; every Stage 1–5 trace step for GQ-01 to GQ-08 gets a purpose-built renderer (no fallback).
   - ✅ **Done 2026-09-15.** Typecheck, lint, build and Prettier pass; Vitest **104 pass (9 new)**. `dotnet` untouched. Checked in headless Chrome against the live API: GQ-01 on Stage 5 (Constrain) and Hybrid (RRF), GQ-02 on Keyword (no matches), GQ-04 on Structured (SQL). Built:
     - `UnderTheHoodTab`: `TraceFlow` (a Radix tablist of step chips, coloured by each step's own stage, ←/→ between them) and `TraceStepView` (title, stage, duration, the step's view, its SQL and parameters, its notes). It opens on the last step, where the stage's own technique runs.
     - Renderers in `src/components/trace/`: `SqlBlock` (Stage 1 also shows row counts and the count query), `TsQueryView`, `EmbeddingView`, `DistanceTable`, `RrfTable` (with "The rules say" on Stage 5), `ConceptMatches`, `ExpansionView`, `ClassificationTable`, `RuleChecks` (checks grouped by product, and the rules.rq SPARQL), `JsonFallback`. Long lists show 10 rows (6 products for checks) with "Show N more" (`useShowMore`).
     - `lib/traceStepKind.ts` picks the view from the `details` keys, never the title. `lib/traceDetails.ts` has one typed reader per kind, checking every field at runtime; RRF rows are split from the API's own formula strings, not recomputed.
     - **Coverage test:** `src/test/fixtures/golden-query-trace-steps.json` lists all 129 trace steps (stage, title, SQL or not, `details` keys) from GQ-01 to GQ-08 on Stages 1–5, plus GQ-01 on Stage 5 with each switch off, captured from the running API. Every step maps to its expected view; none falls back to JSON. `gq-04-structured.json` was added for the SQL view test.
   - Findings:
     - **GQ-04 returns 400 on Stages 2–5** (no query), so its only trace is Stage 1's. Expected: it is the structured-filter moment.
     - **Edge shapes handled:** Expand with synonyms off sends `phrases: null`; Constrain with rules off writes only `applyConstraints`; with no target device, `checks` is empty and the method says why; Keyword can match nothing (GQ-02, GQ-07).
     - **Ligatures lied in the SQL.** JetBrains Mono drew `<=` as `≤` and `->` as an arrow, so the SQL shown wasn't character for character what ran. Ligatures are now off for all monospaced text (`index.css`).
     - The selected step isn't in the URL (local state), so a bookmark opens on the last step. Talk mode (step 10) can add it if a talk step needs to open on, say, RRF.
9. **How it works tab, glossary and content** (0014 § Content)
   - `StageExplanation` renders `content/stages/{stage}.md` with the fixed headings. Stage 5 presents SKOS first and the rules as the step beyond it ([ADR-0013](0013-domain-ontology-and-compatibility.md)).
   - Inline `[term](term:id)` links with hover cards that also open on focus; `glossary.json`.
   - Vitest content-integrity tests (step files, golden-query IDs, `term:` links, one explanation per stage, valid `tabs` values).
   - ✅ **Done 2026-09-15.** Typecheck, lint, build and Prettier pass; Vitest **135 pass (29 new)**. `dotnet` untouched. Checked in headless Chrome at 1280×720 against the design screen (Stage 5) and on Stage 2. Built:
     - **Content:** `content/stages/{structured,keyword,vector,hybrid,ontology}.md` with the seven fixed headings. Every claim was checked against the ADRs and the live API (e.g. GQ-01's barrel charger is #2 in Hybrid and fails two checks; GQ-02 has no keyword matches; 1/(60+1) + 1/(60+2) = 0.03252). Stage 5 leads with SKOS and presents the rules as the step beyond it.
     - **`content/glossary.json`:** 44 entries in 8 topics, including every term ADR-0014 lists and the going-further terms (each marked "Discussed in the talk, not built").
     - **`react-markdown` 10.1 + `remark-gfm` 4.0** (named in ADR-0014). `Markdown` renders content with two link schemes of our own: `[RRF](term:rrf)` → `GlossaryTerm` (Radix hover card, opens on focus too) and `[ADR-0011 · …](adr:0011-hybrid-search-rrf)` → `/decisions/…`. `urlTransform` keeps those schemes; everything else goes through react-markdown's default, and raw HTML isn't rendered.
     - `StageExplanation` (three columns, as the design) and `HowItWorksTab`. `lib/content.ts` loads the files at build time with `import.meta.glob`, splits them at the fixed headings and finds `term:`, `adr:` and `GQ-nn` references.
     - **Content-integrity tests:** an explanation for every stage the API serves (and no stray files); fixed headings in order, each with text; every `term:` link resolves; every `adr:` link names a file in `docs/adr/`; every GQ id is in the API's `golden-queries.json`; glossary ids unique and kebab-case, `seeAlso` and `adr` references resolve.
   - Findings:
     - **Moved to step 10:** the `talk.json` checks (step files, `tabs` values) and the `/glossary` and `/decisions/:id` pages. Until then, term and ADR links go to routes that redirect to `/demo`.
     - **react-markdown blanks unknown link schemes** (a guard against `javascript:` URLs), so `term:` and `adr:` links rendered with no href until `urlTransform` allowed exactly those two.
     - Stage 6–7 explanations are written with those stages in Phase 4; until then their How it works tab says the file doesn't exist yet.
10. **Pages and talk mode** (0014 § Pages)
    - Routes `/`, `/talk/:step/:tab?`, `/demo`, `/glossary`, `/decisions`, `/decisions/:id`.
    - Home: title in Dosis, thesis, triad, *Start the talk* / *Explore the demo*, speaker card from `speaker.md` (placeholders for name, title, bio, email, LinkedIn, and a LinkedIn QR code image).
    - Talk: `talk.json` with intro, hook, stages 1–5 and summary steps; → walks each stage step's `tabs`, then the next step. Vitest for the navigation order.
    - Glossary page; Decisions pages from `docs/adr/*.md` via `import.meta.glob`, with ADR links rewritten.
    - ✅ **Built 2026-09-15.** Typecheck, lint, build and Prettier pass; Vitest **164 pass (29 new)**. `dotnet` untouched. Checked in headless Chrome at 1280×720 against the live API: Home, the talk intro, Stage 5 in talk mode (Results), Going further, Glossary, Decisions and ADR-0014. Built:
      - **Routes** `/`, `/talk` (→ first step), `/talk/:step/:tab?`, `/demo`, `/glossary`, `/decisions`, `/decisions/:id`; anything else → `/`. `AppHeader` has page navigation (Talk · Demo · Glossary · Decisions) and, in talk mode, the position ("Stage 5 of 7").
      - **Home:** Dosis title, thesis from `content/home.md`, the triad, *Start the talk* and *Explore the demo*, and `SpeakerCard` from `content/speaker.md` (`- **Key:** value` lines, bio below). Every field is a placeholder; photo and QR code images go in `content/images/`.
      - **Talk mode:** `content/talk.json` has nine steps: intro, "the needle" (GQ-01 as the thread), Stages 1–5 (GQ-04, GQ-02, GQ-01, GQ-03, GQ-01), Going further (the ADR-0018 table) and Summary. Stage steps reuse the demo's screen (`StageScreen`, extracted from `DemoPage`) with the step's golden query and options, a one-line `TalkCaption` above the tabs, and the filter drawer. Presenter changes last until the step changes. ←/→ and Page Up/Down (clickers) walk tabs then steps (`useTalkKeys`); a focused stepper or tab list keeps its own arrows. `TalkControls` gives Previous/Next buttons and "Step 7 of 9 · Results (2 of 3)".
      - **Glossary:** searchable, grouped by topic, `#id` anchors, see-also and ADR links. **Decisions:** index with status; each ADR rendered with links to other ADRs rewritten to their pages (anchors kept) and links to other repository files shown as text. `Markdown` gained heading ids (GitHub-style `slugify`), tables, code blocks and quotes. The reading pages are lazy-loaded.
      - Pure, tested: `lib/talk.ts` (step tabs, next/previous position, preset state), `lib/decisions.ts`, `lib/speaker.ts`, `lib/glossarySearch.ts`, `lib/slug.ts`; `lib/keyboard.ts` shares the typing check with `StageTabs`.
      - **Content integrity:** talk step ids unique and kinds known; every step file exists; Stages 1–5 in order, each with a golden query the API serves; `tabs` valid and available; Going further then Summary last; every `term:`, `adr:` and GQ reference in talk files resolves; every ADR has a title and status, and **every link between ADRs, including its `#anchor`, resolves**.
    - Findings:
      - **Stages 6–7 steps** are added before Going further in Phase 4.
      - **Bundle size:** the ADRs are text in the bundle. Globbing only `NNNN-*.md` and lazy-loading the reading pages keeps them out of the first page; Vite's 500 kB warning remains for the main chunk (lucide's icon-name list, react-markdown, the app).
      - **Hash links** (`/glossary#rrf`, `/decisions/…#visual-design`) screenshotted blank with Chrome's `--screenshot` flag after scrolling. Step 11's DevTools-protocol run shows `/glossary#rrf` scrolls to the entry and highlights it: the blank shots were the flag, not the page.
      - ❓ **Talk content is a first draft** (intro, the needle, captions, Going further, Summary): Pete to edit. **Speaker details** are still placeholders.
      - `ADR-0014` asks for talk mode to be validated with Pete before content is finalised: this is that point.
11. **Presentation mode, themes and accessibility pass**
    - Presentation mode: large type, non-essential controls hidden; on by default in talk mode.
    - Check both themes against the design screens at 1280×720; contrast ≥ 4.5:1 (3:1 large text); visible focus; landmarks; keyboard-only run through the talk.
    - ✅ **Done 2026-09-15.** Typecheck, lint, build and Prettier pass; Vitest **219 pass (55 new)**. `dotnet` untouched. Built:
      - **Presentation mode:** `PresentationProvider` puts `.presentation` on `<html>`; `index.css` zooms the page 12.5% (pixel sizes included, so proportions hold and popovers stay aligned), and a `presentation:` Tailwind variant hides non-essential controls (the page links in the header, the ←/→ hint in the talk bar). On by default in talk mode, off elsewhere; the header's `PresentationToggle` (`aria-pressed`) overrides it everywhere and is remembered on the device, like the theme. Rules in `lib/presentation.ts`, tested.
      - **Accessibility fixes:** a *Skip to content* button first in the header; a visually hidden `<h1>` on stage screens (talk stage steps and the demo had none); a real focus ring on the two inputs that hid their outline (search bar, glossary search).
      - **Contrast as a test:** `lib/contrast.ts` computes WCAG ratios, and `contrast.test.ts` reads the real tokens from `index.css` in both themes: 25 text-on-background pairings at ≥ 4.5:1, and the focus ring at ≥ 3:1 against the page and cards. It found one failure: white on dark-theme Search purple was **4.37:1**; `--search` in `.dark` is now `#6961ef` (4.61:1, still ≥ 3:1 against the dark background and cards). Vitest skips CSS unless told to, so `vite.config.ts` includes `index.css` for the test.
    - **Keyboard-only run** (headless Chrome driven over the DevTools protocol with real key events, against the live API; no new package):
      - → from `/talk/intro` visits all **19** positions in order (intro, the needle, each stage's three tabs, Going further, Summary) and stops; ← visits them in exactly reverse order. U on a stage step jumps to Under the hood.
      - Presentation mode on in talk mode, off on `/demo`.
      - Tab through Home, a talk stage step, the demo and the glossary: 43–45 focus stops each, **every one with a visible outline or ring**. The first stop is *Skip to content*.
      - Landmarks on seven pages: one `header`, one `main`, labelled `nav`s ("Pages", "Talk steps"), one `h1`; no unnamed buttons; no images without `alt`.
      - A dropdown opened by keyboard (golden-query picker, presentation mode, dark theme) lists its 9 options, aligned under its trigger.
    - **Both themes at 1280×720** (Home, talk intro, Stage 5 How it works · Results · Under the hood, demo Hybrid, glossary `#rrf`) match the design screens' structure and colours: triad colours, red status badges with icons, Dosis only on the logo and title.
    - Findings:
      - Presentation mode makes pages taller than 720px, so talk steps scroll; the talk bar is sticky, so Previous/Next stay on screen.
      - Card and input borders (`--border`) are about 1.2:1 against the page. WCAG 1.4.11 doesn't require them where text and position already identify the control, so they are left as the design has them.
12. **CI** (0002)
    - Add `npm ci`, `typecheck`, `lint`, `build` and `test` for `web-ui` to the GitHub Actions workflow.

### Acceptance criteria
- `aspire run` opens the UI. Choosing GQ-01 and stepping 1 → 5 shows the results changing, and in Stage 5 the near miss appears in the Flagged column with its failed checks, **with no paging**.
- Every trace step renders with a purpose-built view (no raw JSON for stages 1–5).
- Both themes match the design screens in [docs/design](../design/README.md) closely: triad colours, status badges with text, Dosis only on the logo and title, fonts loading with the network disconnected.
- Readable on a 1280×720 projector in presentation mode; keyboard-only operable.
- Talk mode walks from Home through the intro and stages 1–5 using only the keyboard, stepping through each stage's tabs; H / R / A / U jump to a tab. Glossary terms show definitions on hover and focus; ADR pages render with working cross-links.
- Filters are built entirely from `/api/taxonomy` and `/api/vocabularies`: an ontology label edit appears after re-running the AppHost.
- CI runs the web-ui checks.
- ADR-0014 → **Accepted** (for stages 1–5).

### Open questions
- ✅ Visual style and branding: Pi & Mash; see ADR-0014 § Visual design and [docs/design](../design/README.md).
- ❓ Speaker details for `speaker.md` (name, title, bio, email, LinkedIn, photo, LinkedIn QR code image). Placeholders until supplied.
- ✅ `docs/adr/thoughts.md` no longer exists, so the Decisions glob is clean.

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
   - Enable the **Answer tab**: `AnswerPanel` (streamed answer, `[PROD-…]` chips, citation validation badges, time to first token), `ExplanationPanel` (Stage 7's five headings, or the baseline), `EvidenceSet` (what the model was given; chips jump here). The Results tab badge shows results are ready before the first token ([design](../design/screens/stage-7-answer.png)).
   - **Audience picker** and Stage 7's **"Apply pedagogy" toggle** in `StageOptions` on the tab row (both part of URL state).
   - `PromptView` renderer in the Under the hood tab; loading states for multi-second calls.
   - Talk-mode steps, stage explanations and glossary entries for Stages 6–7, including the baseline-then-pedagogy sequence for GQ-01.

### Acceptance criteria
- GQ-01 in Stage 6 gives a grounded answer citing the compatible charger and warning about the near miss.
- **GQ-01 in Stage 7, `novice`, pedagogy off:** a free-form explanation with no citation warnings, and a trace that shows the baseline prompt and "structure checks not applied".
- **The same with pedagogy on:** all five headings, a Decision on the compatible charger, and connector + wattage explained with the near miss as a counter-example.
- Switching audience visibly changes the explanation's wording (novice uses everyday labels; expert is spec-first) without changing the facts.
- In Stages 6–7 the results list renders before any LLM text, and the summary's first token appears within ~1.5 s on the presenter laptop (local Ollama). Stage 7's answer and explanation complete in under ~15 s combined.
- Switching stage mid-stream cancels generation (visible in Ollama/the trace).
- With Ollama stopped, Stages 6–7 still show results, the Answer tab shows the 503 guidance, and Stages 1–5 are unaffected.
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

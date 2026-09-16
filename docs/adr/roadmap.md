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
| **Could** | ~300-product growth; OpenAI providers; public ADR rewrite; OpenAPI drift check in CI |

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

## Phase 3 — Frontend (stages 1–5) ✅ (closed 2026-09-15)

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
    - ✅ **Done 2026-09-15** (not yet run on GitHub: it runs on the next push). `.github/workflows/ci.yml` has a second job, `web-ui`, beside `build-and-test`: checkout (the whole repository, because tests read `docs/adr` and the API's `golden-queries.json`), `setup-node` from `src/web-ui/.nvmrc` with the npm cache keyed on `package-lock.json`, then `npm ci` → `format:check` → `typecheck` → `lint` → `test` → `build`. No API, Docker or models needed: `schema.d.ts` is committed and the tests use captured responses.
    - **Verified locally the way CI runs:** a copy of the working tree with no `node_modules`, `dist`, `bin` or `obj`, on **Node 24.21** (the `.nvmrc` version, fetched with `npx -p node@24` so nothing was installed in the repo) and again on Node 26.7: `npm ci` from the lockfile, formatting, typecheck, lint, **219 tests** and the build all pass. The workflow parses as YAML with both jobs and every step. The files CI reads from outside `src/web-ui` are tracked, and none of the new content or fixtures is git-ignored.
    - Findings:
      - `npm ci` prints a notice that `fsevents` (a macOS-only optional dependency) has install scripts; it is skipped on Linux and changes nothing.
      - The workflow keeps `actions/checkout@v4` to match the existing job.

### Phase 3 status ✅ closed by Pete (2026-09-15)
- Steps 1–12 are built, and their checks pass locally (Vitest 219, typecheck, lint, build, Prettier; `dotnet build` at 0 warnings).
- **Closed with the remaining checks and the copy review moved to the end of the build:** Pete runs the manual checks (➡️ below) and reviews the talk and copy once the whole demo is complete, in [Phase 5 step 8, Clean-up and sign-off](#phase-5--finish--publish). **ADR-0014 stays Proposed until then**, because an ADR is Accepted only when its criteria are verified.

### Acceptance criteria
- ✅ `aspire run` opens the UI. Choosing GQ-01 and stepping 1 → 5 shows the results changing, and in Stage 5 the near miss appears in the Flagged column with its failed checks, **with no paging**.
- ✅ Every trace step renders with a purpose-built view (no raw JSON for stages 1–5): all 129 steps for GQ-01 to GQ-08 are covered by a test.
- ✅ Both themes match the design screens in [docs/design](../design/README.md) closely: triad colours, status badges with text, Dosis only on the logo and title. ➡️ *Fonts loading with the network disconnected:* Pete checks (the fonts are self-hosted).
- ✅ Readable on a 1280×720 projector in presentation mode; keyboard-only operable (step 11's run).
- ✅ Talk mode walks the intro and stages 1–5 using only the keyboard, stepping through each stage's tabs; H / R / A / U jump to a tab; glossary terms show definitions on hover and focus; ADR pages render with working cross-links. ➡️ *Starting from Home by keyboard:* Pete checks.
- ➡️ Filters are built entirely from `/api/taxonomy`, `/api/vocabularies` (and `/api/brands`): *an ontology label edit appears after re-running the AppHost:* Pete checks.
- ➡️ CI runs the web-ui checks: the job is built and passes on a clean copy on Node 24; *green on GitHub* after the next push.
- ➡️ ADR-0014 → **Accepted** (for stages 1–5): in Phase 5 step 8.

### Open questions
- ✅ Visual style and branding: Pi & Mash; see ADR-0014 § Visual design and [docs/design](../design/README.md).
- ➡️ Speaker details for `speaker.md` (name, title, bio, email, LinkedIn, photo, LinkedIn QR code image): placeholders until supplied; moved to Phase 5 step 8.
- ➡️ The laptop-charger icon (`laptop-chargers` → `laptop` in the TTL): deferred; moved to Phase 5 step 8.
- ✅ `docs/adr/thoughts.md` no longer exists, so the Decisions glob is clean.
- ✅ Brand filter: a dropdown from `GET /api/brands` (step 6, decided at the step 7 checkpoint).

---

## Phase 4 — AI stages (6–7) with UI

**ADRs:** [0015](0015-llm-hosting-and-client.md), [0016](0016-rag-grounding-and-citations.md), [0017](0017-pedagogy-engine.md)

**Decided with Pete at the start of Phase 4 (2026-09-15; ADR-0015 amended):**
- `Llm` settings live on the API (`appsettings.json` + user secrets for the key), not forwarded by the AppHost.
- Anthropic is built and tested live now, with `claude-sonnet-5` as its default.
- The bake-off is an opt-in integration test over the two installed Ollama models (`qwen3.6:35b`, `gemma4:31b`); no small model is pulled.
- Stage 7 has three talk steps: baseline → pedagogy → audience switch.

**Order change:** the bake-off scores the real prompts and validators, so it moves after the Stage 7 API and the prompt review. `qwen3.6:35b` is the provisional default until then.

**Second order change (Pete, 2026-09-15, at the step 3 checkpoint):** the Stages 6–7 UI comes before the prompt review and the bake-off, so Pete can review the prompts in the running app. Build order is now 1 → 2 → 3 → **5 → 6 → 7** (the stage explanations `rag.md` and `pedagogy.md` are brought forward from step 8, because the content tests require one per stage the API serves) → **4** → 8 → 9. The step numbers below are kept, so references still work.

**Third change (Pete, 2026-09-15):** the prompt review is no longer a Phase 4 checkpoint. Functionality first; the prompts, the Stage 6–7 content and all other copy are reviewed together at the end, in [Phase 5 step 8](#phase-5--finish--publish), where Pete goes through the finished app as tutor and learner. The bake-off runs on the current prompts and gets a short re-run if they change then.

**Build order.** Every step ends with `dotnet build` at 0 warnings, unit tests, integration tests (LLM tests skip cleanly without an LLM), and the UI checks once UI files change.

1. **LLM provider and streaming spike** (0015)
   - `Microsoft.Extensions.AI.OpenAI` (Ollama, OpenAI) and `Anthropic` (Claude) packages; `Llm/` folder: `LlmOptions`, `LlmClientFactory` (the only provider-specific code, with OpenTelemetry middleware), provider-specific `ChatOptions` (no `temperature` for Claude), `LlmUnavailableException` → 503 guidance, trace info without the key, optional Ollama warm-up.
   - Throwaway spike: streaming from Ollama and Anthropic through `IChatClient`; check "thinking" output is off or hidden for Qwen/Gemma; confirm the Anthropic refusal stop reason. Record the results here, then delete the spike.
   - ✅ **Provider layer built 2026-09-15.** `dotnet build` 0 warnings; unit tests **181 pass (15 new)**. Built:
     - Packages: `Microsoft.Extensions.AI` and `Microsoft.Extensions.AI.OpenAI` 10.10.0 (`Microsoft.Extensions.AI.Abstractions` bumped 10.7.0 → 10.10.0, which the OpenAI adapter requires) and `Anthropic` 12.48.0.
     - `Llm/`: `LlmOptions`, `LlmProviders`, `LlmClientFactory` (OpenTelemetry under the API's own source name with sensitive data on, logging in Development, no retries, 60 s timeout), `LlmChatOptions`, `LlmTraceInfo` (an allow-list: the key can't reach the trace), `LlmUnavailableException` (→ 503 "LLM unavailable" with provider-specific guidance), `LlmWarmUpService`.
     - The `IChatClient` is a lazy singleton: a missing key or unknown provider is a 503 on the AI stages, never a failed startup. `PI.SearchApi` has a `UserSecretsId` for `Llm:ApiKey`; prompts under `assets/prompts/` are copied to the output.
   - ✅ **Ollama spike 2026-09-15** (`GET /api/spike/llm`, `qwen3.6:35b`):
     - **Thinking had to be turned off.** Through `/v1`, Qwen 3.6 and Gemma 4 both reason before answering: a 60-token request returned only reasoning and no answer. `ChatOptions.Reasoning.Effort = None` → `reasoning_effort: "none"` fixes both (Ollama's `think: false` is ignored on `/v1`). Recorded in ADR-0015.
     - **Timings:** first call 5.0 s to the first token (Ollama loading the model); then 38–55 ms to the first token and ~0.35 s for two sentences, direct and through the Vite proxy alike (39 ms). No reasoning updates; finish reason `stop`.
   - ⏸ **Anthropic spike: waiting for a key.** No Anthropic credential on this machine. The provider is built and unit-tested, and the SDK reports a refusal as `ChatFinishReason.ContentFilter`. Server-side refusal fallbacks aren't exposed through `IChatClient`: recorded as a limitation in ADR-0015. The spike endpoint is deleted (it also broke the "every OpenAPI operation has a summary" test); the Anthropic check runs through the real `/api/search/rag/answer` once `Llm:ApiKey` is set: `dotnet user-secrets set "Llm:ApiKey" "<key>" --project src/PI.SearchApi`, with `Llm__Provider=anthropic` and `Llm__Model=claude-sonnet-5`.
2. **Stage 6 — RAG API** (0016)
   - `POST /api/search/rag` (JSON results + evidence trace step) and `POST /api/search/rag/answer` (SSE, plus JSON mode for tests).
   - Evidence-set builder, including each matched concept's definition, `prefLabel` and `altLabel`s; prompt files in `assets/prompts/`; `GetStreamingResponseAsync` → `meta`/`delta`/`final`/`done`/`error` events.
   - Validation after generation: citations, `INSUFFICIENT_EVIDENCE` sentinel, incompatible-recommendation heuristic.
   - Cancellation; unbuffered response; trace with prompts, raw output and timings (time to first token, total); unit + structural integration tests.
   - ✅ **Done 2026-09-15.** `dotnet build` 0 warnings; unit tests **215 pass (34 new)**; integration tests pass with Ollama running (3 new). ADR-0016 amended first. Built:
     - `Pipeline/Rag/`: `EvidenceSetBuilder` (pure), `EvidenceFormatter`, `RagSearch` (Stage 5 + the evidence step, with the descriptions query `WHERE id = ANY(@ids)` in its trace), `AnswerGenerator` (prompt → stream → validate; its answer section is reusable by Stage 7), `CitationValidator`, `AnswerValidator`, and small records (`EvidenceSet`, `EvidenceItem`, `EvidenceRole`, `EvidenceLimits`, `EvidenceConcept`, `EvidenceRule`, `AnswerValidation`, `ValidationCheck`, `AnswerEvent`, `AnswerSectionOutcome`).
     - `Llm/LlmStreaming`: one streaming loop for both AI stages. It measures time to first token and turns network errors, HTTP errors and timeouts into `LlmUnavailableException` (→ 503); a cancellation the caller asked for is not a failure.
     - `Pipeline/PromptTemplate` (`{{name}}`, missing values throw) and `PromptLibrary`; `assets/prompts/rag-system.md` and `rag-user.md`.
     - Contracts: `AnswerMeta`, `AnswerDelta`, `AnswerFinal` (with `invalidCitations`), `AnswerDone`, `AnswerResponse`, `AnswerSections`.
     - Endpoints: `POST /api/search/rag`, `POST /api/search/rag/answer`; `AnswerStreamWriter` (SSE or JSON; a failure before the first event is an ordinary 503, after it an `error` event) and `ServerSentEventWriter` (buffering off, flush per event).
     - Stage 5 now also offers `SearchWithContextAsync` → `OntologySearchResult` (device, understood query, rule checks), so the evidence is built from typed values rather than read back out of the trace. `SearchAsync` is unchanged.
   - **Checked live** (GQ-01, `qwen3.6:35b`):
     - `/rag`: 50 results; evidence = the laptop, PROD-0012/0013/0011 Compatible and PROD-0014/0015/0016 Incompatible (ranks 44–46, after demotion); concepts Chargers (altLabels power adapter, power brick, AC adapter, PSU) and Laptops; rule chargers → laptops.
     - `/rag/answer` (JSON): cites only evidence IDs, recommends the three compatible chargers and warns about all three near misses; no invalid citations.
     - SSE: `meta`, 227 `delta`s, `final`, `done`; **first delta at 56 ms, complete in 2.3 s**.
   - Findings:
     - **The warning heuristic was too literal.** The model wrote "Avoid these incompatible options:" and then bullets that only gave reasons ("fails because it uses a barrel plug and supplies insufficient power"). Both were flagged. Bullets under a warning lead-in now count as warnings, and the word list includes the ways a failed check is stated (fails, insufficient, below, lacks). The real output is a unit test.
     - The model also cites the target device (PROD-0001). It is in the evidence, so that's valid.
3. **Stage 7 — Pedagogy API** (0017)
   - `POST /api/search/pedagogy` + `/answer` streaming the `answer` section, then the `explanation` section.
   - Two prompts: `pedagogy-system.md` (principles, fixed markdown headings, audience sections with label choice) and `pedagogy-baseline.md` (a fair, plain prompt with the same grounding rules), selected by `options.applyPedagogy`.
   - Heading parser; validator (Decision Compatible, Near miss Incompatible, concept-label heuristic), skipped for the baseline apart from citations; parsed structure in `final` (`null` for the baseline); per-section timings and the prompt used in the trace; tests.
   - ➡️ **Prompt review moved to Phase 5 step 8** (Pete, 2026-09-15): `pedagogy-baseline.md` (fair, not a straw man) and `pedagogy-system.md`, reviewed with the rest of the content once everything works.
   - ✅ **Built 2026-09-15.** `dotnet build` 0 warnings; unit tests **246 pass (31 new)**; integration tests **45 pass** (2 new, live against Ollama). ADR-0017 amended first. Built:
     - `Pipeline/Pedagogy/`: `PedagogyEngine` (the Stage 6 answer section, then the explanation), `PedagogyPromptBuilder`, `ExplanationHeadingParser`, `ExplanationValidator`, and records `PedagogyPrompt`, `ExplanationSection`, `ExplanationValidation`.
     - Prompts: `pedagogy-system.md`, `pedagogy-baseline.md`, `pedagogy-audiences.md` (one section per audience) and `pedagogy-user.md`. The user message is shared: the toggle changes only the system prompt, which a unit test asserts.
     - Contracts: `AnswerFinal.structure` (`ExplanationStructure`, `ExplanationProduct`); `EvidenceRule.SpecTerms` for the expert's words.
     - Endpoints: `POST /api/search/pedagogy` (the same retrieval and evidence as Stage 6) and `POST /api/search/pedagogy/answer`.
   - **Live side by side** (GQ-01, `qwen3.6:35b`; full JSON kept for the review):
     | | First token | Answer | Explanation | Total | Explanation warnings |
     |---|---|---|---|---|---|
     | novice, pedagogy off | 250 ms | 2.8 s | 3.7 s | 6.6 s | none; `structure: null` |
     | novice, pedagogy on | 59 ms | 2.6 s | 3.4 s | 6.0 s | none after the parser fix below |
     | expert, pedagogy on | 56 ms | 2.6 s | 3.1 s | 5.7 s | Decision cites 2 products |
     - **Baseline:** fair and useful. It lists the three compatible chargers and warns about the three near misses, but has no single decision, no named concepts and no rule of thumb.
     - **Pedagogy, novice:** Decision PROD-0011; Concepts **Connector** and **Wattage**, each explained in plain words; Near miss PROD-0016 ("looks correct because it has the right USB-C plug, but it only provides 45W"), the best counter-example in the evidence; a rule of thumb; a next step.
     - **Pedagogy, expert:** spec-first (`wattageW ≥ minChargerWattageW`), with PROD-0015 as the near miss. Validation caught the Decision naming two products.
   - Findings:
     - **Concepts took every bold word.** The novice run emphasised **USB-C** and **65W** mid-sentence, and both were flagged as "not from the ontology". A concept is now the bold term that starts a bullet, as the prompt asks. The real output is a unit test.
     - **The expert run wrote LaTeX** (`$\ge$`), which the UI won't render (no KaTeX, ADR-0014). For the prompt review: add "plain text, no LaTeX" to `pedagogy-system.md`?
     - **Decision sometimes names two products** when two chargers fit equally. The validator warns, as designed. For the prompt review: whether to strengthen "exactly one" (for example "if several fit, choose one and say why").
     - The near-miss choice varies between runs (PROD-0016 or 0015): both are valid. The bake-off will show how often each structure rule holds over 10 runs.
4. **Model bake-off** (0015)
   - Opt-in `BakeOff/ModelBakeOffTests` (`PI_BAKEOFF_MODELS`): the 7 golden queries with a query, 10 runs each, Stage 6 and Stage 7 (pedagogy on and off) in JSON mode. Record structure, citation warnings, sentinel, time to first token and totals in ADR-0015; choose the Ollama default.
   - ✅ **Harness built 2026-09-15.** `BakeOff/ModelBakeOffTests` and `BakeOffCollection` (never runs alongside the shared AppHost fixture, which uses the same data volume). For each model it starts its own AppHost with `Llm__Model` set on the `searchapi` resource, sends one untimed warm-up request, then runs every scenario `PI_BAKEOFF_RUNS` times (10 by default). It scores what the API's validators report (invalid citations, warnings, the explanation's non-heuristic structure checks, the sentinel) plus first-token and total times, and writes `TestResults/model-bake-off-{time}.md` (git-ignored), with a table per query and the most common warnings.
   - **Smoke run** (`qwen3.6:35b`, 1 run, 2 minutes) found two validator faults, now fixed with unit tests; unit tests **248 pass**:
     - **The target device counted as a choice.** "For your laptop [PROD-0001], get …" made Decision "cite 2 products". Decision and Near miss now ignore citations of the target device.
     - **No device, no rule words.** Without a target device no rule is checked, so the evidence had no rules and the concept heuristic flagged "Voltage vs. Platform". It now also reads the products' compatibility reasons, which quote the rules.
     - Worth noting for ADR-0015: Stage 7's answer reaches its first token faster than Stage 6's (about 40 ms against 600 ms), probably because Ollama reuses the cached prompt Stage 6 just sent. And without a target device (GQ-02, GQ-03, GQ-07) the answer can start with `INSUFFICIENT_EVIDENCE`: 3 of 7 queries in the smoke run.
   - ✅ **Done 2026-09-16. Default: `qwen3.6:35b`** (decided by Pete once gemma's speed was clear). Full results in [ADR-0015](0015-llm-hosting-and-client.md) § Bake-off results: 210 requests, none failed, **no invalid citations at all**, structure checks 65/70, Stage 6 median 2.3 s, Stage 7 median 5.4 s, first token 43–73 ms. Every acceptance criterion for the AI stages is met by this model.
     - **`gemma4:31b` was abandoned mid-run.** It generates ~22 tokens/second on this laptop (a dense 31B model runs every weight per token), so a request took ~15 s and Stage 7 ~30 s, twice the talk's budget; holding both models also pushed the 64 GB machine into swap. Its half of the run was stopped after three hours.
     - **The report is now written after each model**, not at the end: stopping gemma lost qwen's completed results the first time.
     - **macOS throttling, not the model:** with the display asleep the run took three hours of wall-clock for ~20 minutes of model time, roughly one call every 30 s, even under `caffeinate -i`. Use `caffeinate -dimsu`. The report's timings are the API's own, so they are unaffected.
     - **Two faults the bake-off exposed, both fixed with unit tests:** with no target device the evidence carried no rules at all (the applicable rules are now named from the ontology), and a concept named after a product's own spec ("Capacity (Ah)", "Form Factor") was flagged as "not from the ontology" (spec names now count as grounded).
     - **30 of 210 answers began with `INSUFFICIENT_EVIDENCE`:** exactly the three golden queries with no target device (GQ-02, GQ-03, GQ-07), where no rule can be checked. Correct behaviour, and a talk moment: the answer says what it can't confirm.
   - Done alongside: README "Getting started" (Node, LLM set-up with Ollama or Anthropic, the web UI under `aspire run`, the Stage 6–7 endpoints, UI checks, the bake-off command); `architecture.md` (the `Llm/` folder, the six prompt files, `useAnswerStream`, `answerEvents.ts`, `BakeOff/`, and LLM settings living on the API, not the AppHost); `src/PI.SearchApi/CLAUDE.md` layout table; the Answer panel shows long waits in seconds ("6.2 s") via `formatDuration`.
5. **UI plumbing** (0014)
   - `gen:api`; `answerEvents.ts`; a pure incremental SSE parser; `useAnswerStream` (fetch + parser, in parallel with the results request, `AbortController`); Stages 6–7 selectable.
6. **Answer tab and stage options** (0014, [design](../design/screens/stage-7-answer.png))
   - Enable the **Answer tab**: `AnswerPanel` (streamed answer, `[PROD-…]` chips, citation validation badges, time to first token), `ExplanationPanel` (Stage 7's five headings, or the baseline), `EvidenceSet` (what the model was given; chips jump here). The Results tab badge shows results are ready before the first token.
   - **Audience picker** and Stage 7's **"Apply pedagogy" toggle** in `StageOptions` on the tab row (both part of URL state).
7. **Under the hood for Stages 6–7** (0014, 0003)
   - `PromptView`, `EvidenceView`, `GenerationView` renderers; the answer's `done` trace appended to the search trace; the coverage test extended to Stages 6–7.
   - ✅ **Steps 5–7 built together 2026-09-15** (brought forward for the prompt review). Typecheck, lint, Prettier and build pass; Vitest **266 pass (47 new)**. Checked in headless Chrome at 1280×900 against the live API: GQ-01 on Stage 7, novice, pedagogy on. Built:
     - **Plumbing:** `gen:api` (the stage type now excludes the `/answer` sub-paths, so `SearchStage` is the seven stages); `api/answerEvents.ts` (hand-typed events, checked at runtime); `lib/parseSseEvents.ts`; `streamAnswer` in `client.ts`; `hooks/useAnswerStream.ts` (runs in parallel with `usePipelineSearch`, aborts on change, a stream that closes without `done` is an error).
     - **Answer tab:** `AnswerTab`, `AnswerPanel`, `ExplanationPanel`, `EvidenceSet`, `CitationChip`, `CitationSummary`, `AnswerWarnings`, `StreamingBadge`; `lib/citations.ts` (links `[PROD-…]` to a `product:` scheme `Markdown` renders as a chip, and hides the sentinel line). Chips are coloured by the product's verdict in the evidence set, marked invalid when `final` says so, and select the product in the evidence set.
     - **Options and tabs:** the audience picker and Apply pedagogy switch on Stage 7; "50 ready" on Results and "live" on Answer while streaming; Stages 6–7 selectable in the stepper; Stages 6–7 show Stage 5's grouped results and signals.
     - **Under the hood:** `EvidenceView`, `PromptView` (full system and user messages; the audience section highlighted; words offered per concept), `GenerationView` (raw output, timings, per-section timings), `ValidationView` (each check, heuristics labelled, parsed structure), `LlmSettings`. The answer's trace steps follow the results' steps; chips read Evidence → Prompt → Generate → Validate → Explain: prompt / generate / validate.
     - **Content (from step 8):** `content/stages/rag.md` and `pedagogy.md`; 8 glossary entries (evidence set, citation, prompt, SSE, time to first token, pedagogy, baseline explanation, audience); talk steps `stage-rag`, `stage-pedagogy-baseline`, `stage-pedagogy`, `stage-pedagogy-audience` with draft captions. The talk-order test allows Stage 7's repeated steps.
   - Findings:
     - **Cold model after idle:** the browser run's first token took 6.2 s (answer and explanation 12.1 s), against 56 ms when warm. Ollama unloads a model after 5 minutes idle by default, and the warm-up service only runs at API startup. For step 9: either set Ollama's `keep_alive` longer on the presenter laptop, or warm up again before the talk.
     - Long first-token times read as "6231.00 ms"; a seconds format above 1 s would read better on the projector.
8. **Content, talk mode and glossary**
   - Stage explanations for `rag` and `pedagogy`; talk steps `stage-rag`, `stage-pedagogy-baseline`, `stage-pedagogy`, `stage-pedagogy-audience`; glossary entries; content-integrity tests for Stages 1–7.
9. **Acceptance run and ADR status**
   - ✅ **Live run 2026-09-16, through the Vite proxy** (`qwen3.6:35b`, GQ-01 "power adapter for my laptop" with the Aerobook as the target device):
     | Request | First token | Total | Result |
     |---|---|---|---|
     | `/api/search/rag` (results) | — | **213 ms** | 50 results, no LLM call |
     | Stage 6 answer | 966 ms (model loading) | 3.4 s | 7 citations, none invalid, no warnings |
     | Stage 7 novice, baseline | 38 ms | 5.9 s | 7 citations, none invalid, no warnings |
     | Stage 7 novice, pedagogy | 67 ms | 5.8 s | Decision PROD-0011, near miss PROD-0016, no warnings |
     | Stage 7 expert, pedagogy | 71 ms | 5.8 s | Decision PROD-0011, near miss PROD-0015, no warnings |
     - Results are ready in a fifth of a second, long before the first token; the first token lands in well under 1.5 s once the model is loaded, and Stage 7's two calls finish in under 6 s against the ~15 s budget. Switching audience changed the near miss it chose and the wording, not the decision.
   - ✅ **Mid-stream cancellation cancels generation** (the check left open since Phase 3 step 2), proven in Ollama's request log through the Vite proxy: a completed Stage 7 request logs **two** calls (answer 3.0 s, explanation 3.4 s); aborting at the first delta logs **one and no second**, so the explanation call never starts; aborting right after the `meta` event logs **none at all**, with 30 s of slack for one to appear.
   - ✅ **With no LLM reachable, everything else still works** (checked 2026-09-16 by starting the AppHost with `Llm__Endpoint=http://localhost:11999`, so Ollama itself was left running):
     - All seven stages still return their results: Stage 1 60, keyword 4, the rest 50, with Stages 6–7 keeping their 9 trace steps including the evidence step.
     - Both answer endpoints in JSON mode return **503 "LLM unavailable"** with the fix: *"Is Ollama running at http://localhost:11999? Start it with `ollama serve`, and make sure the model is pulled: `ollama pull qwen3.6:35b`. (Connection refused)"*.
     - The event stream returns `200 text/event-stream` with `meta` (the evidence IDs) and then an `error` event carrying that ProblemDetails, because `meta` is sent before the model is called. ADR-0016 amended to say so.
     - In the UI the Answer tab reads **failed**, shows the guidance, "The results are unaffected: they come from a separate request" and a Try again button, while Results still says **50 ready** and the evidence set is listed in full.
   - ✅ **Talk mode walks Stages 1–7 by keyboard:** → visits **29 positions** in order, from the intro through the four new Stage 6–7 steps (RAG's four tabs, then the baseline, pedagogy and expert steps) to Going further and Summary, and ← returns through exactly the same positions in reverse. A Stage 7 step arrives with its preset applied (`stage-pedagogy-baseline`: novice, Apply pedagogy off).
   - The acceptance criteria below, live under `aspire run` (Ollama and Anthropic), including mid-stream cancellation through the Vite proxy (left open in Phase 3 step 2); README, `architecture.md` and `src/PI.SearchApi/CLAUDE.md` updated; ADRs 0015–0017 → Accepted.

### Acceptance criteria
- ✅ GQ-01 in Stage 6 gives a grounded answer citing the compatible charger and warning about the near miss: 7 citations, none invalid, no warnings; across the bake-off's 210 requests **no citation ever fell outside the evidence**.
- ✅ **GQ-01 in Stage 7, `novice`, pedagogy off:** free-form, no citation warnings; `structure: null`, and the trace step reads "Validate: citations (structure checks not applied)".
- ✅ **The same with pedagogy on:** all five headings, Decision on a compatible charger (PROD-0011), Connector and Wattage explained, and the near miss (PROD-0016, the 45W USB-C charger that "looks correct") as the counter-example.
- ✅ Switching audience changes the wording, not the facts: novice gets plain words and an analogy, expert is spec-first (`wattageW ≥ minChargerWattageW`); the decision stayed PROD-0011.
- ✅ Results render before any LLM text (**213 ms**, no LLM call, while the tab reads "50 ready"); first token **38–73 ms** warm, and Stage 7's answer plus explanation complete in **5.8 s** against the ~15 s budget. A cold model adds a few seconds to the first request: warm up before the talk.
- ✅ Switching stage mid-stream cancels generation, proven in Ollama's request log: a completed Stage 7 request logs two calls, aborting at the first delta logs one and no second, and aborting at `meta` logs none at all.
- ✅ With no LLM reachable, Stages 6–7 still show results and the evidence set, the Answer tab shows the 503 guidance and a Try again button, Stages 1–5 are unaffected, and the LLM integration tests skip with a message (41 pass, 5 skip, 0 fail).
- ✅ ADRs 0016 and 0017 → **Accepted**. ADR-0015 → **Accepted for Ollama**; the Anthropic provider is built and unit-tested but has not been run live, for want of an API key.

### Phase 4 status ✅ closed 2026-09-16

- Steps 1–9 are built and verified: `dotnet build` 0 warnings, **250 unit tests**, **45 integration tests** (LLM tests skipping cleanly without an LLM), **267 UI tests**, plus UI typecheck, lint, Prettier and build.
- ➡️ **Carried into Phase 5 step 8:** the live Anthropic check once a key is set; the prompt and content review; and a warm-up (or a longer Ollama `keep_alive`) before the talk, so the first answer isn't the cold one.

### Open questions
- ✅ Default Ollama model: **`qwen3.6:35b`** (bake-off, 2026-09-16; `gemma4:31b` rejected on speed). See [ADR-0015](0015-llm-hosting-and-client.md) § Bake-off results.
- ✅ Default hosted model for learners: Anthropic `claude-sonnet-5` (decided 2026-09-15). OpenAI's is set at publish time (Phase 5 step 7).
- ➡️ Wording of `pedagogy-baseline.md` (fair, not a straw man): moved to Phase 5 step 8, with the other prompt questions.

---

## Phase 5 — Finish & publish

1. ✅ **Dataset growth (done 2026-09-16):** `tools/PI.CatalogGenerator` (C# console) adds 240 template-built distractors after the 60 curated products (300 total; ~500 was tried and cut, see ADR-0005 teaching notes). `nomic.jsonl` rebuilt (curated vectors unchanged). Golden queries adapted: GQ-02 vector/hybrid bound top 5 → top 10 and its moment text (power banks outrank the chargers); the integration client reads every page; GQ-08 checks the target-device reason only when the laptop is retrieved. **UI fix found by the growth:** fusion can retrieve more than one page of 50 (GQ-08 returns 67, flagged items last), so `usePipelineSearch` now reads every page for ranked stages (ADR-0014 amended; ADR-0003 wording on `totalResults` corrected). 250 unit, 45 integration (Stages 6–7 on Ollama) and 269 UI tests pass.
2. **README (final pass; kept current since Phase 1):** prerequisites (.NET 10, Docker, Node LTS, Aspire CLI, Hugging Face CLI, and either Ollama or an OpenAI/Anthropic API key), model download, `aspire run`, a tour of the 7 stages, how to reset the data volume, troubleshooting.
3. **CI:** OpenAPI → TypeScript drift check; optional manual integration-test workflow.
4. **Talk content & rehearsal:**
   - The talk-mode steps, the **"Going further" step** ([ADR-0018](0018-scope-and-going-further.md)) and its glossary entries were drafted in Phase 3 (step 10); Stages 6–7 steps are added in Phase 4. The copy itself is reviewed in step 8. No agent protocols.
   - Rehearse the full talk end to end in the UI (there are no slides). `nomic.jsonl` was rebuilt after dataset growth; GQ-01's evidence set for Stages 6–7 may now include generated chargers (the Stage 6–7 integration tests pass on Ollama), so look at the answers during the prompt review.
1a. ✅ **Stage 5 without a target device (added and done 2026-09-16, ADR-0013 amended):** requirements stated in the query ("65W", "USB-C") are checked with the same rules when no device is given (`compatibility.source: Query`), and every rule-bound product lists the catalog devices it fits (`compatibility.fits`). Evidence for Stages 6–7 carries both. New GQ-09 ("65W USB-C charger"); GQ-07 now also flags the barrel charger. UI: "you asked for" on flagged cards and rule checks, "Fits N of M" on rows and cards, requirements in the understand-step trace; UI types regenerated. 271 unit, 48 integration (Stages 6–7 on Ollama) and 272 UI tests pass. One GQ-09 Stage 6 run cited outside the evidence in the first full run and couldn't be reproduced in 13 more runs; the test now reports the IDs and text if it recurs.
5. ✅ **Public ADRs (drafted 2026-09-16, for Pete's review):** 18 learner-facing ADRs plus an index in `docs/decisions/`, same numbers and file names, 0012 kept as Rejected; all others Accepted. Written from each ADR's decisions and teaching notes, with build history removed and numbers re-measured on the 300-product catalog. The UI's `/decisions` glob, code-comment `Decision:` links, CI comment and CLAUDE.md ADR links now point at `docs/decisions/`; the root CLAUDE.md still lists the working index, architecture and roadmap. **Check after step 7:** ADR-0009 and ADR-0015 describe the OpenAI providers without test results, and ADR-0015 says the README names the suggested OpenAI model.
6. **Final review:** code comments read as teaching material; every stage file opens with its technique / strength / failure-mode comment; all ADRs **Accepted**, **Rejected** or explicitly superseded.
7. **OpenAI providers (when credits allow)** (0009, 0015): test OpenAI embeddings (`Embeddings:Provider = openai`, `Rebuild: true` → commit `openai.jsonl`) and OpenAI chat; adjust golden-query expectations if needed; document the one-key setup in the README.
8. **Clean-up and sign-off (Pete, once the whole demo is complete)** (0014):
   - **Copy and talk review:** every piece of UI text Pete hasn't written yet. The talk steps (`content/talk.json`, `content/talk/*.md`: intro, the needle, each stage caption, Going further, summary), the stage explanations (`content/stages/*.md`), the glossary (`content/glossary.json`), the Home thesis (`content/home.md`), and short labels in components (filter hints, empty states, trace section titles).
   - **Speaker details** in `content/speaker.md`, with the photo and LinkedIn QR code in `content/images/`.
   - **Prompt review** (moved from Phase 4 step 3, 2026-09-15), in `src/PI.SearchApi/assets/prompts/`, with GQ-01 on Stage 7 (novice off, novice on, expert) side by side in the Answer tab and the prompts in Under the hood:
     - Is `pedagogy-baseline.md` a fair first prompt, not a straw man?
     - `pedagogy-system.md`: add "plain text, no LaTeX"? The expert run wrote `$\ge$`, which the UI doesn't render.
     - `pedagogy-system.md`: should Decision always choose one product ("if several fit, choose one and say why")? It sometimes names two.
     - `rag-system.md`, `rag-user.md`, `pedagogy-audiences.md`, `pedagogy-user.md`: wording.
     - If a prompt changes, re-run the bake-off for the default model (Phase 4 step 4) and the Stage 6–7 integration tests.
   - **Stage 6–7 content drafted in Phase 4:** `content/stages/rag.md` and `pedagogy.md`; talk steps `stage-rag`, `stage-pedagogy-baseline`, `stage-pedagogy`, `stage-pedagogy-audience` and their captions; the AI-stage glossary entries; UI labels in the Answer tab and the Stage 6–7 trace views.
   - **Deferred:** the laptop-charger icon (`laptop-chargers` → `laptop` in the TTL; `plug` suggested).
   - **Checks carried over from Phase 3:** fonts load with the network disconnected; an ontology label edit appears in the filters after re-running the AppHost; the talk starts from Home by keyboard alone; the CI `web-ui` job is green on GitHub.
   - Then **ADR-0014 → Accepted**.

### Acceptance criteria
- A fresh clone on a clean machine runs end to end by following the README alone.
- The whole golden-query suite passes at ~300 products.
- Public ADRs are published; the full talk, including the going-further step, is rehearsed end to end in the UI's talk mode.
- Pete has reviewed the copy and the talk, filled in the speaker details, and run the checks carried over from Phase 3 (step 8).
- ADR-0014 → **Accepted**.
- ADR-0018 → **Accepted**.

### Open questions
- ✅ No slides: the talk lives in the web UI ([ADR-0014](0014-web-ui-architecture.md)).
- ✅ Public ADR location: `docs/decisions/`. The working ADRs, `architecture.md` and `roadmap.md` stay on the build branch and are **not merged to `main`** (decided 2026-09-16). At merge time, drop `docs/adr/` and repoint the root CLAUDE.md "Read before you build" list and the README's roadmap link.
- ✅ Generator script language: C# console in `tools/` (decided 2026-09-16).

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
| 0014 | 3 (+ AI panels in 4, clean-up, copy review and acceptance in 5) |
| 0015–0017 | 4 |
| 0018 | 0–2 (rework), 4 (pedagogy baseline), 5 (going-further content) |

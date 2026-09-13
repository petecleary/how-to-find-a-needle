# Roadmap: How to Find a Needle

From today's cleaned-up scaffold to a finished, teachable demo. Each phase lists its tasks, the ADRs they implement, the acceptance criteria that mark it done, and the open questions to resolve along the way.

> Private working document (see [ADR-0001](0001-record-architecture-decisions.md)). The repo is one finished codebase on `main`; phases are a build order, not branches learners check out.

**Order:** Data → Search APIs (stages 1–6) → Frontend → AI stages (7–8) with their UI → Finish & publish.

**Rules for every phase**
- Build passes with **0 warnings** (`TreatWarningsAsErrors`).
- Unit tests pass. Integration tests pass for every stage implemented so far.
- An ADR moves to **Accepted** only when its acceptance criteria are met and verified.
- Update [architecture.md](architecture.md) if anything built differs from the plan, and update the ADR first.

---

## Phase 0 — Clean-up ✅ (done)

- Removed unused packages (Redis output caching, `Aspire.Hosting.JavaScript`, `Aspire.Npgsql` in the AppHost); patched the `Microsoft.OpenApi` vulnerability via `Microsoft.AspNetCore.OpenApi` 10.0.12.
- `.gitignore`: keep `appsettings.Development.json`; ignore downloaded models except their README; `docs/adr/` stays private.
- Removed the dead Datafiniti CSV import; the API fails fast without its connection string; removed `UseFileServer`; tidied template comments.
- Fixed the models README (correct files and paths for Nomic and BGE-M3).

---

## Phase 1 — Data

**ADRs:** [0002](0002-solution-structure-and-orchestration.md), [0005](0005-curated-dataset-and-golden-queries.md), [0006](0006-database-schema-and-seeding.md), [0013](0013-domain-ontology-and-compatibility.md) (vocabulary and facts only)

### Tasks

1. **Solution conventions** (0002)
   - Add `Directory.Build.props` (nullable, warnings as errors, analysers), `Directory.Packages.props` (central versions) and `.editorconfig`.
   - Pin the Postgres image tag in `AppHost.cs`.
2. **Test projects** (0002)
   - `tests/PI.SearchApi.Tests` (xUnit) and `tests/PI.SearchApi.IntegrationTests` (xUnit + `Aspire.Hosting.Testing`), added to the `.slnx`.
   - One smoke test each (a health check for integration).
3. **CI** (0002)
   - GitHub Actions workflow that builds and runs unit tests on every push.
4. **Golden queries first** (0005)
   - Draft `assets/data/golden-queries.json` (GQ-01 to GQ-07) with the talk moment each demonstrates. ❓ Review with Pete.
5. **Catalog** (0005)
   - Author `assets/data/products.json` (~60 items) to create each golden-query moment: targets, correct answers, near misses, keyword traps, filler.
   - Write a JSON schema file (`products.schema.json`) to catch typos.
6. **Ontology** (0013)
   - `assets/data/domain-ontology.ttl`: SKOS taxonomy (the categories, with icons and definitions), synonyms and multilingual labels, value vocabularies (connectors, storage interfaces, memory types, platforms) and class-level domain rules. **No product IDs.**
   - Authored alongside the catalog, because product categories and constrained spec values use its notations.
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

### Acceptance criteria
- `aspire run` starts Postgres and the API; the log shows `Seeded 60 products (60 inserted, 0 updated, 0 deleted)`.
- A second run logs `0 inserted, 0 updated`, and startup is noticeably faster.
- Editing one product's description updates exactly that row and nulls its embeddings.
- `dotnet test` passes, including the catalog validation tests.

### Resolved
- ✅ Fictional brands and golden queries GQ-01 to GQ-07 (GQ-06 is an SSD interface mismatch rather than camera lenses). Draft wording is still reviewed in task 4.
- ✅ Category icons, no product images.
- ✅ GBP, with `en-GB` price formatting.
- ✅ The repo is on GitHub, so the CI task applies.

---

## Phase 2 — Search APIs (stages 1–6)

**ADRs:** [0003](0003-search-api-contract-and-debug-trace.md), [0004](0004-pipeline-composition.md), [0007](0007-structured-search.md), [0008](0008-keyword-search-bm25-style.md), [0009](0009-local-embeddings-onnx-runtime.md), [0010](0010-vector-search-pgvector.md), [0011](0011-hybrid-search-rrf.md), [0012](0012-bge-m3-dense-and-sparse.md), [0013](0013-domain-ontology-and-compatibility.md)

Build strictly in this order. Each step ends with its golden-query integration tests passing.

1. **Contract** (0003, 0004)
   - `Contracts/` types, `Pipeline/` shared types (`Candidate`, `StageResult`, `TraceStep`), `SqlFilterBuilder`, ProblemDetails mapping for 503/502.
   - `IOntology` basics: load the TTL; label, taxonomy and narrower-concept lookups (needed by Stage 1 category filters).
   - `GET /api/demo/queries`, `GET /api/demo/devices` and `GET /api/taxonomy`.
   - Integration test harness that runs golden-query expectations per stage.
2. **Stage 1 — Structured** (0007)
   - `POST /api/search/structured`; `categories &&` filter with narrower-concept expansion; JSONB `@>` spec filters; `COUNT(*)` total; SQL in trace. ✅ GQ-04.
3. **Stage 2 — Keyword** (0008)
   - `websearch_to_tsquery` + `ts_rank_cd`; parsed tsquery and matched lexemes in trace; "BM25-style" notes. ✅ GQ-02 (misses), GQ-03 (trap ranks high).
4. **Embeddings (Nomic)** (0009)
   - `INomicEmbedder` (tokenizer, prefixes, mean pooling, L2); verify the tokenizer file requirement and update the models README; seeder backfill for `embedding_nomic`; unit tests for pooling and normalisation.
5. **Stage 3 — Vector** (0010)
   - pgvector cosine; `SET LOCAL hnsw.ef_search`; distances in trace. ✅ GQ-02 (hit), GQ-01 (near miss ranks high).
   - ⚠️ If the embeddings don't produce the expected moments, iterate on product *wording* ([ADR-0005](0005-curated-dataset-and-golden-queries.md) workflow).
6. **Stage 4 — Hybrid** (0011)
   - Pure `ReciprocalRankFusion` with exhaustive unit tests; concurrent Keyword + Vector; per-item formula strings in trace. ✅ GQ-03 corrected; GQ-01 near miss still present.
7. **BGE-M3 verification** (0012) ⏱ ~1 hour
   - Reuse the earlier in-memory BGE-M3 approach. Confirm tokenizer parity (EN/ES/DE), ONNX outputs and a cross-lingual cosine sanity check against this export. Update ADR-0012 only if something differs.
8. **Stage 5 — BGE-M3** (0012)
   - Dense + sparse embedder; `SparseVector` index-base conversion tests; seeder backfill; dense and sparse retrieval fused via `IRankFusion`; tokens and sparse weights in trace. ✅ GQ-07.
9. **Stage 6 — Ontology** (0013)
   - `IOntologySearch`: understand (label matcher) → expand (keyword OR-groups + expanded embedding text) → Hybrid → classify (in/out of concept) → constrain (class-level rules vs target-device specs).
   - Toggles `expandSynonyms` / `applyConstraints`; flagged items kept with reasons; one trace step per step.
   - Unit tests for label matching, expansion, the tsquery builder, classification and each rule operator. ✅ GQ-01, GQ-02 (keyword side rescued), GQ-03 (phone battery out of concept), GQ-05, GQ-06.

### Acceptance criteria
- All 6 endpoints appear in Scalar and return the shared contract with a populated `debugTrace`.
- The integration suite shows the talk's story as passing tests: synonym miss → vector hit; keyword trap → hybrid fix; near miss → ontology flag with reason; cross-language → BGE-M3 hit.
- Missing ONNX models give a `503` with fix-it guidance, not a stack trace.
- ADRs 0003, 0004 and 0007–0013 → **Accepted**.

### Open questions
- ❓ Language for GQ-07 (proposed Spanish).
- ❓ Can the earlier in-memory BGE-M3 code be shared, so Stage 5 reuses its tokenizer and pooling approach?
- ❓ Keep `reviews` out of `search_vector`? Revisit after GQ-02 and GQ-03 results.

---

## Phase 3 — Frontend (stages 1–6)

**ADRs:** [0014](0014-web-ui-architecture.md)

1. Scaffold `src/web-ui` (Vite + React + TS strict, Tailwind, shadcn/ui init, Lucide, ESLint, Prettier, Vitest, `.nvmrc`).
2. Re-add `Aspire.Hosting.JavaScript`; `AddViteApp` with `WithReference(searchApi)`; Vite `/api` proxy from the service-discovery environment variable.
3. `npm run gen:api` with `openapi-typescript` → committed `src/api/schema.d.ts`; typed `fetch` client.
4. `usePipelineSearch` hook (same request across stages, AbortController, URL state) + Vitest tests.
5. Layout: `SearchBar` (golden-query presets, device picker), `FilterBar`, `PipelineStepper` (keyboard ←/→), `ResultCard` with `SignalBadges` and `CompatibilityBadge`.
6. `DebugDrawer` renderers: `SqlBlock`, `TsQueryView`, `DistanceTable`, `RrfTable`, `TokenWeights`, `ConceptMatches`/`ExpansionView`/`RuleChecks`, JSON fallback.
7. Presentation mode (large type, hidden filters); accessibility pass.
8. CI: add `typecheck`, `lint` and `build` for `web-ui`.

### Acceptance criteria
- `aspire run` opens the UI. Choosing GQ-01 and stepping 1 → 6 shows the results changing and the near miss flagged in Stage 6 with its reason.
- Every trace step renders with a purpose-built view (no raw JSON for stages 1–6).
- Readable on a 1280×720 projector in presentation mode; keyboard-only operable.
- ADR-0014 → **Accepted** (for stages 1–6).

### Open questions
- ❓ Visual style and branding for the talk (colours, logo), if any.

---

## Phase 4 — AI stages (7–8) with UI

**ADRs:** [0015](0015-llm-hosting-and-client.md), [0016](0016-rag-grounding-and-citations.md), [0017](0017-pedagogy-engine.md)

1. **LLM provider** (0015)
   - `Llm` settings (`Provider` = `ollama` | `openai` | `anthropic`, `Model`, `Endpoint`); API keys in user secrets; no containers.
   - `LlmClientFactory` builds one `IChatClient` (`Microsoft.Extensions.AI.OpenAI` for Ollama and OpenAI; the official `Anthropic` SDK for Claude) with OpenTelemetry middleware. Sampling parameters are provider-specific (no `temperature` for Claude).
   - 503 guidance; optional warm-up.
2. **Model bake-off** (0015)
   - Run the golden queries 10× each against the candidate models; record JSON validity, citation correctness and latency in ADR-0015; choose the default.
3. **Stage 7 — RAG** (0016)
   - Evidence-set builder, prompt files in `assets/prompts/`, JSON schema output, one retry on invalid JSON, citation validator, warnings; trace with prompts and raw output; unit + structural integration tests.
4. **Stage 8 — Pedagogy** (0017)
   - Audience-aware prompt, output schema, validator (decision Compatible, near miss Incompatible), trace; tests.
5. **UI** (0014)
   - `AnswerCard` with citation chips that scroll to result cards; `ExplanationCard` (decision → concepts → near miss → rule of thumb → next step); audience switcher; `PromptView` trace renderer; loading states for multi-second calls.

### Acceptance criteria
- GQ-01 in Stage 7 gives a grounded answer citing the compatible charger and warning about the near miss. Stage 8 explains connector + wattage with the near miss as a counter-example.
- Switching audience visibly changes the explanation without changing the facts.
- With Ollama stopped, stages 7–8 return 503 with guidance while stages 1–6 keep working.
- Stage 7 + 8 complete in under ~10 s combined on the presenter laptop (local Ollama).
- ADRs 0015–0017 → **Accepted**.

### Open questions
- ❓ Default model (from the bake-off).
- ❓ Default hosted model IDs for learners (OpenAI and Anthropic). Confirm when Phase 4 starts, since model versions move quickly.

---

## Phase 5 — Finish & publish

1. **Dataset growth:** a generator script (in `tools/`, language to be decided) adds distractors to reach ~500 products without disturbing the curated core; golden-query tests still pass.
2. **README:** prerequisites (.NET 10, Docker, Node LTS, Aspire CLI, Hugging Face CLI, and either Ollama or an OpenAI/Anthropic API key), model download, `aspire run`, a tour of the 8 stages, how to reset the data volume, troubleshooting.
3. **CI:** OpenAPI → TypeScript drift check; optional manual integration-test workflow.
4. **Demo script:** stage-by-stage talk script tied to golden queries and UI bookmarks (location to be decided, see open questions).
5. **Public ADRs:** write learner-facing ADRs from each ADR's *Teaching notes*; choose their public location; decide what happens to this private folder.
6. **Final review:** code comments read as teaching material; every stage file opens with its technique / strength / failure-mode comment; all ADRs **Accepted** or explicitly superseded.

### Acceptance criteria
- A fresh clone on a clean machine runs end to end by following the README alone.
- The whole golden-query suite passes at ~500 products.
- Public ADRs are published; the talk demo script is rehearsed against the build.

### Open questions
- ❓ Does `presentation/` (slides, speaker notes) live in this repo?
- ❓ Public ADR location (`docs/decisions/`?) and whether the private ADRs are archived or deleted.
- ❓ Generator script language (C# console in `tools/` proposed, to avoid adding Python).

---

## Traceability

| ADR | Phase(s) |
|---|---|
| 0001 | all (process) |
| 0002 | 1 (+ CI in 3, 5) |
| 0003, 0004 | 2 |
| 0005 | 1 (+ growth in 5) |
| 0006 | 1 (+ backfill in 2) |
| 0007–0012 | 2 |
| 0013 | 1 (vocabulary), 2 (stage) |
| 0014 | 3 (+ AI panels in 4) |
| 0015–0017 | 4 |

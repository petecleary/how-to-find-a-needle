# Roadmap: How to Find a Needle

From today's cleaned-up scaffold to a finished, teachable demo. Each phase lists its tasks, the ADRs they implement, the acceptance criteria that mark it done, and the open questions to resolve along the way.

> Working document on the build branch (see [ADR-0001](0001-record-architecture-decisions.md)). The repo is one finished codebase on `main`; phases are a build order, not branches learners check out.

**Order:** Data → Search APIs (stages 1–4, 6) → Frontend → AI stages (7–8) with their UI → Finish & publish → *optional* Stage 5 BGE-M3, always last.

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
| **Must** (the talk works) | Data and ontology; Stages 1–4 and 6; Stage 7 with streaming; demo screen; talk mode with stage explanations; golden-query tests for stages 1–6; README "Getting started" |
| **Should** | Stage 8 pedagogy; glossary with hover terms; ADR pages; CI |
| **Could** | ~500-product growth; OpenAI providers; public ADR rewrite; OpenAPI drift check in CI; Stage 5 BGE-M3 (always last) |

**Weekly shape**

| Week | Focus |
|---|---|
| 1 | Phase 1 (data, ontology, seeding) and Phase 2 through Stage 3 (vector) |
| 2 | Stage 4 (hybrid) and Stage 6 (ontology); Phase 3 UI scaffold with the demo screen |
| 3 | Phase 4 (Stages 7–8 streaming) and talk mode with content |
| 4 | **Protected:** rehearsals, polish, and fixing what rehearsal exposes |

---

## Phase 0 — Clean-up ✅ (done)

- Removed unused packages (Redis output caching, `Aspire.Hosting.JavaScript`, `Aspire.Npgsql` in the AppHost); patched the `Microsoft.OpenApi` vulnerability via `Microsoft.AspNetCore.OpenApi` 10.0.12.
- `.gitignore`: keep `appsettings.Development.json`; ignore downloaded models except their README; `docs/adr/` was initially ignored and is now tracked on the build branch.
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
11. **README "Getting started" (minimal)**
    - Prerequisites so far, `aspire run`, and how to reset the data volume.
    - Extend it at the end of every phase, and verify each phase from a clean clone.

### Acceptance criteria
- `aspire run` starts Postgres and the API; the log shows `Seeded 60 products (60 inserted, 0 updated, 0 deleted)`.
- A second run logs `0 inserted, 0 updated`, and startup is noticeably faster.
- Editing one product's description updates exactly that row and nulls its embeddings.
- `dotnet test` passes, including the catalog validation tests.
- A clean clone runs Phase 1 by following the README alone.

### Resolved
- ✅ Fictional brands and golden queries GQ-01 to GQ-07 (GQ-06 is an SSD interface mismatch rather than camera lenses). Draft wording is still reviewed in task 4.
- ✅ Category icons, no product images.
- ✅ GBP, with `en-GB` price formatting.
- ✅ The repo is on GitHub, so the CI task applies.

---

## Phase 2 — Search APIs (stages 1–6)

**ADRs:** [0003](0003-search-api-contract-and-debug-trace.md), [0004](0004-pipeline-composition.md), [0007](0007-structured-search.md), [0008](0008-keyword-search-bm25-style.md), [0009](0009-local-embeddings-onnx-runtime.md), [0010](0010-vector-search-pgvector.md), [0011](0011-hybrid-search-rrf.md), [0013](0013-domain-ontology-and-compatibility.md)

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
   - Pure `ReciprocalRankFusion` with exhaustive unit tests; concurrent Keyword + Vector; per-item formula strings in trace. ✅ GQ-03 corrected; GQ-01 near miss still present.
7. **Stage 6 — Ontology** (0013)
   - `IOntologySearch`: understand (label matcher) → expand (keyword OR-groups + expanded embedding text) → Hybrid → classify (in/out of concept) → constrain (class-level rules vs target-device specs).
   - Toggles `expandSynonyms` / `applyConstraints`; flagged items kept with reasons; one trace step per step.
   - Unit tests for label matching, expansion, the tsquery builder, classification and each rule operator. ✅ GQ-01, GQ-02 (keyword side rescued), GQ-03 (phone battery out of concept), GQ-05, GQ-06, GQ-07 (Spanish label → concept expansion).

### Acceptance criteria
- All 5 endpoints (stages 1–4 and 6; Stage 5 is deferred to Phase 6) appear in Scalar and return the shared contract with a populated `debugTrace`.
- The integration suite shows the talk's story as passing tests: synonym miss → vector hit; keyword trap → hybrid fix; near miss → ontology flag with reason; Spanish query → chargers via ontology labels.
- Missing ONNX models give a `503` with fix-it guidance, not a stack trace.
- ADRs 0003, 0004, 0007–0011 and 0013 → **Accepted**.

### Open questions
- ❓ Keep `reviews` out of `search_vector`? Revisit after GQ-02 and GQ-03 results.
- ❓ Language for GQ-07 (proposed Spanish).

---

## Phase 3 — Frontend (stages 1–6)

**ADRs:** [0014](0014-web-ui-architecture.md)

1. Scaffold `src/web-ui` (Vite + React + TS strict, Tailwind, shadcn/ui init, Lucide, ESLint, Prettier, Vitest, `.nvmrc`).
2. Re-add `Aspire.Hosting.JavaScript`; `AddViteApp` with `WithReference(searchApi)`; Vite `/api` proxy from the service-discovery environment variable. Spike a dummy SSE endpoint through the proxy to confirm streaming isn't buffered, since Phase 4 depends on it.
3. `npm run gen:api` with `openapi-typescript` → committed `src/api/schema.d.ts`; typed `fetch` client.
4. `usePipelineSearch` hook (same request across stages, AbortController, URL state) + Vitest tests.
5. Layout: `SearchBar` (golden-query presets, device picker), `FilterBar`, `PipelineStepper` (keyboard ←/→), `ResultCard` with `SignalBadges` and `CompatibilityBadge`.
6. `DebugDrawer` renderers: `SqlBlock`, `TsQueryView`, `DistanceTable`, `RrfTable`, `ConceptMatches`/`ExpansionView`/`RuleChecks`, JSON fallback.
7. **Pages & content** (0014)
   - React Router routes `/`, `/talk/:step`, `/demo`, `/glossary`, `/decisions`, `/decisions/:id`.
   - `src/web-ui/content/`: placeholder `speaker.md`; `talk.json` with intro, hook, stages 1–4 and 6, and summary steps; `stages/*.md` explanations; `glossary.json`.
   - Inline `term:` links with hover cards; ADRs rendered from `docs/adr/*.md` via `import.meta.glob`.
   - Vitest content-integrity tests.
   - ❓ Review the talk/demo split with Pete in the running UI before writing full content.
8. Presentation mode (large type, on by default in talk mode); accessibility pass.
9. CI: add `typecheck`, `lint` and `build` for `web-ui`.

### Acceptance criteria
- `aspire run` opens the UI. Choosing GQ-01 and stepping 1 → 6 shows the results changing and the near miss flagged in Stage 6 with its reason.
- Every trace step renders with a purpose-built view (no raw JSON for stages 1–6).
- Readable on a 1280×720 projector in presentation mode; keyboard-only operable.
- Talk mode walks from Home through the intro and stages 1–4 and 6 using only the keyboard. Each stage step shows its explanation and live results; glossary terms show definitions on hover and focus; ADR pages render with working cross-links.
- ADR-0014 → **Accepted** (for stages 1–6).

### Open questions
- ❓ Visual style and branding for the talk (colours, logo), if any.
- ❓ Speaker details for `speaker.md` (name, role, bio, photo, links). Placeholders until supplied.

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
   - `POST /api/search/rag` (JSON results + evidence trace step) and `POST /api/search/rag/answer` (SSE stream, plus JSON mode for tests).
   - Evidence-set builder; prompt files in `assets/prompts/`; `GetStreamingResponseAsync` → `meta`/`delta`/`final`/`done`/`error` events.
   - Validation after generation: citations, `INSUFFICIENT_EVIDENCE` sentinel, incompatible-recommendation heuristic.
   - Cancellation; unbuffered response; trace with prompts, raw output and timings (time to first token, total); unit + structural integration tests.
4. **Stage 8 — Pedagogy** (0017)
   - `POST /api/search/pedagogy` + `/answer` streaming the `answer` section, then the `explanation` section.
   - Audience-aware prompt with fixed markdown headings; heading parser; validator (Decision Compatible, Near miss Incompatible, concept-label heuristic); parsed structure in `final`; per-section timings in the trace; tests.
5. **UI** (0014)
   - `useAnswerStream` (fetch + SSE parser, in parallel with the results request); `SummaryPanel` rendering streamed markdown with `[PROD-…]` chips that scroll to result cards, warning badges and time to first token; audience switcher; `PromptView` trace renderer; loading states for multi-second calls; talk-mode steps, stage explanations and glossary entries for Stages 7–8.

### Acceptance criteria
- GQ-01 in Stage 7 gives a grounded answer citing the compatible charger and warning about the near miss. Stage 8 explains connector + wattage with the near miss as a counter-example.
- Switching audience visibly changes the explanation without changing the facts.
- In Stages 7–8 the results list renders before any LLM text, and the summary's first token appears within ~1.5 s on the presenter laptop (local Ollama). Stage 8's answer and explanation complete in under ~15 s combined.
- Switching stage mid-stream cancels generation (visible in Ollama/the trace).
- With Ollama stopped, Stages 7–8 still show results, the summary panel shows the 503 guidance, and Stages 1–6 are unaffected.
- ADRs 0015–0017 → **Accepted**.

### Open questions
- ❓ Default model (from the bake-off).
- ❓ Default hosted model IDs for learners (OpenAI and Anthropic). Confirm when Phase 4 starts, since model versions move quickly.

---

## Phase 5 — Finish & publish

1. **Dataset growth:** a generator script (in `tools/`, language to be decided) adds distractors to reach ~500 products without disturbing the curated core; golden-query tests still pass.
2. **README (final pass; kept current since Phase 1):** prerequisites (.NET 10, Docker, Node LTS, Aspire CLI, Hugging Face CLI, and either Ollama or an OpenAI/Anthropic API key), model download, `aspire run`, a tour of the 8 stages, how to reset the data volume, troubleshooting.
3. **CI:** OpenAPI → TypeScript drift check; optional manual integration-test workflow.
4. **Talk content & rehearsal:** finalise the talk-mode steps, speaker details and summary; rehearse the full talk end to end in the UI (there are no slides). Rebuild `nomic.jsonl` after dataset growth.
5. **Public ADRs:** write learner-facing ADRs from each ADR's *Teaching notes*; choose their public location; decide what happens to these working ADRs; point the UI's `/decisions` pages at the public versions.
6. **Final review:** code comments read as teaching material; every stage file opens with its technique / strength / failure-mode comment; all ADRs **Accepted** or explicitly superseded.
7. **OpenAI providers (when credits allow)** (0009, 0015): test OpenAI embeddings (`Embeddings:Provider = openai`, `Rebuild: true` → commit `openai.jsonl`) and OpenAI chat; adjust golden-query expectations if needed; document the one-key setup in the README.

### Acceptance criteria
- A fresh clone on a clean machine runs end to end by following the README alone.
- The whole golden-query suite passes at ~500 products.
- Public ADRs are published; the full talk is rehearsed end to end in the UI's talk mode.

### Open questions
- ✅ No slides: the talk lives in the web UI ([ADR-0014](0014-web-ui-architecture.md)).
- ❓ Public ADR location (`docs/decisions/`?) and whether the working ADRs stay, are archived, or are replaced by the public versions before merging to `main`.
- ❓ Generator script language (C# console in `tools/` proposed, to avoid adding Python).

---

## Phase 6 — Optional: Stage 5 BGE-M3 (build last)

**ADRs:** [0012](0012-bge-m3-dense-and-sparse.md)

> **Build this last, whoever is building.** Do not start it until Phases 1–5 are complete. It is the most cuttable stage: dense + sparse fused with RRF overlaps with Stage 4. Its unique value is learned sparse weights and full-sentence cross-language search. With a 4-week build-and-rehearse window, the core story (stages 1–4, 6–8) comes first. If time runs out, mark ADR-0012 **Deferred** and the stepper shows 7 stages.

1. **BGE-M3 verification** (0012) ⏱ ~1 hour
   - Reuse the earlier in-memory BGE-M3 approach. Confirm tokenizer parity (EN/ES/DE), ONNX outputs and a cross-lingual cosine sanity check against this export. Update ADR-0012 only if something differs.
2. **Stage 5 — BGE-M3** (0012)
   - Dense + sparse embedder; `SparseVector` index-base conversion tests; seeder loads or rebuilds `bge-m3.jsonl`; dense and sparse retrieval fused via `IRankFusion`; tokens and sparse weights in trace. ✅ GQ-07.
3. **UI:** `TokenWeights` trace renderer; add the Stage 5 tab to the stepper.
4. **Docs & talk:** README stage tour, a talk-mode step, a stage explanation and glossary entries for Stage 5.

### Acceptance criteria
- GQ-07 (cross-language) ranks the correct chargers in BGE-M3's top 5, while stages 2–4 don't.
- Full golden-query suite still passes; ADR-0012 → **Accepted**.

### Open questions
- ❓ Can the earlier in-memory BGE-M3 code be shared, so Stage 5 reuses its tokenizer and pooling approach?

---

## Traceability

| ADR | Phase(s) |
|---|---|
| 0001 | all (process) |
| 0002 | 1 (+ CI in 3, 5) |
| 0003, 0004 | 2 |
| 0005 | 1 (+ growth in 5) |
| 0006 | 1 (+ backfill in 2) |
| 0007–0011 | 2 |
| 0012 | 6 (optional, build last) |
| 0013 | 1 (vocabulary), 2 (stage) |
| 0014 | 3 (+ AI panels in 4) |
| 0015–0017 | 4 |

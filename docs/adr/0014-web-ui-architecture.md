# ADR-0014: Web UI — the talk, the demo & learning pages

- **Status:** Proposed
- **Date:** 2026-09-13
- **Related:** ADR-0002, ADR-0003, ADR-0005, ADR-0013; roadmap Phase 3 (pages, talk and stages 1–4 and 6), Phase 4 (AI stages), Phase 5 (content and public ADRs)

## Context

The audience needs to see the *same query* produce different results as the presenter steps through the stages, with the mechanics (SQL, distances, RRF maths, concepts, rule checks, prompts) shown next to the results. Scalar is fine for developers but can't tell that story on stage.

**The UI replaces the slides.** Talk content, live demo, terminology and design decisions live in one place, so the talk and the demo can't drift apart, and learners who clone the repo get the whole talk as well as the code.

The frontend must be as readable as the backend: no black-box component libraries, and no heavy state management that learners would need to study first.

## Decision

### Stack and hosting

- **React + Vite + TypeScript (strict)**, in `src/web-ui`.
- **Tailwind CSS + shadcn/ui**: components are copied into `src/components/ui`, so every line is in the repo. **Lucide** icons.
- **React Router** for pages. **`react-markdown` + `remark-gfm`** for content.
- **Hosted by Aspire** via `Aspire.Hosting.JavaScript`:

```csharp
builder.AddViteApp("web-ui", "../web-ui")
    .WithReference(searchApi)
    .WaitFor(searchApi)
    .WithExternalHttpEndpoints();
```

- **No CORS.** The Vite dev server proxies `/api` to the Search API, using the service-discovery URL Aspire injects as an environment variable (`services__searchapi__https__0`), read in `vite.config.ts`. The browser only ever talks to the Vite origin.
- **Package manager:** npm, with `package-lock.json` committed. Node LTS version pinned in `.nvmrc`.

### Pages and routes

| Route | Page | Content |
|---|---|---|
| `/` | **Home** | Speaker details (from `speaker.md`), talk title and abstract, the thesis, the triad (Search → Ontology → Pedagogy), buttons: *Start the talk* and *Explore the demo* |
| `/talk/:step` | **Talk mode** | A linear sequence that replaces slides, driven by ←/→. Intro and summary steps are full-width content. **Stage steps show the live demo** with the stage explanation panel, a preset golden query and preset options (e.g. Stage 6 toggles) |
| `/demo` | **Demo** | The free-exploration screen: search, filters, stepper, results, debug drawer; the explanation panel is collapsible |
| `/glossary` | **Glossary** | Searchable terms and acronyms, grouped by topic |
| `/decisions`, `/decisions/:id` | **Decisions** | The ADR index and each ADR, rendered |

The talk-versus-demo split is agreed in principle and **validated with Pete in the build** before content is finalised.

### Content as markdown, not React code (`src/web-ui/content/`)

| File | Purpose |
|---|---|
| `speaker.md` | Name, role, bio, photo path, links. **Placeholder values until Pete supplies them** |
| `talk.json` + `talk/*.md` | Ordered talk steps: `{ id, kind: intro \| stage \| summary, title, file, stage?, goldenQuery?, options? }`. A JSON manifest instead of front-matter avoids a parser dependency |
| `stages/{stage}.md` | One explanation per stage with fixed headings: *What it is · How it works · What to look for · Strength · Failure mode · Try this · Read the decision* |
| `glossary.json` | `[{ id, term, acronym?, definition, topic, seeAlso?, adr? }]`, e.g. BM25, FTS, tsvector, embedding, cosine distance, HNSW, RRF, dense/sparse, SKOS, RAG, ONNX |

- **Inline terms:** in any markdown content, `[RRF](term:rrf)` renders as an underlined term with a hover card showing its definition and a link to the glossary. A custom `a` renderer in `react-markdown` does this, with no remark plugin. Trace notes from the API may use the same syntax.
- **ADRs are read straight from the repo at build time.** `import.meta.glob` loads `docs/adr/*.md` as raw text (`server.fs.allow` includes the repo root), and links between ADRs are rewritten to `/decisions/:id`. There's no copy to drift and no API endpoint. In Phase 5 the glob points at the public learner ADRs instead.

### API types

- **`openapi-typescript`** generates `src/api/schema.d.ts` from the API's OpenAPI document (`/openapi/v1.json`) with an `npm run gen:api` script. The generated file is committed, so the UI builds without the API running. Regenerate when the contract changes. CI checks for drift from Phase 5. The API's OpenAPI output is verified at the start of Phase 2, before the UI depends on it.
- There is no generated client, just a small typed `fetch` wrapper in `src/api/client.ts`.

### State and data flow

- **One hook, `usePipelineSearch`**, holds `{ request, stage, response, status, error }`. It is used by `/demo` and by talk stage steps.
  - Changing the stage or submitting re-runs `POST /api/search/{stage}` with the **same request**.
  - An `AbortController` cancels in-flight calls when the stage changes quickly.
- **`useAnswerStream`** (Stages 7–8) runs **in parallel** with `usePipelineSearch`, so the results list never waits for the LLM ([ADR-0016](0016-rag-grounding-and-citations.md)).
  - It POSTs the same request to `/api/search/{rag|pedagogy}/answer`.
  - It reads the response with `fetch` and a small, readable SSE parser, because `EventSource` can't POST.
  - It appends `delta` text to the summary and applies `final` citations and warnings.
  - `[PROD-…]` renders as a chip that scrolls to its result card. The same `AbortController` cancels it, and the Vite proxy passes `text/event-stream` through unbuffered.
- **No global state library** (Redux, Zustand) and **no TanStack Query**. Plain React state is enough and easier to read.
- The URL holds demo state (`/demo?stage=hybrid&q=...&gq=GQ-01`), and talk position lives in the route (`/talk/stage-hybrid`), so the presenter can bookmark and the browser back button works.

### Demo layout and components (architecture §5)

```text
Demo / talk stage step
├─ SearchBar            query input · golden-query preset picker (GET /api/demo/queries) · target-device picker (GET /api/demo/devices)
├─ FilterBar            brand · category tree (GET /api/taxonomy) · price · spec chips (collapsible)
├─ PipelineStepper      stage tabs, with keyboard ←/→ · Stage 6 toggles: expand synonyms / apply constraints
├─ StageExplanation     content/stages/{stage}.md with inline glossary terms (always shown in talk mode, collapsible in demo)
├─ ResultsPanel (left)
│   ├─ SummaryPanel     stages 7–8: streamed markdown from /answer · [PROD-…] citation chips · warning badges · time to first token (Phase 4)
│   │                   stage 8 streams its pedagogy sections below the answer
│   └─ ResultCard[]     name · brand · price · key specs · SignalBadges · CompatibilityBadge (+ reasons popover)
└─ DebugDrawer (right)
    └─ TraceStep[]      one collapsible section per trace step, rendered by stage type:
        SqlBlock · TsQueryView · DistanceTable · RrfTable · ConceptMatches · ExpansionView · RuleChecks · PromptView (· TokenWeights if Stage 5 is built)
```

- **Trace renderers** map the known `details` keys to purpose-built views, falling back to pretty-printed JSON ([ADR-0003](0003-search-api-contract-and-debug-trace.md)).
- **Maths as text:** RRF formulas and distances are rendered as monospaced strings from the API. No KaTeX dependency.
- **Presentation mode:** a global toggle that increases font size and hides non-essential controls, for projector readability. It is on by default in talk mode.
- **No product images** (confirmed). Each taxonomy concept names a Lucide icon in the TTL (`ex:icon`), and a result card uses the icon of its first category.
- **Stretch goal (not in scope):** a 2D projection plot of vector space for Stage 3.

### Quality bar

- ESLint (typescript-eslint, react-hooks) and Prettier. `npm run typecheck`, `npm run lint` and `npm run build` must pass.
- Accessibility basics:
  - semantic landmarks and visible focus;
  - badges that use text as well as colour;
  - the stepper as an ARIA tablist;
  - talk mode fully keyboard-operable;
  - hover cards also open on focus.
- **Tests (Vitest):**
  - `usePipelineSearch`; trace-renderer selection.
  - Content integrity: every `talk.json` step file exists, every `goldenQuery` id is in `golden-queries.json`, every `term:` link resolves to a glossary entry, every stage has an explanation file.
  - No end-to-end browser tests; golden queries are covered by the API integration tests.

## Consequences

- One `aspire run` gives the talk, the demo, the glossary and the decisions, and there is nothing separate to keep in sync.
- Rehearsing the talk *is* testing the product, and a broken stage shows up in rehearsal.
- Content writing becomes real roadmap work: talk steps, stage explanations and glossary entries.
- Committed generated types make contract changes visible in diffs.
- The Vite proxy keeps the API free of CORS configuration, but the UI is dev-server-only. A production build/serve isn't in scope.

## Alternatives considered

| Option | Why not (for this repo) |
|---|---|
| Separate slides (PowerPoint/Keynote) + demo | Content duplicated and drifts; context-switching on stage; learners don't get the talk |
| MDX for content | Mixes code into content and adds build tooling; markdown + manifest is enough |
| Serve ADRs via an API endpoint | A backend responsibility unrelated to search; build-time import is simpler |
| Mantine | Rich but opaque; less code for learners to read |
| Plain Tailwind, no component kit | Maximum transparency, but lots of hand-built accessible widgets (tabs, popovers, hover cards) |
| Next.js | SSR isn't needed; more concepts |
| Blazor | Keeps one language, but the architecture specifies React, and the frontend audience is broader |
| TanStack Query | Excellent, but caching and retries hide the request-per-stage behaviour we want visible |
| `EventSource` or SignalR for the summary stream | `EventSource` can't POST a request body; SignalR is heavier than a one-way SSE stream |
| Enable CORS on the API | Works; the proxy avoids teaching a security setting that isn't the topic |

## Teaching notes

- The UI is an *instrument* for the experiment: same input, switchable technique, visible internals.
- Put content in content files and behaviour in code, so each stays easy to change.
- Explaining terms where they appear (inline glossary) is pedagogy applied to the tool itself.
- Generated API types keep frontend and backend honest with each other.

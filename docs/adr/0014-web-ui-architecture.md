# ADR-0014: Web UI architecture

- **Status:** Proposed
- **Date:** 2026-09-13
- **Related:** ADR-0002, ADR-0003, ADR-0013; roadmap Phase 3 (stages 1–6), Phase 4 (AI panels)

## Context

The audience needs to see the *same query* produce different results as the presenter steps through the stages, with the mechanics (SQL, distances, RRF maths, triples, prompts) shown next to the results. Scalar is fine for developers but can't tell that story on stage.

The frontend must be as readable as the backend: no black-box component libraries, and no heavy state management that learners would need to study first.

## Decision

### Stack and hosting

- **React + Vite + TypeScript (strict)**, in `src/web-ui`.
- **Tailwind CSS + shadcn/ui**: components are copied into `src/components/ui`, so every line is in the repo. **Lucide** icons.
- **Hosted by Aspire** via `Aspire.Hosting.JavaScript`:

```csharp
builder.AddViteApp("web-ui", "../web-ui")
    .WithReference(searchApi)
    .WaitFor(searchApi)
    .WithExternalHttpEndpoints();
```

- **No CORS.** The Vite dev server proxies `/api` to the Search API, using the service-discovery URL Aspire injects as an environment variable (`services__searchapi__https__0`), read in `vite.config.ts`. The browser only ever talks to the Vite origin.
- **Package manager:** npm, with `package-lock.json` committed. Node LTS version pinned in `.nvmrc`.

### API types

- **`openapi-typescript`** generates `src/api/schema.d.ts` from the API's OpenAPI document (`/openapi/v1.json`) with an `npm run gen:api` script. The generated file is committed, so the UI builds without the API running. Regenerate when the contract changes. CI checks for drift from Phase 5.
- There is no generated client, just a small typed `fetch` wrapper in `src/api/client.ts`.

### State and data flow

- **One hook, `usePipelineSearch`**, holds `{ request, stage, response, status, error }`.
  - Changing the stage or submitting re-runs `POST /api/search/{stage}` with the **same request**.
  - An `AbortController` cancels in-flight calls when the stage changes quickly.
- **No global state library** (Redux, Zustand) and **no TanStack Query**. Plain React state is enough for one screen and easier to read.
- The URL holds `?stage=hybrid&q=...&gq=GQ-01`, so a presenter can bookmark each demo moment and the browser back button steps through stages.

### Layout and components (architecture §5)

```text
App
├─ SearchBar            query input · golden-query preset picker (GET /api/demo/queries) · target-device picker (GET /api/demo/devices)
├─ FilterBar            brand · category tree (GET /api/taxonomy) · price · spec chips (collapsible)
├─ PipelineStepper      8 stage tabs, with keyboard ←/→ for presenting · Stage 6 toggles: expand synonyms / apply constraints
├─ ResultsPanel (left)
│   ├─ AnswerCard       stage 7 answer with citation chips        (Phase 4)
│   ├─ ExplanationCard  stage 8 pedagogy output                    (Phase 4)
│   └─ ResultCard[]     name · brand · price · key specs · SignalBadges · CompatibilityBadge (+ reasons popover)
└─ DebugDrawer (right)
    └─ TraceStep[]      one collapsible section per trace step, rendered by stage type:
        SqlBlock · TsQueryView · DistanceTable · RrfTable · TokenWeights · ConceptMatches · ExpansionView · RuleChecks · PromptView
```

- **Trace renderers** map the known `details` keys to purpose-built views, falling back to pretty-printed JSON ([ADR-0003](0003-search-api-contract-and-debug-trace.md)).
- **Maths as text:** RRF formulas and distances are rendered as monospaced strings from the API. No KaTeX dependency.
- **Presentation mode:** a toggle that increases font size and hides filters, for projector readability.
- **No product images** (confirmed). Each taxonomy concept names a Lucide icon in the TTL (`ex:icon`), and a result card uses the icon of its first category.
- **Stretch goal (not in scope):** a 2D projection plot of vector space for Stage 3.

### Quality bar

- ESLint (typescript-eslint, react-hooks) and Prettier. `npm run typecheck`, `npm run lint` and `npm run build` must pass.
- Accessibility basics: semantic landmarks, visible focus, badges that use text as well as colour, stepper as an ARIA tablist.
- Tests: Vitest for `usePipelineSearch` and trace-renderer selection. No end-to-end browser tests (golden queries are covered by the API integration tests).

## Consequences

- One `aspire run` shows the UI, API and dashboard together.
- The committed generated types make contract changes visible in diffs.
- The Vite proxy keeps the API free of CORS configuration, but the UI is dev-server-only. A production build/serve isn't in scope, and the ADR says so.

## Alternatives considered

| Option | Why not (for this repo) |
|---|---|
| Mantine | Rich but opaque; less code for learners to read |
| Plain Tailwind, no component kit | Maximum transparency, but lots of hand-built accessible widgets (tabs, popovers) |
| Next.js | SSR and routing aren't needed; more concepts |
| Blazor | Keeps one language, but the architecture specifies React, and the frontend audience is broader |
| TanStack Query | Excellent, but caching and retries hide the request-per-stage behaviour we want visible |
| Enable CORS on the API | Works; the proxy avoids teaching a security setting that isn't the topic |

## Teaching notes

- The UI is an *instrument* for the experiment: same input, switchable technique, visible internals.
- Generated API types keep frontend and backend honest with each other.

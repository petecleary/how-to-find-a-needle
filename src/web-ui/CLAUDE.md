# CLAUDE.md — web-ui (React / TypeScript)

Repo-wide rules (teaching principles, commenting standard, vocabulary) are in the [root CLAUDE.md](../../CLAUDE.md). The decision is [ADR-0014](../../docs/adr/0014-web-ui-architecture.md).

> The app is scaffolded in roadmap Phase 3. Until then this folder only holds this file; scaffold Vite into the existing folder rather than deleting it.

**The UI is the talk.** It replaces slides: home, talk mode, demo, glossary and ADR pages. It is an instrument for the experiment: same input, switchable technique, visible internals.

## Stack and constraints

- React + Vite + **TypeScript strict**; Tailwind CSS; shadcn/ui **copied into** `src/components/ui`; Lucide icons; React Router; `react-markdown` + `remark-gfm`.
- npm with `package-lock.json` committed; Node LTS pinned in `.nvmrc`.
- **Not used (see ADR-0014 alternatives):** Redux, Zustand, TanStack Query, MDX, KaTeX, `EventSource`, SignalR, CORS, Next.js.
- No CORS: the Vite dev server proxies `/api` using the Aspire-injected service URL. The browser only talks to the Vite origin.

## Layout

| Path | Holds |
|---|---|
| `content/` | `speaker.md`, `talk.json` + `talk/*.md`, `stages/{stage}.md`, `glossary.json` |
| `src/api/schema.d.ts` | **Generated** by `npm run gen:api`. Never hand-edit; commit it |
| `src/api/client.ts` | Small typed `fetch` wrapper |
| `src/api/answerEvents.ts` | Hand-typed SSE event shapes (OpenAPI can't describe them) |
| `src/hooks/` | `usePipelineSearch`, `useAnswerStream` |
| `src/components/` | App components and trace renderers |
| `src/components/ui/` | shadcn/ui primitives |
| `src/pages/` | Route-level pages |
| `src/assets/fonts/` | Self-hosted `.woff2` files and each family's `OFL.txt` |
| `assets/images/` | Pi & Mash logos: `logo_{green,purple,orange}.png` (filled, light theme) and `logo_*_bo(a)rder.png` (outline, dark theme) |

## Visual design

Decided in [ADR-0014 § Visual design](../../docs/adr/0014-web-ui-architecture.md#visual-design); pictures in [docs/design](../../docs/design/README.md). Match them.

- **Triad colours:** purple = Search (Stages 1–4), green = Ontology (Stage 5), orange = Pedagogy (Stages 6–7). Get a stage's colour from `stageGroup()`, never by hard-coding.
- **Status colours are not brand colours.** Incompatible is red, never orange. Every badge has an icon and text.
- **Fonts:** Dosis (`font-brand`) for the logo wordmark and the talk title **only**; Atkinson Hyperlegible for all other text; JetBrains Mono for SQL, formulas and trace values.
- Colours come from theme tokens (CSS variables); no raw hex in components. Both light and dark themes must work.
- Green and orange fills take dark text; white text only on purple.
- Shapes: circles for numbers, pills for controls, 18px card radius, 2px borders, flat colour, no gradients.
- **Stage tabs:** How it works · Results · Answer · Under the hood. Each tab gets the full width; don't put the trace beside the results.

## Naming conventions

| Kind | Pattern | Example |
|---|---|---|
| Component file and export | `PascalCase.tsx`, one component per file, **named export** | `ResultRow.tsx` → `export function ResultRow` |
| Props type | `{Component}Props` | `ResultCardProps` |
| Page | `{Name}Page.tsx` | `DemoPage.tsx` |
| Hook | `useX.ts` | `usePipelineSearch.ts` |
| Utility | `camelCase.ts` | `parseSseEvents.ts` |
| Trace renderer | Named after what it shows | `SqlBlock`, `RrfTable`, `RuleChecks` |
| Types | PascalCase, no `I` prefix | `PipelineStage` |
| Content files | kebab-case, stage files use the stage slug | `stages/ontology.md` |
| Tests | Beside the code | `usePipelineSearch.test.ts` |

- Booleans read as questions: `isStreaming`, `hasWarnings`. Event handlers: `onX` props, `handleX` functions.
- Stage slugs, compatibility statuses and golden-query IDs match the API exactly.

## TypeScript and React style

- Function components only. Plain `useState` / `useReducer` / context where needed.
- API types come from `schema.d.ts`. Don't hand-duplicate DTOs. No `any`; use `unknown` and narrow.
- Every request gets an `AbortController`; switching stage cancels the previous search and stream.
- `usePipelineSearch` and `useAnswerStream` run **in parallel**. Results never wait for the LLM.
- Demo state lives in the URL (`/demo?stage=hybrid&q=…&gq=GQ-01`); talk position lives in the route (`/talk/:step`).
- Tailwind classes in markup; no CSS-in-JS. Put conditional class logic in a small `cn()` helper.
- Render maths and formulas as monospaced text from the API.
- Trace renderers map known `details` keys to purpose-built views, falling back to pretty-printed JSON.

## Content vs code

- Talk text, stage explanations, glossary entries and speaker details live in `content/`, **never hard-coded in components**.
- Stage explanation headings are fixed: *What it is · How it works · What to look for · Strength · Failure mode · Try this · Read the decision*.
- Inline glossary terms use `[RRF](term:rrf)`; every `term:` link must resolve to a `glossary.json` entry.
- ADRs are imported from `docs/adr/*.md` with `import.meta.glob`; don't copy them into the UI.
- Content is British English, as in the root CLAUDE.md.

## Comments

The root commenting standard applies. In the UI, comments explain **what the view reveals about the technique** and why the UI is built the way it is:

```ts
// Hand-written SSE parser: EventSource can only GET, and /answer needs the same POST body as the search.
```

Trace renderers open with one line: `// RrfTable — shows each item's per-list ranks and the RRF sum, so the audience can check the maths.`

## Accessibility and presentation

- Semantic landmarks and visible focus. Badges use text as well as colour.
- The stepper and the stage tabs are ARIA tablists. Talk mode is fully keyboard-operable: ←/→ walk a stage step's tabs, then the next step; H / R / A / U jump to a tab. Hover cards also open on focus.
- Text contrast ≥ 4.5:1 (3:1 for large text) in both themes.
- Readable on a 1280×720 projector in presentation mode (on by default in talk mode).

## Checks

`npm run typecheck`, `npm run lint` (typescript-eslint, react-hooks) and `npm run build` must pass, plus Prettier formatting. Run `npm run gen:api` whenever the API contract changes, and commit the result.

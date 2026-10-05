# web-ui — the talk and the demo

A React + Vite + TypeScript app that **is** the talk: a home page, the presenter's slide deck, the live demo, a glossary and the decision records. It is an instrument for one experiment: **same input, switchable technique, visible internals.**

Start it with `aspire run` from the repository root. Aspire installs the npm packages, starts the Vite dev server and tells it where the API is; Vite proxies `/api` to the API, so the browser only ever talks to one origin and no CORS is needed.

## Pages

| Route                          | Page                                                                                                                                                            |
| ------------------------------ | --------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `/`                            | Home: the talk's abstract, thesis and the triad (Search → Ontology → Pedagogy)                                                                                  |
| `/slides/:slide`               | The presenter's deck. ← / → (or a clicker) move one slide; **D** opens the demo in a second window that follows the deck                                        |
| `/demo`                        | The stage screen: search bar, golden-query picker, the seven-stage stepper and the tabs. Every input lives in the URL, so a moment can be bookmarked and shared |
| `/glossary`                    | Every term and acronym used in the talk                                                                                                                         |
| `/decisions`, `/decisions/:id` | The decision records from `docs/decisions/`, rendered                                                                                                           |

## The stage screen

Each stage has the same tabs, so you can compare stages without the layout moving:

- **How it works** — the stage explanation, from `content/stages/{stage}.md`.
- **Results** — the candidates, with concept and compatibility badges on Stage 5.
- **Answer** — Stages 6–7 only: the streamed answer, its citations and the evidence the model was given. Stages 6–7 also have a **Model** picker, so you can compare models on the same evidence.
- **Under the hood** — the API's trace: SQL, tsqueries, distances, RRF arithmetic, concepts, rule checks and prompts, each in a view built for it (`src/components/trace/`).
- **Going further** — where the technique goes next, from `content/going-further/{stage}.md`.

Keys: **H R A U G** jump to a tab.

## How the code is organised

| Path              | Holds                                                                                                                                                                           |
| ----------------- | ------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| `content/`        | The words: `home.md`, `slides.json` + `slides/*.md`, `stages/*.md`, `going-further/*.md`, `glossary.json`, `speaker.md`. Text never lives in components                         |
| `src/api/`        | `schema.d.ts` (generated from the API's OpenAPI document — never edit it), `client.ts` (one small `fetch` function per endpoint), `answerEvents.ts` (the streamed event shapes) |
| `src/hooks/`      | `usePipelineSearch` (results) and `useAnswerStream` (the LLM's text) run **in parallel**: results never wait for the model                                                      |
| `src/lib/`        | Plain TypeScript helpers with tests beside them: URL state (`searchState.ts`), slides, the SSE parser, stage colours                                                            |
| `src/components/` | The screens and the trace renderers; `ui/` holds the [shadcn/ui](https://ui.shadcn.com/) primitives, copied in so every line is readable                                        |
| `src/pages/`      | One component per route                                                                                                                                                         |

There is no state library: React state and context, with the URL as the source of truth for the demo ([ADR-0014](../../docs/decisions/0014-web-ui-architecture.md)).

## Scripts

| Command                                                     | What it does                                                                                                                                       |
| ----------------------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------- |
| `npm run dev`                                               | The Vite dev server (normally started for you by `aspire run`)                                                                                     |
| `npm test`                                                  | Vitest: hooks, parsers, trace-renderer choice and **content integrity** (every slide file exists, every glossary link resolves)                    |
| `npm run typecheck`, `npm run lint`, `npm run format:check` | TypeScript strict, ESLint and Prettier: CI runs all three                                                                                          |
| `npm run build`                                             | Typecheck, then a production bundle                                                                                                                |
| `npm run gen:api`                                           | Regenerates `src/api/schema.d.ts` from the running API (start `aspire run` first). Run it whenever the API contract changes, and commit the result |

## Changing the content

- **A slide**: edit `content/slides/{id}.md`. Keep to a short title and at most five short bullets (a test enforces it); add or reorder slides in `content/slides.json`. A slide's `demo` names the stage, golden query, options and tab the demo window shows.
- **A stage explanation**: edit `content/stages/{stage}.md`, keeping its fixed headings.
- **A glossary term**: add it to `content/glossary.json`, then link it from any markdown as `[RRF](term:rrf)`. `npm test` fails if a link points at a term that doesn't exist.

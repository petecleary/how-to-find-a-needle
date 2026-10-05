# ADR-0014: Web UI — the talk, the demo & learning pages

- **Status:** Accepted
- **Date:** 2026-09-13 (amended 2026-09-14: visual design, stage tabs, filter placement and the Answer tab, agreed with Pete from the design canvas; accepted 2026-09-19 at Phase 5 sign-off; amended 2026-10-01: a slide deck at `/slides` with a demo window that follows it, after rehearsal feedback)
- **Related:** ADR-0002, ADR-0003, ADR-0005, ADR-0013, ADR-0016, ADR-0017, ADR-0018; [design reference](../design/README.md); roadmap Phase 3 (pages, talk and stages 1–5), Phase 4 (AI stages), Phase 5 (content, going-further step and public ADRs)

## Context

The audience needs to see the *same query* produce different results as the presenter steps through the stages, with the mechanics (SQL, distances, RRF maths, concepts, rule checks, prompts) shown next to the results. Scalar is fine for developers but can't tell that story on stage.

**The UI replaces the slides.** Talk content, live demo, terminology and design decisions live in one place, so the talk and the demo can't drift apart, and learners who clone the repo get the whole talk as well as the code.

**Amended 2026-10-01: the deck lives in the UI too.** A rehearsal audience found the talk steps too dense to follow: each stage step opens on the live stage screen, so the audience meets tabs, filters and results before the concept has been explained. The fix is a **slide deck inside the UI** (`/slides`): simple concept slides, several per stage for the stages with new vocabulary, with the demo in a second window that follows the current slide. The slides are still markdown in `content/`, pointing at the same golden queries, so the original reason for rejecting separate slides (drift) doesn't apply. ~~`/talk` stays as the self-guided path for learners.~~

**Amended 2026-10-05: talk mode removed.** Pete removed *Start the talk* from Home and *Talk* from the header; the deck and the demo cover the live talk and learners reading along afterwards. `/talk`, `TalkPage`, `content/talk.json` + `talk/*.md` and their tests are deleted. The stage-seed logic the deck shares moved to `lib/stageSeed.ts`; the deck's controls and keys are now `SlideControls` and `useSlideKeys`. Lines below that describe talk mode are struck through.

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
| `/` | **Home** | Speaker details (from `speaker.md`), talk title and abstract, the thesis, the triad (Search → Ontology → Pedagogy), buttons: *Present the slides* and *Explore the demo* |
| ~~`/talk/:step/:tab?`~~ | ~~**Talk mode**~~ | **Removed 2026-10-05.** ~~A linear sequence that replaces slides, driven by ←/→, **one position per step**. Intro and summary steps are full-width content and can sit anywhere in the order. **Stage steps show the live stage screen** with a preset golden query and preset options (e.g. Stage 5 toggles), landing on the step's `tab`; the letter keys move between tabs within the step~~ |
| `/slides/:slide?` | **Slides** | The presenter's deck: one full-screen concept slide per position, driven by ←/→. **D** opens (or focuses) the demo window for the current slide. No app header, so the slide fills the screen |
| `/demo` | **Demo** | The free-exploration screen: the same stage screen, with a collapsible filter sidebar and every tab available. Opened from the deck (`?follow=1`), it **follows the slides**: each slide that names a demo loads its stage, golden query and options |
| `/glossary` | **Glossary** | Searchable terms and acronyms, grouped by topic |
| `/decisions`, `/decisions/:id` | **Decisions** | The ADR index and each ADR, rendered |

The talk-versus-demo split is agreed in principle and **validated with Pete in the build** before content is finalised.

### Content as markdown, not React code (`src/web-ui/content/`)

| File | Purpose |
|---|---|
| `speaker.md` | Name, title, bio, email, LinkedIn URL, photo path and LinkedIn QR code image path. **Placeholder values until Pete supplies them** |
| ~~`talk.json` + `talk/*.md`~~ | **Removed 2026-10-05**; the deck's `slides.json` + `slides/*.md` replace them |
| `stages/{stage}.md` | One explanation per stage with fixed headings: *What it is · How it works · What to look for · Strength · Failure mode · Try this · Read the decision* |
| `going-further/{stage}.md` | Free-form: where that stage's technique goes next. No file for `structured`, and a missing file is what makes the tab unavailable ([ADR-0018](0018-scope-and-going-further.md)) |
| `glossary.json` | `[{ id, term, acronym?, definition, topic, seeAlso?, adr? }]`, e.g. BM25, FTS, tsvector, embedding, cosine distance, HNSW, RRF, dense/sparse, SKOS, RAG, ONNX, plus the going-further terms (chunking, re-ranking, cross-encoder, learned sparse, OWL, SHACL, knowledge graph) |

- **A "Going further" slide** follows the last stage and comes before the summary, with no live demo: one table of the topics that sit *around* the pipeline rather than inside one stage of it — before-retrieval work, telemetry, A/B testing, index freshness, personalisation and permissions. Each stage's own topics live in that stage's going-further tab ([ADR-0018](0018-scope-and-going-further.md)).
- **Inline terms:** in any markdown content, `[RRF](term:rrf)` renders as an underlined term with a hover card showing its definition and a link to the glossary. A custom `a` renderer in `react-markdown` does this, with no remark plugin. Trace notes from the API may use the same syntax.
- **ADRs are read straight from the repo at build time.** `import.meta.glob` loads `docs/adr/*.md` as raw text (`server.fs.allow` includes the repo root), and links between ADRs are rewritten to `/decisions/:id`. There's no copy to drift and no API endpoint. Since Phase 5 the glob reads the public learner ADRs in `docs/decisions/`.

### Slides mode: a deck and a demo that follows it

- **Content.** `content/slides.json` lists the slides in order: `{ id, section, title, file, demo? }`. `section` is a stage slug or `intro`, `needle`, `going-further`, `summary`; it gives the slide its triad colour and its "Stage n of 7" label. A stage slide's heading calls out its stage and triad group ("Stage 1 Search: Structured"), with the slide's `title` beneath it; the stage name is dropped where it repeats the group ("Stage 5 Ontology"). `demo` takes the same fields as a talk step (`stage`, `goldenQuery`, `options`, `tab`), so a slide and a talk step seed the stage screen through one function. Each slide's text is `content/slides/{id}.md`.
- **Readable from the back row.** A slide has a short title and at most five bullets of about twelve words, plus an optional formula line. A content test enforces the limits; the detail stays in the stage explanations and the ADRs.
- **Two windows, one channel.** The deck posts `{ type: 'slide', slideId, demo }` on a `BroadcastChannel` (`needle-presenter`) at every slide change. A demo window that is following replaces its URL state with the slide's demo, so the browser's back button still steps back through what was shown, and a badge names the slide it is following. A slide without `demo` changes only the badge. Changes the presenter makes in the demo last until the next slide with a demo. Without `BroadcastChannel`, the deck and demo still work; the demo just doesn't follow.
- **Presentation mode** is on by default on `/slides` as well as `/talk`.

### API types

- **`openapi-typescript`** generates `src/api/schema.d.ts` from the API's OpenAPI document (`/openapi/v1.json`) with an `npm run gen:api` script. The generated file is committed, so the UI builds without the API running. Regenerate when the contract changes. CI checks for drift from Phase 5. The API's OpenAPI output is verified at the start of Phase 2, before the UI depends on it.
- There is no generated client, just a small typed `fetch` wrapper in `src/api/client.ts`.

### State and data flow

- **One hook, `usePipelineSearch`**, holds `{ request, stage, response, status, error }`. It is used by `/demo` and by talk stage steps.
  - Changing the stage or submitting re-runs `POST /api/search/{stage}` with the **same request**.
  - An `AbortController` cancels in-flight calls when the stage changes quickly.
  - It asks for **`pageSize: 50`**, the API's maximum and the default `candidateDepth`. For ranked stages it then reads any further pages, because hybrid fusion can retrieve more than 50 (Phase 5: GQ-08's flagged chargers rank 52nd–67th at 300 products). Stage 1 keeps its first page, since its total counts the whole filtered catalog. Switching tabs never refetches.
  - Why: Stage 5 orders flagged items *after* the out-of-concept ones and keeps them all. At the default page size of 10, GQ-03's three incompatible chargers aren't on page 1, so the near-miss moment would be invisible. The endpoint still pages exactly once ("retrieve deep, page late"); the UI only groups and collapses what it received.
- **`useAnswerStream`** (Stages 6–7) runs **in parallel** with `usePipelineSearch`, so the results list never waits for the LLM ([ADR-0016](0016-rag-grounding-and-citations.md)).
  - It POSTs the same request to `/api/search/{rag|pedagogy}/answer`.
  - It reads the response with `fetch` and a small, readable SSE parser, because `EventSource` can't POST.
  - It appends `delta` text to the Answer tab and applies `final` citations and warnings.
  - `[PROD-…]` renders as a chip that jumps to that product in the Answer tab's evidence set. The same `AbortController` cancels it, and the Vite proxy passes `text/event-stream` through unbuffered.
- **No global state library** (Redux, Zustand) and **no TanStack Query**. Plain React state is enough and easier to read.
- The URL holds demo state, including the tab, audience and toggles (`/demo?stage=pedagogy&tab=answer&q=...&gq=GQ-03&audience=novice`), and the slide position lives in the route (`/slides/hybrid-rrf`), so the presenter can bookmark and the browser back button works.

### Stage screen: layout and components (architecture §5)

**Not everything is on screen at once.** Each stage splits into five tabs, so the presenter can jump to the tab a question needs. Pictures: [docs/design](../design/README.md).

| Tab | Shows | Key |
|---|---|---|
| **How it works** | The stage explanation (`content/stages/{stage}.md`) with inline glossary hover cards | H |
| **Results** | The candidates, with signal, concept and compatibility badges | R |
| **Answer** | Stages 6–7 only (disabled, with a "Stages 6–7" hint, before that): the answer, the streamed explanation and the evidence set | A |
| **Under the hood** | The trace: one chip per trace step as a flow, and the selected step's renderer | U |
| **Going further** | Stages 2–7 (disabled, with a "Not for Stage 1" hint, on Stage 1): `content/going-further/{stage}.md` — where this technique goes next, in prose and glossary links ([ADR-0018](0018-scope-and-going-further.md)) | G |

An unavailable tab stays visible and disabled with its reason beside it, rather than disappearing: a tab that comes and goes as the stage changes moves every other tab under the presenter's hand.

```text
Demo / talk stage step
├─ AppHeader            logo · "How to Find a Needle" · talk position (talk) or page nav (demo) · PresentationToggle · ThemeToggle
├─ FilterPanel          brand picker (GET /api/brands) · price · category tree (GET /api/taxonomy) · spec vocabularies with synonyms as hints (GET /api/vocabularies)
│                       demo: collapsible sidebar · talk: drawer opened from the Filters button
├─ SearchBar            golden-query picker (GET /api/demo/queries) · query input · target-device picker (GET /api/demo/devices)
│                       · Filters button with the active-filter count
├─ PipelineStepper      seven stages in three triad groups (Search 1–4 · Ontology 5 · Pedagogy 6–7); ARIA tablist, ←/→ in the demo
└─ StageTabs            How it works · Results · Answer · Under the hood; ARIA tablist
    ├─ StageOptions     on the tab row, next to what they change: Stage 5 expand synonyms / apply constraints
    │                   · audience picker novice / enthusiast / expert (only Stage 7 reads it; its trace says so)
    │                   · Stage 7 apply pedagogy (off = baseline prompt, same facts and audience)
    ├─ HowItWorksTab    StageExplanation with inline glossary terms
    ├─ ResultsTab       ResultRow[]: name · brand · price · SignalBadges · CompatibilityBadge · ConceptBadge
    │                   Stage 5 groups: in concept · out of concept (collapsed, "kept, moved down") · a Flagged column of
    │                   FlaggedCard (every check, failed and passed, with has / needs values, and the rank before rules)
    ├─ AnswerTab        Stages 6–7 (Phase 4): AnswerPanel (streamed answer, [PROD-…] chips, citation validation, time to first token)
    │                   · ExplanationPanel (Stage 7's five headings, or the baseline) · EvidenceSet (exactly what the model was given)
    └─ UnderTheHoodTab  TraceFlow (step chips coloured by each step's stage) · the selected step, rendered by stage type:
                        SqlBlock · TsQueryView · EmbeddingView · DistanceTable · RrfTable · ConceptMatches · ExpansionView
                        · ClassificationTable · RuleChecks · PromptView · JSON fallback
```

- **Keyboard.** In slides mode ←/→ (and PageUp/PageDown, for clickers) move one slide and **D** opens the demo. H, R, A, U and G jump to a tab on the stage screen. In the demo, ←/→ switch stages from the focused stepper (standard ARIA tablist behaviour). ~~Talk mode's one-step-per-arrow rule~~ removed with talk mode.
- **The stepper** changes the stage in place, keeping the query and filters. ~~In talk mode it moved to that stage's step.~~
- **Honest labels on flagged items.** A flagged card shows "#2 before rules", from `signals.fusedRank`: the rank from Stage 5's own fusion, before the rules moved the item. It is not the standalone Hybrid stage's rank (the design mock-up's "was #2 in Hybrid" wording is replaced for that reason).

- **Trace renderers** map the known `details` keys to purpose-built views, falling back to pretty-printed JSON ([ADR-0003](0003-search-api-contract-and-debug-trace.md)).
- **Filters come from the ontology.** The category tree comes from `GET /api/taxonomy`, and each spec filter from `GET /api/vocabularies`: its label, its values (with synonyms as hints) and the spec key to send ([ADR-0013](0013-domain-ontology-and-compatibility.md)). No category or spec value is hard-coded in the UI, so an edit to the TTL appears after re-running the AppHost and refreshing the page.
- **Maths as text:** RRF formulas and distances are rendered as monospaced strings from the API. No KaTeX dependency.
- **Presentation mode:** a global toggle that increases font size and hides non-essential controls, for projector readability. It is on by default on the slides.
- **No product images** (confirmed). Each taxonomy concept names a Lucide icon in the TTL (`ex:icon`), and a result row uses the icon of its first category.
- **Stretch goal (not in scope):** a 2D projection plot of vector space for Stage 3.

### Visual design

The UI is branded **Pi & Mash** (Pete's company). Agreed on 2026-09-14 from a design canvas built with real GQ-03 output; the screens are committed in [docs/design](../design/README.md).

- **Brand colours = the triad.** The three logo colours mark which part of the argument a stage belongs to, in the stepper, tabs, trace chips and stage labels:

  | Triad group | Stages | Colour | Why here |
  |---|---|---|---|
  | Search | 1–4 | Purple `#4f46e5` (dark theme `#6d66f0`) | *What is relevant?* |
  | Ontology | 5 | Green `#10b981` | *How is it related and constrained?* Also the primary-action colour |
  | Pedagogy | 6–7 | Orange `#ff7a59` | *How should I explain it?* Stage 6 decides *what to say* (a grounded answer), Stage 7 *how to say it*. RAG adds no retrieval of its own, so it doesn't belong with Search |

- **Status colours are not brand colours.** Compatible uses green; **Incompatible uses red, never orange**, so a warning never reads as "Pedagogy"; Unknown uses amber; Not evaluated is neutral. Every badge has an icon **and** text.
- **Contrast.** Green and orange fills take dark text; white text is used only on purple. Coloured *text* uses a darker "ink" shade of each colour in the light theme and a lighter one in the dark theme.
- **Tokens.** Colours are CSS variables: the light theme on `:root`, the dark theme on `.dark` (shadcn's convention), mapped into Tailwind. Each brand colour has a fill, an ink and a tint. Neutrals are slightly green-toned.
- **Light and dark themes both ship.** The default follows the system setting; the header toggle overrides it and is remembered in `localStorage`. The filled logo is used on light, the outline logo on dark (`assets/images/logo_*`).
- **Type.**
  - **Dosis** (the logo's typeface) for the logo wordmark and the talk title **only**.
  - **Atkinson Hyperlegible** for everything else. It was designed for legibility: l / I / 1 and 0 / O are distinct, which matters for product IDs on a projector.
  - **JetBrains Mono** for SQL, formulas and trace values.
  - All three use the **SIL Open Font License 1.1**. They are **self-hosted**: `.woff2` files and each family's `OFL.txt` are committed under `src/assets/fonts/`, loaded with `@font-face`, so the talk works with no internet at the venue.
- **Shape language from the logo.** Circles for stage and step numbers, pills for buttons, toggles and chips, 18px card radius, 2px borders (the outline logo's weight), flat colour with almost no shadows (hover cards and drawers only). No gradients.

### Quality bar

- ESLint (typescript-eslint, react-hooks) and Prettier. `npm run typecheck`, `npm run lint` and `npm run build` must pass.
- Accessibility basics:
  - semantic landmarks and visible focus;
  - badges that use text as well as colour;
  - the stepper and the stage tabs as ARIA tablists;
  - the slide deck fully keyboard-operable;
  - hover cards also open on focus;
  - text contrast of at least 4.5:1 in both themes (3:1 for large text).
- **Tests (Vitest):**
  - `usePipelineSearch`; trace-renderer selection; talk navigation (→ walks a step's `tabs`, then the next step); Stage 5 result grouping.
  - Content integrity: every `slides.json` slide file exists, every `goldenQuery` id is in `golden-queries.json`, every `term:` link resolves to a glossary entry, every stage has an explanation file.
  - No end-to-end browser tests; golden queries are covered by the API integration tests.
  - Hook tests render with `@testing-library/react` (`renderHook`) in a `jsdom` environment, chosen per test file with `// @vitest-environment jsdom`. Everything else runs in Node. Both are dev dependencies only (added in Phase 3 step 4).

## Consequences

- One `aspire run` gives the talk, the demo, the glossary and the decisions, and there is nothing separate to keep in sync.
- Rehearsing the talk *is* testing the product, and a broken stage shows up in rehearsal.
- Content writing becomes real roadmap work: talk steps, stage explanations and glossary entries.
- Committed generated types make contract changes visible in diffs.
- The Vite proxy keeps the API free of CORS configuration, but the UI is dev-server-only. A production build/serve isn't in scope.
- Tabs mean a stage takes several keypresses in the talk, but each view can use large type, and the presenter controls what the audience looks at.
- Self-hosted fonts add about a megabyte of committed files and their licence texts; in return the talk has no network dependency.

## Alternatives considered

| Option | Why not (for this repo) |
|---|---|
| Separate slides (PowerPoint/Keynote) + demo | Content duplicated and drifts; learners don't get the talk. Rehearsal showed the audience *does* need simple concept slides, so the deck is built into the UI instead (`/slides`) |
| Slides and demo in one window, toggled with a key | One screen, but the presenter loses the slide while demoing, and a second screen can't show both |
| `localStorage` `storage` events to sync the windows | Works, but turns a message into shared state that outlives the talk; `BroadcastChannel` says what it means and needs no cleanup |
| Sync through the API (SignalR or polling) | A server round trip for something two tabs of one browser can say to each other |
| MDX for content | Mixes code into content and adds build tooling; markdown + manifest is enough |
| Serve ADRs via an API endpoint | A backend responsibility unrelated to search; build-time import is simpler |
| Mantine | Rich but opaque; less code for learners to read |
| Plain Tailwind, no component kit | Maximum transparency, but lots of hand-built accessible widgets (tabs, popovers, hover cards) |
| Next.js | SSR isn't needed; more concepts |
| Blazor | Keeps one language, but the architecture specifies React, and the frontend audience is broader |
| TanStack Query | Excellent, but caching and retries hide the request-per-stage behaviour we want visible |
| `EventSource` or SignalR for the summary stream | `EventSource` can't POST a request body; SignalR is heavier than a one-way SSE stream |
| Enable CORS on the API | Works; the proxy avoids teaching a security setting that isn't the topic |
| Everything on one screen (explanation, results and trace side by side) | Tried in the design canvas: nothing is hidden, but at 1280×720 the trace text falls below back-row size |
| Results with the trace in an overlay on demand | Readable, but still competes for one screen; tabs give every section the full width |
| Dosis for all text | It's the brand face, but narrow and light at small sizes; kept for the logo and title |
| Google Fonts `<link>` | Simplest, but the talk would break without venue internet |
| `@fontsource` npm packages | Self-hosted too, but a package per font for three files and a CSS rule |
| A fourth colour for the AI stages | Breaks the one-colour-per-triad-question mapping and the link to the three logos |
| Orange for Incompatible | Reads as "Pedagogy"; status colours stay separate from brand colours |

## Teaching notes

- The UI is an *instrument* for the experiment: same input, switchable technique, visible internals.
- Put content in content files and behaviour in code, so each stays easy to change.
- Explaining terms where they appear (inline glossary) is pedagogy applied to the tool itself.
- Generated API types keep frontend and backend honest with each other.
- Colour carries the argument: purple steps nested inside a green stage *show* that Stage 5 reuses Stage 4, and green and purple inside orange show that RAG reuses Stage 5.
- The tabs mirror the talk's rhythm for every stage: what it is → what changed → why.
- Rehearsal changed the rhythm to **concept slide, then demo**: an audience follows a live screen far better once it knows what to look for. Several slides for Stages 3, 4, 5 and 7; one each for Stages 1, 2 and 6, because the first two techniques need no new vocabulary, and by Stage 6 RAG adds only one step to retrieval the audience already knows.
- The design canvas caught a real problem before any UI code existed: at `pageSize` 10, Stage 5's flagged items weren't on page 1. Designing with real API output, not placeholder data, is what found it.

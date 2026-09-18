# ADR-0014: Web UI — the talk, the demo & learning pages

- **Status:** Accepted
- **Area:** Frontend
- **Related:** [ADR-0002](0002-solution-structure-and-orchestration.md), [ADR-0003](0003-search-api-contract-and-debug-trace.md), [ADR-0005](0005-curated-dataset-and-golden-queries.md), [ADR-0013](0013-domain-ontology-and-compatibility.md), [ADR-0016](0016-rag-grounding-and-citations.md), [ADR-0017](0017-pedagogy-engine.md), [ADR-0018](0018-scope-and-going-further.md)

## Context

The audience needs to watch the *same query* change as the presenter steps through the stages, with the mechanics (SQL, distances, fusion maths, concepts, rule checks, prompts) next to the results. An API explorer can't tell that story on stage.

**The UI replaces the slides.** Talk content, live demo, glossary and these decisions live in one place, so the talk and the demo can't drift apart, and anyone who clones the repository gets the whole talk.

The frontend has to be as readable as the backend: no black-box component libraries, no state management to study first.

## Decision

### Stack and hosting

- **React, Vite and strict TypeScript** in `src/web-ui`.
- **Tailwind CSS and shadcn/ui**: the components are copied into the repository, so every line is readable. Lucide icons, React Router, `react-markdown` for content.
- **Aspire runs the Vite dev server** and passes it the Search API's address. **Vite proxies `/api`**, so the browser only talks to one origin and the API needs no CORS settings.

### Pages

| Route | Page |
|---|---|
| `/` | **Home:** speaker, talk title, the thesis and the triad (Search → Ontology → Pedagogy) |
| `/talk/:step/:tab?` | **Talk mode:** a linear sequence driven by ← and →, one position per step. Stage steps show the live stage screen with a golden query and preset options |
| `/demo` | **Demo:** the same stage screen, with a filter sidebar and every tab |
| `/glossary` | **Glossary:** searchable terms and acronyms |
| `/decisions`, `/decisions/:id` | **Decisions:** these records, rendered |

### Content is markdown, not React

`src/web-ui/content/` holds `talk.json` and `talk/*.md` (the talk steps), `stages/*.md` (one explanation per stage, with fixed headings: *What it is · How it works · What to look for · Strength · Failure mode · Try this · Read the decision*), `going-further/*.md`, `glossary.json` and `speaker.md`.

- **Inline glossary terms:** `[RRF](term:rrf)` renders as an underlined term with a hover card, through a custom link renderer, with no plugin.
- **`going-further/{stage}.md` is free-form**, unlike a stage explanation: each stage's horizon needs a different shape. There is no file for `structured`, and a missing file is what makes the tab unavailable, so absence needs no special case in code.
- **A "Going further" step** after Stage 7 maps the topics that sit around the pipeline rather than inside one stage of it ([ADR-0018](0018-scope-and-going-further.md)).
- **These decisions are read from `docs/decisions/` at build time** with `import.meta.glob`. Links between records become links between pages. There is no copy to drift and no API endpoint.

### Data flow

- **`openapi-typescript` generates the API types** from the API's OpenAPI document (`npm run gen:api`). The generated file is committed, so the UI builds without the API running and contract changes show up in diffs. A small typed `fetch` wrapper replaces a generated client.
- **One hook, `usePipelineSearch`**, sends the same request to whichever stage is selected. An `AbortController` cancels a search when the stage changes.
  - It asks for **50 results per page**, the API's maximum. Stage 5 keeps flagged items but sorts them last, so a page of 10 would hide GQ-03's near miss.
  - For ranked stages it then **reads every remaining page**. Fusion keeps the union of two lists, so Stage 5 can return more than 50: GQ-08 returns 67, with its flagged chargers last. Stage 1 keeps its first page, because its total counts the whole filtered catalog.
  - One response holds every candidate, so switching tabs never refetches.
- **`useAnswerStream`** (Stages 6–7) runs **in parallel**, so results never wait for the LLM. It reads Server-Sent Events with `fetch` and a small parser, because `EventSource` can't send a POST body.
- **No global state library and no TanStack Query.** Plain React state is enough, and easier to read.
- **The URL holds the state**, including stage, tab, query, audience and toggles, so the browser's back button works and a demo can be bookmarked.

### The stage screen

Each stage has five tabs, so the presenter controls what the audience looks at, and each view gets the full width:

| Tab | Shows | Key |
|---|---|---|
| **How it works** | The stage explanation, with glossary hover cards | H |
| **Results** | Candidates with rank, concept and compatibility badges. Stage 5 groups them: in concept, out of concept (collapsed, "kept, moved down"), and a **Flagged** column showing every check with *has* and *needs* values | R |
| **Answer** | Stages 6–7: the streamed answer with product chips, the explanation, and exactly the evidence the model was given | A |
| **Under the hood** | The trace as a flow of step chips, and the selected step in a purpose-built view (SQL, tsquery, distances, RRF table, concepts, rule checks, prompts), with JSON as the fallback | U |
| **Going further** | Stages 2–7: where this technique goes next, in prose and glossary links ([ADR-0018](0018-scope-and-going-further.md)) | G |

- **Answer and Going further are conditional**, and an unavailable tab stays visible and disabled with the reason beside it, rather than disappearing. A tab that comes and goes as the stage changes moves every other tab under the presenter's hand.
- **Filters come from the ontology:** categories from `GET /api/taxonomy`, spec values from `GET /api/vocabularies`. Nothing is hard-coded, so a Turtle edit appears in the filters after a restart.
- **Honest labels:** a flagged card says "#2 before rules", Stage 5's own fused rank, not the standalone hybrid stage's rank.

### Talk mode: one position per step

`content/talk.json` lists the steps in order. **← and → move one step**, and a stage step declares the single `tab` it lands on; the letter keys move between tabs within a step. A talk is a sequence of arguments, not of panels, so the arrow keys should change the argument. Tab-by-tab arrows made the presenter count keypresses to reach the next stage, and pressing a letter left the count wrong.

- **Each stage is one position**, seeded with its golden query and options. Stage 7 lands on its baseline, and the presenter flips **Apply pedagogy** and then the audience live: fewer positions, and the audience watches one thing change on screen rather than between screens.
- **Every stage opens on How it works**, in the demo as well as the talk: the technique is explained before its results are argued about. A step can name another tab, and the letter keys are always a keypress away.
- **The stepper moves the talk.** Choosing a stage in talk mode goes to that stage's step, so its caption, golden query and preset options arrive with the stage. In the demo it changes the stage in place, keeping the query and the filters — that is the demo's whole argument: same input, switchable technique.
- **Content steps** (`intro` and `summary` kinds) are full-width markdown with no demo. They can sit anywhere in the order, not only at the ends, so a turn in the argument can have a page of its own.
- Changes the presenter makes during a step (a toggle, the query) last until the talk moves on; the next step starts from its own preset.

### Visual design

- **Brand colours carry the argument.** The three logo colours mark the triad: **purple** for Search (Stages 1–4), **green** for Ontology (Stage 5), **orange** for Pedagogy (Stages 6–7). RAG is orange, not purple: it retrieves nothing new, it decides what to say.
- **Status colours are separate.** Compatible is green, **Incompatible is red, never orange** (orange means Pedagogy), Unknown is amber. Every badge has an icon *and* text.
- **Light and dark themes** from CSS variables, following the system setting, with a toggle.
- **Type:** Dosis for the logo and talk title only; **Atkinson Hyperlegible** for everything else, because l/I/1 and 0/O are distinct, which matters for product IDs on a projector; JetBrains Mono for SQL and formulas. All three are **self-hosted**, so the talk works without venue Wi-Fi.
- **Presentation mode** enlarges text for a projector, and is on by default in talk mode.

### Quality bar

- `npm run typecheck`, `lint` and `build` must pass; Prettier formats everything.
- Accessibility: landmarks and visible focus, text as well as colour on badges, ARIA tablists for the stepper and tabs, a fully keyboard-driven talk (← and → move between steps; H, R, A, U and G jump to a tab), text contrast of at least 4.5:1 in both themes.
- **Vitest** tests the hooks, trace-renderer choice, talk navigation, Stage 5 grouping and **content integrity**: every talk file exists, every golden query and glossary term resolves, and no link between these records is broken. Golden-query behaviour is tested against the real API, not in the browser.

## Consequences

- One `aspire run` gives the talk, the demo, the glossary and the decisions.
- **Rehearsing the talk is testing the product:** a broken stage shows up in rehearsal.
- Writing content is real work: talk steps, stage explanations and glossary entries.
- The UI runs on the Vite dev server only; a production build and hosting are out of scope.
- Tabs cost a few keypresses per stage, and let each view use large type.

## Alternatives considered

| Option | Why not (for this repository) |
|---|---|
| Slides plus a separate demo | Content drifts, context-switching on stage, and learners don't get the talk |
| MDX | Mixes code into content and adds build tooling |
| Serve the decisions from an API endpoint | A backend job unrelated to search; a build-time import is simpler |
| Next.js | Server rendering isn't needed; more concepts to learn |
| TanStack Query | Excellent, but caching and retries hide the request-per-stage behaviour we want visible |
| `EventSource` or SignalR for the answer stream | `EventSource` can't POST; SignalR is heavy for a one-way stream |
| Everything on one screen | Tried in the design mock-ups: at 1280×720 the trace text fell below back-row size |
| ← and → walking each step's tabs before moving on | Twenty-nine positions to reach eleven arguments, and one letter key left the presenter's count wrong |
| Hiding a tab the stage doesn't have | Every other tab shifts under the presenter's hand when the stage changes |
| Google Fonts `<link>` | The talk would break without venue internet |
| Orange for Incompatible | Reads as "Pedagogy"; status and brand colours stay separate |

## What to take away

- The UI is an **instrument for the experiment**: same input, switchable technique, visible internals.
- Put content in content files and behaviour in code, so each stays easy to change.
- Explaining terms where they appear is pedagogy applied to the tool itself.
- Generated API types keep the frontend and backend honest with each other.
- **Design with real data.** The design mock-ups, built from real API output, showed that at a page size of 10, Stage 5's flagged items weren't on page one.
- **A page size is an assumption about your data.** "50 results is every candidate" was true with 60 products and false with 300. Growing the catalog is what exposed it.

# Design reference — Pi & Mash look and feel

The visual design for the `web-ui`, agreed with Pete on 2026-09-14. The decision it records is in [ADR-0014 § Visual design](../adr/0014-web-ui-architecture.md#visual-design); this folder holds the pictures.

- **Editable canvas:** [Needle Look and Feel](https://claude.ai/code/artifact/6bb701e8-6cdb-4fcb-b17e-c8a6669c36a8) (private to Pete; the PNGs below are the committed record).
- **Data:** every result, score and trace value on the Stage 1–5 screens is real GQ-01 output from the API. The Stage 7 answer text and timings are **sample** text written from ADR-0016 and ADR-0017, because Stages 6–7 are built in Phase 4.
- Screens are drawn at the projector target (1280×720, presentation mode on) except the Demo (1440×900, presentation mode off).

## Screens

| Screen | Light | Dark |
|---|---|---|
| Home: title, thesis, triad, speaker card with LinkedIn QR slot | [home-light.png](screens/home-light.png) | [home-dark.png](screens/home-dark.png) |
| Stage 5 · How it works: stage explanation with a glossary hover card | [stage-5-how-it-works.png](screens/stage-5-how-it-works.png) | |
| Stage 5 · Results: in concept, out of concept, flagged (never hidden) | [stage-5-results.png](screens/stage-5-results.png) | [stage-5-results-dark.png](screens/stage-5-results-dark.png) |
| Stage 5 · Under the hood: trace steps as a flow, RRF maths, rule checks | [stage-5-under-the-hood.png](screens/stage-5-under-the-hood.png) | |
| Stage 7 · Answer: Stage 6 answer, streaming explanation, evidence set | [stage-7-answer.png](screens/stage-7-answer.png) | [stage-7-answer-dark.png](screens/stage-7-answer-dark.png) |
| Demo: filter sidebar from the ontology, Stage 4 results with signal badges | [demo-filters.png](screens/demo-filters.png) | |
| Look and feel: palette, type, badges, tabs, shapes | [look-and-feel.png](screens/look-and-feel.png) | |

## The design in one page

- **Brand = the triad.** The three Pi & Mash logo colours mark the three parts of the argument: **purple** `#4f46e5` Search (Stages 1–4), **green** `#10b981` Ontology (Stage 5), **orange** `#ff7a59` Pedagogy (Stages 6–7). RAG sits with Pedagogy: Stage 6 decides *what to say*, Stage 7 *how to say it*.
- **Status colours are separate from brand colours.** Incompatible is red, never orange. Every status badge has an icon and text.
- **Type.** Dosis for the logo wordmark and the talk title only; Atkinson Hyperlegible for everything else; JetBrains Mono for SQL, formulas and IDs in traces. All three are SIL Open Font License 1.1 and self-hosted.
- **Shape language from the logo.** Circles for stage and step numbers, pills for controls, 18px card radius, 2px borders, flat colour.
- **Logos.** Filled logo on light, outline logo on dark (`src/web-ui/assets/images/`).
- **Stage tabs.** Each stage has *How it works · Results · Answer · Under the hood*. → steps through them in the talk; the presenter can jump to any tab when someone asks a question. Answer is disabled before Stage 6. *The screens draw Results first; the order above is the agreed one (confirmed 2026-09-15), and the screens were not redrawn for it.*
- **Stage options sit on the tab row**, next to what they change: Stage 5's *Expand synonyms* / *Apply constraints*; the audience picker and Stage 7's *Apply pedagogy*.
- **Filters** open from a *Filters* button in every search bar: a sidebar in the Demo, a drawer in talk mode. Every category and spec value comes from `/api/taxonomy` and `/api/vocabularies`.

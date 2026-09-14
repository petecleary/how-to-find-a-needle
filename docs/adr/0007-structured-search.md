# ADR-0007: Stage 1 — Structured search

- **Status:** Accepted (Phase 2, 2026-09-14). Amended 2026-09-14 by [ADR-0018](0018-scope-and-going-further.md): BGE-M3 removed and stages renumbered, with no change in behaviour (code updated in the Phase 2 rework).
- **Date:** 2026-09-13
- **Related:** ADR-0003, ADR-0004, ADR-0006; golden query GQ-04; roadmap Phase 2

## Context

The talk opens with the argument that a simple `WHERE` clause is sometimes the best search. When users know exactly what they want (brand, voltage, price range), fuzzy ranking only gets in the way. Stage 1 also sets the baseline for everything that follows: high precision, zero tolerance for vague intent.

## Decision

- `IStructuredSearch` builds **parameterised SQL** from `filters` only. `query` is ignored, and the trace says so explicitly.
- Supported filters:
  - `brand`: exact, case-insensitive.
  - `categories`: match **any** of the given taxonomy notations, using array overlap `categories && @categories` (GIN index).
    - Notations are validated against the ontology ([ADR-0013](0013-domain-ontology-and-compatibility.md)).
    - A broader concept includes its narrower ones, so filtering by `chargers` also matches `laptop-chargers`. The expansion is deterministic and shown in the trace.
  - `minPrice` / `maxPrice`.
  - `specs`: key/value pairs matched with JSONB containment `specs @> @specs::jsonb`. This uses the GIN index and treats numbers as numbers.
- Ordering: `ORDER BY price, id`. The ordering is deterministic, and **there is no relevance score**; `score` is `null` and `signals.structuredMatch = true`.
- `totalResults` is a real `COUNT(*)` over the filter, since this stage isn't bounded by candidate depth.
- **Dynamic SQL, safely:** WHERE clauses are appended from a fixed set of fragments, and values are always parameters. Unknown `specs` keys are allowed, because containment is safe. Keys are validated against `^[a-zA-Z][a-zA-Z0-9]*$` for tidy traces.
- Trace: the exact SQL, parameter values, row count and timing, plus a note: *"Structured search can't understand 'something to charge my laptop'. It only matches the attributes you give it."*
- **The filter-building code is shared.** Stages 2–7 reuse the same `SqlFilterBuilder` (Stages 6–7 through the Stage 5 pipeline) so that filters mean the same thing in every stage ([ADR-0003](0003-search-api-contract-and-debug-trace.md)).

## Consequences

- Stage 1 is the fastest and most precise stage, and the talk shows exactly that.
- A request with no filters returns the whole catalog, paged. That is intentional and shows that structured search has no notion of relevance.

## Alternatives considered

| Option | Why not (for this repo) |
|---|---|
| Parse filters out of free text ("18V Brakk under £100") | That is query understanding, a different talk; hides the point that structured search needs structured input |
| Typed columns per spec (voltage, wattage…) | Doesn't scale across categories; JSONB containment is simpler and indexed |
| Ignore filters in later stages | Real systems combine filtering and ranking; learners should see that |

## Teaching notes

- Normalised data plus exact filters give perfect precision for known-item search.
- The limitation is the lesson: users don't speak in attributes.

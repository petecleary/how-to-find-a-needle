# ADR-0007: Stage 1 — Structured search

- **Status:** Accepted
- **Area:** Search
- **Related:** [ADR-0003](0003-search-api-contract-and-debug-trace.md), [ADR-0004](0004-pipeline-composition.md), [ADR-0006](0006-database-schema-and-seeding.md); golden query GQ-01

## Context

The talk opens with an unfashionable claim: sometimes a `WHERE` clause is the best search there is. When a shopper knows exactly what they want (brand, voltage, price), fuzzy ranking only gets in the way. Stage 1 also sets the baseline for everything after it: perfect precision, and no tolerance at all for vague intent.

## Decision

- `IStructuredSearch` builds **parameterised SQL from `filters` only**. The `query` text is ignored, and the trace says so.
- **Filters:**
  - `brand`: exact, case-insensitive.
  - `categories`: any of the given concept notations, with array overlap `categories && @categories` (GIN index). **A broader category includes its narrower ones**: `chargers` also matches `laptop-chargers`. The expansion comes from the ontology and is shown in the trace.
  - `minPrice` and `maxPrice`.
  - `specs`: JSONB containment, `specs @> @specs::jsonb`. It uses the GIN index and compares numbers as numbers: `{"voltageV": 18}` matches `18`, not `"18V"`.
- **No relevance score.** Results are ordered by `price, id`, `score` is `null`, and `signals.structuredMatch` is `true`.
- **A real count.** This stage isn't bounded by candidate depth, so it pages in SQL (`LIMIT`/`OFFSET`) and runs a `COUNT(*)`.
- **Dynamic SQL, safely.** WHERE clauses are appended from a fixed set of fragments, and every value is a parameter. Spec keys must look like identifiers, so the trace stays tidy.
- **One filter builder for every stage.** Stages 2–7 reuse the same `SqlFilterBuilder`, so a filter means exactly the same thing whichever stage you pick.

```sql
SELECT id, name, brand, categories, price, specs
FROM products
WHERE lower(brand) = lower(@brand)
  AND price <= @maxPrice
  AND specs @> @specs::jsonb
ORDER BY price, id
LIMIT @limit OFFSET @offset;
```

## Consequences

- Stage 1 is the fastest and most precise stage, and the talk shows exactly that: GQ-01 (Brakk, 18V, under £100) returns exactly six products from a catalog of 300.
- A request with no filters returns the whole catalog, paged. That is deliberate: structured search has no idea what "relevant" means.

## Alternatives considered

| Option | Why not (for this repository) |
|---|---|
| Parse filters out of free text ("18V Brakk under £100") | That is query understanding, a different technique; it would hide the point that structured search needs structured input |
| A typed column per spec (voltage, wattage…) | Doesn't scale across product types; JSONB containment is simpler and indexed |
| Ignore filters in later stages | Real systems combine filtering and ranking, and learners should see that |

## What to take away

- Normalised data plus exact filters give perfect precision for known-item search.
- The limitation is the lesson: people don't speak in attributes. "Something to charge my laptop" matches no filter at all.
- Filters are pre-filters in every later stage too. Similarity only decides the order *within* what the filters allow.

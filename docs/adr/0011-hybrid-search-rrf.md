# ADR-0011: Stage 4 — Hybrid search with Reciprocal Rank Fusion

- **Status:** Proposed
- **Date:** 2026-09-13
- **Related:** ADR-0004, ADR-0008, ADR-0010, ADR-0012; golden queries GQ-01 to GQ-03; roadmap Phase 2

## Context

Keyword search is precise but misses synonyms. Vector search understands meaning but misses exact terms and model numbers. Combining the two usually beats either one alone.

Their scores are on incompatible scales (`ts_rank_cd` against cosine distance), so we can't simply add them. Rank-based fusion avoids calibrating scores entirely.

## Decision

### Algorithm (`Pipeline/Fusion/ReciprocalRankFusion.cs`, pure function)

For each document *d* across ranked lists *i* (1-based rank *rᵢ(d)*, weight *wᵢ*):

```text
RRF(d) = Σᵢ  wᵢ / (k + rᵢ(d))        — a list that doesn't contain d contributes 0
```

- **`k = 60` by default** (Cormack, Clarke & Büttcher, 2009). It can be changed through `options.rrfK`.
- **Weights:** `options.keywordWeight` and `options.vectorWeight`, both 1.0 by default (plain RRF). Weighted RRF is shown as a tuning knob, not a default.
- **Inputs:** Keyword and Vector candidate lists, each retrieved to `candidateDepth` (default 50) with the same filters ([ADR-0004](0004-pipeline-composition.md)). The two retrievals run concurrently (`Task.WhenAll`), each with its own connection.
- **Ties:** sort by RRF score descending, then by best individual rank, then by `id`, so ordering is deterministic.
- **Output signals:** `keywordRank`, `vectorRank`, `vectorDistance`, `keywordScore` and `fusedRank` for every result. Missing ranks are `null`.
- **Trace:** a per-result formula string with the numbers filled in, for example:
  - `PROD-0012: 1/(60+2) + 1/(60+1) = 0.03252`
  - `PROD-0031: 1/(60+7) + — = 0.01493 (not in vector list)`

  Also: list sizes, overlap count, k, weights.
- **Signature** (generic, reused by Stage 5):

```csharp
IReadOnlyList<FusedItem> Fuse(IReadOnlyList<RankedList> lists, int k);
// RankedList(string Name, double Weight, IReadOnlyList<string> OrderedIds)
```

### Unit tests (exhaustive, since this is pure maths)

- Hand-calculated examples, including one worked through in the talk.
- A document in only one list; an empty list; single-list fusion equals the original order.
- Weight 0 removes a list's influence. Tie-breaking is deterministic. `k` changes how strongly top ranks dominate.

## Consequences

- No score normalisation or calibration is needed. The formula fits on a slide and the trace shows real numbers.
- RRF ignores *how much* better one result is than the next. Close-scored and far-apart items fuse the same way. The talk mentions this.
- Hybrid still ranks the incompatible charger well (GQ-01). Fusion improves relevance, **not** correctness, which sets up Stage 6.

## Alternatives considered

| Option | Why not (for this repo) |
|---|---|
| Weighted score sum (min-max or z-score normalised) | Needs per-query normalisation; fragile and harder to explain |
| Postgres-side RRF in one SQL statement | Possible with CTEs, but hides the maths from the C# trace and tests |
| Learned re-ranker / cross-encoder | Better quality, but another model; a possible "going further" note |
| Convex combination with tuned α | Needs labelled data to tune; golden queries are too few |

## Teaching notes

- Fuse **ranks**, not scores, when the scores come from different universes.
- Hybrid search is the pragmatic default for most production search today.

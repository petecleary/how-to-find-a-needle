# ADR-0011: Stage 4 — Hybrid search with Reciprocal Rank Fusion

- **Status:** Accepted
- **Area:** Search
- **Related:** [ADR-0004](0004-pipeline-composition.md), [ADR-0008](0008-keyword-search-bm25-style.md), [ADR-0010](0010-vector-search-pgvector.md), [ADR-0018](0018-scope-and-going-further.md); golden queries GQ-01, GQ-02, GQ-03

## Context

Keyword search is precise but misses synonyms. Vector search understands meaning but misses exact terms and model numbers. Combining them usually beats either alone.

Their scores come from different universes (`ts_rank_cd` against cosine distance), so they can't simply be added. Fusing **ranks** avoids calibrating scores at all.

## Decision

### The algorithm: a pure function

For each product *d*, over ranked lists *i*, with 1-based rank *rᵢ(d)* and weight *wᵢ*:

```text
RRF(d) = Σᵢ  wᵢ / (k + rᵢ(d))        — a list that doesn't contain d adds nothing
```

- **k = 60 by default**, the value from the original paper (Cormack, Clarke & Büttcher, 2009). A larger k flattens the difference between top ranks. It can be changed with `options.rrfK`.
- **Weights** default to 1.0 for both lists (plain RRF). `keywordWeight` and `vectorWeight` are there to experiment with, not tuned defaults.
- **Inputs:** keyword and vector results, each retrieved to `candidateDepth` with the same filters, run concurrently.
- **Output:** the union of both lists. A product found by only one retriever still takes part, which is why hybrid search can return more than `candidateDepth` results.
- **Ties** break on the better individual rank, then on `id`, so the order is deterministic.
- **Signals:** every result keeps its `keywordRank`, `vectorRank`, `vectorDistance` and `fusedRank`; a missing rank is `null`.
- **Trace:** one formula per result, with the numbers filled in:
  - `PROD-0012: 1/(60+1) + 1/(60+1) = 0.03279` (1st in both lists)
  - `PROD-1012: — + 1/(60+3) = 0.01587` (not in the keyword list)

```csharp
IReadOnlyList<FusedItem> Fuse(IReadOnlyList<RankedList> lists, int k);
// RankedList(string Name, double Weight, IReadOnlyList<string> OrderedIds)
```

### Tested exhaustively

Hand-calculated examples; a product in only one list; an empty list; one list returns its own order; weight 0 removes a list; ties are deterministic; k changes how much top ranks dominate.

## Consequences

- No score normalisation or calibration. The formula fits on a slide, and the trace shows real numbers.
- RRF ignores *how much* better one result is than the next: close scores and far-apart scores fuse the same way.
- **Hybrid search still ranks the incompatible charger 2nd for GQ-01.** Fusion improves relevance, **not** correctness. That sets up Stage 5.

## Alternatives considered

| Option | Why not (for this repository) |
|---|---|
| A weighted sum of normalised scores (min-max, z-score) | Needs per-query normalisation; fragile and harder to explain |
| RRF inside one SQL statement | Possible with CTEs, but hides the maths from the C# trace and tests |
| A learned re-ranker or cross-encoder | Better quality, but another model; part of the going-further step ([ADR-0018](0018-scope-and-going-further.md)) |
| A tuned convex combination (α) | Needs labelled data to tune; eight golden queries are far too few |

## What to take away

- **Fuse ranks, not scores**, when the scores come from different universes.
- Hybrid search is the pragmatic default for most production search today.
- **Appearing in both lists matters more than being first in one.** 1st and 2nd place in one list differ by 1/61 − 1/62 = 0.00026; appearing in a second list at all adds at least 1/110 = 0.009.
- **A strong keyword trap survives fusion (GQ-03).** For "cordless drill battery", keyword search ranks the phone battery 1st and vector search ranks it 10th: 1/(60+1) + 1/(60+10) = 0.03068. The drill battery, 4th in keyword and 2nd in vector, scores 1/(60+4) + 1/(60+2) = 0.03175. Hybrid search puts the right battery above the trap, but the trap stays 4th. Being at the top of *one* list is worth almost as much as doing well in both. Only the ontology knows it is a phone battery.
- **The margins are tiny.** In that same query, 3rd, 4th and 5th place score 0.03080, 0.03068 and 0.03054. RRF rankings can flip on a single rank change, which is why golden queries assert loose bounds rather than exact positions.

# ADR-0010: Stage 3 — Vector search (pgvector)

- **Status:** Accepted (Phase 2, 2026-09-14). Amended 2026-09-14 by [ADR-0018](0018-scope-and-going-further.md): BGE-M3 removed and stages renumbered, with no change in behaviour (code updated in the Phase 2 rework).
- **Date:** 2026-09-13
- **Related:** ADR-0006, ADR-0009, ADR-0011; golden queries GQ-01, GQ-02, GQ-03; roadmap Phase 2

## Context

Semantic search finds products by meaning: "power brick" matches "AC adapter". It is also where the talk's central warning appears: **similarity ≠ compatibility**. A 45W barrel charger and a 65W USB-C charger embed close together.

We already run Postgres with pgvector, so vectors live next to the structured data and filters.

## Decision

```sql
SET LOCAL hnsw.ef_search = 100;  -- must be >= candidate depth to avoid truncated results

SELECT id, name, brand, categories, price, specs,
       embedding_dense <=> @queryVector AS distance
FROM products
WHERE embedding_model = @activeModel   -- only vectors from the configured provider (ADR-0009)
  -- shared filter fragments from SqlFilterBuilder (ADR-0007); each line is added only when that filter is supplied
  AND categories && @categories      -- category notations, broader concepts expanded to narrower
  AND brand = @brand
  AND price BETWEEN @minPrice AND @maxPrice
  AND specs @> @specs::jsonb         -- normalised spec values, e.g. {"connector":"usb-c"}
ORDER BY embedding_dense <=> @queryVector, id
LIMIT @depth;
```

- **Metric:** cosine distance `<=>`. The response shows `vectorDistance` (0 = identical) and `score = 1 − distance` (cosine similarity). Because vectors are normalised ([ADR-0009](0009-local-embeddings-onnx-runtime.md)), inner product would rank identically; cosine is chosen because it is the most familiar to explain.
- **Index:** HNSW with `vector_cosine_ops`, pgvector defaults (`m = 16`, `ef_construction = 64`). Queries run in a transaction with `SET LOCAL hnsw.ef_search = 100`. The default of 40 would silently cap results below the default candidate depth of 50.
- **Filters narrow first, then similarity ranks.**
  - The request's normalised filters (categories, brand, price, specs) are applied in the **same SQL statement** as the vector ordering, using the shared `SqlFilterBuilder`. They mean exactly what they mean in Stage 1.
  - Similarity only decides the order *within* products that already satisfy the hard constraints. For example, "power brick" with `categories: ["laptop-chargers"]` and `maxPrice: 50` never returns a phone battery or a £90 charger, however similar they are.
  - The trace shows the filter fragments and parameters alongside the distances.
- **Filters with approximate indexes:** filtering can reduce HNSW recall, because the index finds neighbours first and filters afterwards. At demo scale the planner usually does an exact scan, so results are exact. The ADR and trace note that production systems handle this with pgvector's iterative index scans (`hnsw.iterative_scan`, pgvector ≥ 0.8), partial indexes or over-fetching.
- **No similarity threshold.** Vector search always returns *something*, even for nonsense queries. That is a teaching point, shown by a note in the trace.
- **Trace:** query embedding details (from the embedder step), the SQL, `ef_search`, whether the planner used the index (from `EXPLAIN` in development, run once per request only if `options.explain` is true), and per-result distances.

## Consequences

- GQ-02 (synonym) succeeds here where keyword search failed.
- GQ-01 shows the incompatible 45W barrel charger ranked near the top. That is the intended failure the ontology fixes in Stage 5.
- GQ-03 (keyword trap) is corrected: "cordless phone battery" is less similar to "cordless drill battery" than real drill batteries.

## Alternatives considered

| Option | Why not (for this repo) |
|---|---|
| Dedicated vector DB (Qdrant, Weaviate, Pinecone) | Extra infrastructure; loses SQL filters and joins in the same query |
| IVFFlat index | Needs training data and `lists` tuning; HNSW is simpler to explain and has better recall |
| Brute force in C# | Hides the database's role; doesn't teach indexing |
| Similarity cut-off | Arbitrary thresholds vary by model; better to show the raw behaviour |

## Teaching notes

- Nearest neighbour ≠ right answer. Vector search is a *candidate generator* with no notion of constraints.
- Approximate indexes trade recall for speed. Know your `ef_search`.
- Normalised filters remove whole classes of wrong answers that similarity never can. Filtering and vector ranking in one SQL statement is a strong reason to keep vectors next to your structured data.
- The wider vector landscape (dedicated vector databases, multilingual and learned-sparse models such as BGE-M3, chunking long documents) is discussed in the talk's going-further step, not built ([ADR-0018](0018-scope-and-going-further.md)).

**For the talk (found while building, Phase 2):**
- **Named entities pull embeddings (GQ-08).** "charger for my Blackbird Aerobook 14" ranks:
  1. the Blackbird charger (its description names the Aerobook);
  2. the Aerobook 14 itself;
  3. and 4. two more Blackbird laptops;
  5. a Blackbird laptop sleeve;
  6. a Blackbird backpack.

  Only then comes the Voltline 65W charger, at 7th. The embedding captures "Blackbird things" more strongly than "a charger". A vector has no notion of which words are the goal and which are context.
- **Same effect with brands:** "battery for Brakk 18V drill" ranked every Brakk item, including drills, an angle grinder and a work light, above the Tornio 20V MAX battery (11th). As "18V battery" the Tornio battery is in the top 5, and the near miss is visible again.
- **The fix isn't a better embedding, it's understanding the query first** (Stage 5, ADR-0013). This is the thesis in one example.
- **Similarity scores are compressed.** For GQ-08 the top 10 spans cosine similarity 0.88 to 0.71. "Close" and "right" are not the same thing, and there is no threshold that separates them.

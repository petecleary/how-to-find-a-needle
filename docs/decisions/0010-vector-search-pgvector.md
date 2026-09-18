# ADR-0010: Stage 3 — Vector search (pgvector)

- **Status:** Accepted
- **Area:** Search
- **Related:** [ADR-0006](0006-database-schema-and-seeding.md), [ADR-0009](0009-local-embeddings-onnx-runtime.md), [ADR-0011](0011-hybrid-search-rrf.md); golden queries GQ-02, GQ-03, GQ-04, GQ-08

## Context

Semantic search finds products by meaning: a query can match a product that shares none of its words. It is also where the talk's central warning appears: **similarity is not compatibility**. A 45W barrel charger and a 65W USB-C charger are described almost identically, so they embed close together.

Postgres with pgvector is already running, so vectors can live next to the structured data and its filters.

## Decision

```sql
SELECT set_config('hnsw.ef_search', @efSearch, true);  -- this transaction only

SELECT id, name, brand, categories, price, specs,
       embedding_dense <=> @queryVector AS distance
FROM products
WHERE embedding_model = @activeModel          -- never compare vectors from different models
  -- + the shared filters (ADR-0007): categories, brand, price, specs
ORDER BY embedding_dense <=> @queryVector, id
LIMIT @candidateDepth;
```

- **Metric: cosine distance, `<=>`.** 0 means the same direction (same meaning), 1 unrelated. The response shows `vectorDistance`, and `score = 1 − distance`, the cosine similarity. Vectors are normalised, so inner product would rank identically; cosine is the easiest to explain.
- **Index: HNSW** with `vector_cosine_ops` and pgvector's defaults. Each query raises `hnsw.ef_search` to at least 100 for its transaction. The default of 40 would silently return fewer results than the default candidate depth of 50.
- **Filters narrow first, then similarity ranks.** The request's filters are in the **same SQL statement** as the vector ordering, built by the shared filter builder, so they mean exactly what they mean in Stage 1. However similar a £90 charger is, `maxPrice: 50` never returns it.
- **Filtering with an approximate index** can lose recall: the index finds neighbours first and filters afterwards. At this size the planner usually scans exactly. The trace notes how production systems cope: pgvector's iterative index scans, partial indexes, or over-fetching.
- **No similarity threshold.** Vector search always returns *something*, even for nonsense. The trace says so.
- **Trace:** the query's embedding details (model, prefix, token count, the first few dimensions), the SQL, `ef_search`, the distances, and, with `options.explain`, the Postgres query plan, so you can see whether HNSW was used.

## Consequences

- **GQ-03:** the incompatible 45W barrel charger ranks **2nd**, right behind the correct charger. That is the failure the ontology fixes.
- **GQ-04:** "cordless drill battery" puts a drill and the drill battery first; the cordless *phone* battery that tops keyword search drops to 10th.
- **GQ-02:** "power brick for laptop" finds power products that keyword search missed entirely, but ranks power banks above the laptop chargers.

## Alternatives considered

| Option | Why not (for this repository) |
|---|---|
| A dedicated vector database (Qdrant, Weaviate, Pinecone) | More infrastructure, and the filters and joins leave the query |
| IVFFlat index | Needs training data and tuning; HNSW is simpler to explain and has better recall |
| Brute force in C# | Hides the database's role and doesn't teach indexing |
| A similarity cut-off | Thresholds differ by model; better to show the raw behaviour |

## What to take away

- **Nearest neighbour is not the right answer.** Vector search generates candidates. It has no notion of constraints.
- **Approximate indexes trade recall for speed.** Know your `ef_search`.
- **Keep vectors next to your structured data.** Filters remove whole classes of wrong answers that similarity never can, and doing both in one SQL statement is a strong reason to.
- **Named things pull embeddings (GQ-08).** "charger for my Blackbird Aerobook 14" ranks the Blackbird charger 1st, the Aerobook 14 itself 2nd, two more Blackbird laptops, a Blackbird sleeve and a Blackbird backpack, and only then the Voltline charger that fits, at 7th. The embedding captures "Blackbird things" more strongly than "a charger". A vector can't tell what you want from what you own.
- **The same happens with brands.** While the golden queries were being written (with the 60 hand-written products), "battery for Brakk 18V drill" ranked every Brakk product above the Tornio battery that is the real near miss. Asked as "18V battery", the Tornio battery is 3rd, and the near miss is visible again.
- **The fix isn't a better embedding; it's understanding the query first** ([ADR-0013](0013-domain-ontology-and-compatibility.md)).
- **Similarity scores are compressed.** For GQ-08 the top 7 span cosine similarity 0.88 to 0.75. "Close" and "right" are not the same, and no threshold separates them.
- **Meaning is fuzzy in both directions.** "power brick" sits closer to "power bank" than to "laptop charger".
- The wider vector landscape (dedicated databases, multilingual models, chunking long documents) is part of the talk's going-further step, not built ([ADR-0018](0018-scope-and-going-further.md)).

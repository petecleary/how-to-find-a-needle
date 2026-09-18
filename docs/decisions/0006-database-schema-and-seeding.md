# ADR-0006: Database schema & seeding

- **Status:** Accepted
- **Area:** Data
- **Related:** [ADR-0005](0005-curated-dataset-and-golden-queries.md), [ADR-0008](0008-keyword-search-bm25-style.md), [ADR-0009](0009-local-embeddings-onnx-runtime.md), [ADR-0010](0010-vector-search-pgvector.md)

## Context

Every stage queries the same products, so one table has to serve exact filters, full-text search and vector search.

Embedding hundreds of products takes noticeable time with a local model and costs money with a hosted API. Doing that on every restart, or on a learner's first run, would make the repository painful to use.

## Decision

### One Postgres table, one idempotent `init.sql`

```sql
CREATE EXTENSION IF NOT EXISTS vector;

CREATE TABLE IF NOT EXISTS products (
    id              TEXT PRIMARY KEY,
    name            TEXT NOT NULL,
    brand           TEXT NOT NULL,
    categories      TEXT[] NOT NULL,        -- ontology concept notations (ADR-0013)
    price           NUMERIC(10,2) NOT NULL,
    currency        TEXT NOT NULL,
    description     TEXT NOT NULL,
    reviews         TEXT[] NOT NULL DEFAULT '{}',
    specs           JSONB NOT NULL DEFAULT '{}',

    -- Stage 2: a weighted full-text document, kept up to date by Postgres itself
    search_vector   TSVECTOR GENERATED ALWAYS AS (
        setweight(to_tsvector('english', name), 'A') ||
        setweight(to_tsvector('english', brand || ' ' || immutable_array_to_string(categories, ' ')), 'B') ||
        setweight(to_tsvector('english', description), 'C') ||
        setweight(to_tsvector('english', immutable_array_to_string(reviews, ' ')), 'D')
    ) STORED,

    -- Stages 3–7: the product's embedding, and the model that made it
    embedding_dense VECTOR(768),
    embedding_model TEXT,

    content_hash    TEXT NOT NULL,          -- SHA-256 of the text that gets embedded
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_products_brand      ON products (brand);
CREATE INDEX IF NOT EXISTS ix_products_price      ON products (price);
CREATE INDEX IF NOT EXISTS ix_products_categories ON products USING GIN (categories);
CREATE INDEX IF NOT EXISTS ix_products_specs      ON products USING GIN (specs jsonb_path_ops);
CREATE INDEX IF NOT EXISTS ix_products_search     ON products USING GIN (search_vector);
CREATE INDEX IF NOT EXISTS ix_products_dense      ON products USING hnsw (embedding_dense vector_cosine_ops);
```

- `array_to_string` isn't `IMMUTABLE`, which generated columns require, so a small `immutable_array_to_string` wrapper makes categories and reviews indexable.
- **Reviews are indexed, at the lowest weight (D).** Shoppers' words live in reviews, and GQ-04's keyword trap depends on one ([ADR-0008](0008-keyword-search-bm25-style.md)).
- At a few hundred rows the planner may choose a sequential scan over the HNSW index. That is the right choice at this size; the indexes are still created to show the production shape.
- The API gets a pooled `NpgsqlDataSource` from Aspire's Npgsql integration, with pgvector's type mappings registered.

### Seeding before the API accepts requests

`Data/DatabaseSeeder.cs` runs before `app.Run()`, so "healthy" in the Aspire dashboard means the data is ready.

1. **Schema:** run `init.sql`. Running it again changes nothing.
2. **Catalog:** load `products.json` and upsert each product. Only changed rows are written. **If a product's `content_hash` changed, its vectors are set to NULL**, because they no longer describe it. Products removed from the file are deleted.
3. **Embeddings:**
   - Vectors from a different model are cleared first. **Models are never mixed.**
   - **Normal path:** load vectors from the committed `assets/data/embeddings/{provider}.jsonl` for every product whose content hash and model still match. This takes seconds.
   - **Gaps:** new or edited products are embedded live, with a warning suggesting a rebuild.
   - **`Embeddings:Rebuild = true`:** re-embed every product and overwrite the committed file.
4. If the embedding model is unavailable, the vectors stay NULL and a clear warning is logged. Stages 1–2 still work; the stages that need vectors return `503` with the fix.

The log says what happened: `Seeded 300 products (…)` and `Embeddings (nomic): 300 loaded from file, 0 embedded live, 0 missing`.

### No migration tool

A breaking schema change means deleting the Postgres data volume and running the AppHost again. The README shows how.

## Consequences

- Restarts are fast, and editing one product re-embeds only that product.
- A fresh clone loads committed vectors in seconds and gets exactly the vectors the golden queries were tested with.
- The committed file must be rebuilt when products change. A unit test fails if any product's hash doesn't match its line, so staleness is loud.
- Seeding blocks startup. Simple and predictable for a demo; a production system would seed from a separate job.

## Alternatives considered

| Option | Why not (for this repository) |
|---|---|
| Drop and re-seed on every start | Slow restarts; embeddings recomputed for nothing |
| Always embed live on first run | Slower first start, costs hosted-API users money, and results can drift from the tested ones |
| EF Core migrations, DbUp or Flyway | The right choice in production; tooling that distracts from search here |
| Seeding in the background while serving | Stages would return partial results mid-demo |
| A table per embedding model | Cleaner in theory; more joins for nothing at this scale |

## What to take away

- **Embeddings are a cache of your content.** Treat them as derived data you can always rebuild, and store the model and a hash of the source text with every vector.
- Content hashing is a cheap, robust way to keep derived data in step with its source.
- **Indexes are part of search design:** GIN for text and JSONB, HNSW for vectors, B-tree for exact filters.
- **Which fields you index is a relevance decision.** Adding reviews to the text index is what turned a cordless phone battery into keyword search's top result for "cordless drill battery".

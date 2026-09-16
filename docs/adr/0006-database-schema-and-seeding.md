# ADR-0006: Database schema & seeding

- **Status:** Accepted (Phase 2, 2026-09-14). Amended by [ADR-0018](0018-scope-and-going-further.md), which removed the BGE-M3 columns and indexes; re-accepted after the Phase 1 rework was verified (2026-09-14).
- **Date:** 2026-09-13
- **Related:** ADR-0002, ADR-0005, ADR-0008, ADR-0009, ADR-0010, ADR-0018; roadmap Phase 1

## Context

The current `init.sql` mirrors the old Datafiniti CSV, runs `DROP TABLE` on every start, and has no indexes. The connection string is passed around by hand, and there is a legacy `GET /api/products` with its own repository and models.

Embedding ~500 products takes noticeable time with local models, and costs a little with a hosted API. Doing that on every restart, or on every fresh clone, would hurt development, the live demo and a learner's first run.

## Decision

### Connection management

- Use the **`Aspire.Npgsql`** client integration in the API: `builder.AddNpgsqlDataSource("pi-teach-db-search", configureDataSourceBuilder: b => b.UseVector())`.
- The **`Pgvector`** NuGet package provides `Vector`, `SparseVector` and `HalfVector` Npgsql mappings.
- Services inject a pooled `NpgsqlDataSource`. The hand-rolled connection-string constructors are removed.

### Schema (`assets/data/init.sql`, idempotent)

```sql
CREATE EXTENSION IF NOT EXISTS vector;

-- array_to_string is only STABLE, and generated columns require IMMUTABLE expressions.
-- This wrapper is safe for text[] and lets search_vector include categories.
CREATE OR REPLACE FUNCTION immutable_array_to_string(text[], text)
RETURNS text LANGUAGE sql IMMUTABLE PARALLEL SAFE
AS $$ SELECT array_to_string($1, $2) $$;

CREATE TABLE IF NOT EXISTS products (
    id              TEXT PRIMARY KEY,
    name            TEXT NOT NULL,
    brand           TEXT NOT NULL,
    categories      TEXT[] NOT NULL,   -- SKOS concept notations (ADR-0013)
    price           NUMERIC(10,2) NOT NULL,
    currency        TEXT NOT NULL,
    description     TEXT NOT NULL,
    reviews         TEXT[] NOT NULL DEFAULT '{}',
    specs           JSONB NOT NULL DEFAULT '{}',

    -- Stage 2: weighted full-text document (ADR-0008)
    search_vector   TSVECTOR GENERATED ALWAYS AS (
        setweight(to_tsvector('english', name), 'A') ||
        setweight(to_tsvector('english', brand || ' ' || immutable_array_to_string(categories, ' ')), 'B') ||
        setweight(to_tsvector('english', description), 'C') ||
        setweight(to_tsvector('english', immutable_array_to_string(reviews, ' ')), 'D')
    ) STORED,

    -- Stages 3, 4, 5: dense embedding from the configured provider (ADR-0009)
    embedding_dense      VECTOR(768),
    embedding_model      TEXT,              -- e.g. nomic-embed-text-v1.5-int8; models are never mixed

    content_hash    TEXT NOT NULL,   -- hash of the text that is embedded
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_products_brand     ON products (brand);
CREATE INDEX IF NOT EXISTS ix_products_categories ON products USING GIN (categories);
CREATE INDEX IF NOT EXISTS ix_products_price     ON products (price);
CREATE INDEX IF NOT EXISTS ix_products_specs     ON products USING GIN (specs jsonb_path_ops);
CREATE INDEX IF NOT EXISTS ix_products_search    ON products USING GIN (search_vector);
CREATE INDEX IF NOT EXISTS ix_products_dense     ON products USING hnsw (embedding_dense vector_cosine_ops);
```

Notes:
- `reviews` **are** in `search_vector`, at the lowest weight, D (changed in Phase 2; the first draft left them out to avoid lexical noise).
  - *Why:* real shoppers' words live in reviews ("Cordless phone battery arrived quickly"), and GQ-03's keyword trap depends on them.
  - GQ-02 is unaffected, because no review says "power brick".
  - Weight D keeps a review match below a name, brand, category or description match.
- **Changing a generated column in place:** PostgreSQL 16 can't alter a generated column's expression. So `init.sql` checks the stored expression, and if `reviews` is missing it drops and re-adds `search_vector` (the GIN index is then recreated by its `IF NOT EXISTS`). Existing volumes migrate on the next start, without a reset.
- **Removing the BGE-M3 columns in place** ([ADR-0018](0018-scope-and-going-further.md)): `init.sql` runs `DROP INDEX IF EXISTS` for `ix_products_bge_dense` and `ix_products_bge_sparse`, then `ALTER TABLE products DROP COLUMN IF EXISTS` for `embedding_bge_dense` and `embedding_bge_sparse`. Existing volumes lose them on the next start without a reset; the seeder no longer mentions them.
- `pg_trgm` is **not** installed. Keyword search uses full-text search only ([ADR-0008](0008-keyword-search-bm25-style.md)).
- Keep the `pgvector/pgvector:pg16` image unless Phase 1 finds a reason to move to pg17. Pin the tag in `AppHost.cs`.
- At 60–500 rows the planner may choose a sequential scan over HNSW. That is correct behaviour, and the indexes are still created to teach the production shape ([ADR-0010](0010-vector-search-pgvector.md)).

### Seeding (`Data/DatabaseSeeder.cs`)

Seeding runs in `Program.cs` **before `app.Run()`**, so the API doesn't accept requests until the data is ready. Aspire's `WaitFor` and health check handle ordering.

1. **Schema:** execute `init.sql`. It is idempotent, so running it again is a no-op.
2. **Catalog upsert:** load `products.json` and compute `content_hash` = SHA-256 of the embedded text (name, brand, categories, description, reviews). `INSERT … ON CONFLICT (id) DO UPDATE` updates only rows whose data changed; **if `content_hash` changed, set the embedding columns to NULL.** Products removed from the JSON are deleted.
3. **Embeddings** for the configured provider (`Embeddings:Provider`, [ADR-0009](0009-local-embeddings-onnx-runtime.md)). This step is added in Phase 2, when the Nomic embedder lands.
   - **Provider switch:** rows whose `embedding_model` differs from the active model are cleared first, so vectors from different models are never mixed.
   - **Load from file (normal path, seconds):** read `assets/data/embeddings/{provider}.jsonl` and write the stored vector for every product whose `contentHash` and `model` match.
   - **Fill gaps live:** products missing from the file, or with a stale hash, are embedded with the live model or API in small batches, with a warning suggesting a rebuild.
   - **`Embeddings:Rebuild = true`:** ignore the file, re-embed every product live, then **overwrite the file**. This regenerates the committed file after `products.json` changes. Set it back to `false` afterwards.
   - Log the outcome, e.g. `Embeddings (nomic): 58 loaded from file, 2 embedded live, 0 missing`.
4. If vectors are needed but the provider is unavailable (model files missing, no API key), log a clear warning and leave them NULL. Stages that need embeddings return `503` with fix-it guidance, and Stages 1–2 keep working ([ADR-0003](0003-search-api-contract-and-debug-trace.md)).

### Schema changes during development

There is no migration tool. A breaking schema change means **resetting the data volume**; the README documents the command (`docker volume rm pgvector-data-search`). The seeder then rebuilds everything.

### Legacy removal

Delete `Endpoints/Products/GetProducts`, `Models/Products/*`, `Data/IProductRepository.cs` and `Data/ProductRepository.cs`. Replace `DatabaseManager` with `DatabaseSeeder`.

## Consequences

- Restarts are instant after the first run. Editing one product re-embeds only that product.
- A fresh clone loads committed vectors in seconds. Only a rebuild or a stale product needs the live model or API.
- Committed embedding files must be rebuilt when products change. The hash guard, the seeder warning and a unit test make staleness visible rather than silent.
- Blocking startup is simple and predictable for a demo. A production system would seed from a separate job.
- Generated columns and indexes live in SQL, so learners can read them in one file.

## Alternatives considered

| Option | Why not (for this repo) |
|---|---|
| Drop and re-seed every start (current) | Slow restarts; embeddings recomputed needlessly |
| Always embed live on first run (no committed vectors) | Slower first start, costs hosted-API users money, and results can drift from the rehearsed golden queries |
| Precomputed query embeddings too (no model needed at all) | Over-engineering for this repo's audience: needs a query cache and a restricted UI mode |
| EF Core migrations / DbUp / Flyway | The right choice in production; adds tooling that distracts from search |
| Background seeding while the API serves | Stages would return partial results mid-demo |
| Separate table per embedding model | Cleaner in theory; more joins for no teaching gain at this scale |

## Teaching notes

- Content hashing is a cheap and robust way to keep derived data (embeddings) in sync with source data.
- Indexes are part of search design: GIN for text and JSONB, HNSW for vectors, B-tree for exact filters.
- "Embeddings are a cache of your content": treat them as derived data you can always rebuild. Committing them is fine if every vector records its model and a hash of its source text.

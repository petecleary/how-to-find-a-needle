# ADR-0006: Database schema & seeding

- **Status:** Proposed
- **Date:** 2026-09-13
- **Related:** ADR-0002, ADR-0005, ADR-0008, ADR-0009, ADR-0010, ADR-0012; roadmap Phase 1

## Context

The current `init.sql` mirrors the old Datafiniti CSV, runs `DROP TABLE` on every start, and has no indexes. The connection string is passed around by hand, and there is a legacy `GET /api/products` with its own repository and models.

Embedding ~500 products with two ONNX models takes noticeable time on a laptop. Doing that on every restart would hurt both development and the live demo.

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
        setweight(to_tsvector('english', description), 'C')
    ) STORED,

    -- Stages 3–5: embeddings, NULL until the seeder fills them (ADR-0009, ADR-0012)
    embedding_nomic      VECTOR(768),
    embedding_bge_dense  VECTOR(1024),
    embedding_bge_sparse SPARSEVEC(250002),

    content_hash    TEXT NOT NULL,   -- hash of the text that is embedded
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE INDEX IF NOT EXISTS ix_products_brand     ON products (brand);
CREATE INDEX IF NOT EXISTS ix_products_categories ON products USING GIN (categories);
CREATE INDEX IF NOT EXISTS ix_products_price     ON products (price);
CREATE INDEX IF NOT EXISTS ix_products_specs     ON products USING GIN (specs jsonb_path_ops);
CREATE INDEX IF NOT EXISTS ix_products_search    ON products USING GIN (search_vector);
CREATE INDEX IF NOT EXISTS ix_products_nomic     ON products USING hnsw (embedding_nomic vector_cosine_ops);
CREATE INDEX IF NOT EXISTS ix_products_bge_dense ON products USING hnsw (embedding_bge_dense vector_cosine_ops);
CREATE INDEX IF NOT EXISTS ix_products_bge_sparse ON products USING hnsw (embedding_bge_sparse sparsevec_ip_ops);
```

Notes:
- `reviews` are **not** in `search_vector`. Reviews add semantic colour for embeddings, but they also add lexical noise we don't want in Stage 2. Revisit if GQ-02 or GQ-03 need it.
- `pg_trgm` is **not** installed. Keyword search uses full-text search only ([ADR-0008](0008-keyword-search-bm25-style.md)).
- Keep the `pgvector/pgvector:pg16` image unless Phase 1 finds a reason to move to pg17. Pin the tag in `AppHost.cs`.
- At 60–500 rows the planner may choose a sequential scan over HNSW. That is correct behaviour, and the indexes are still created to teach the production shape ([ADR-0010](0010-vector-search-pgvector.md)).

### Seeding (`Data/DatabaseSeeder.cs`)

Seeding runs in `Program.cs` **before `app.Run()`**, so the API doesn't accept requests until the data is ready. Aspire's `WaitFor` and health check handle ordering.

1. **Schema:** execute `init.sql`. It is idempotent, so running it again is a no-op.
2. **Catalog upsert:** load `products.json` and compute `content_hash` = SHA-256 of the embedded text (name, brand, categories, description, reviews). `INSERT … ON CONFLICT (id) DO UPDATE` updates only rows whose data changed; **if `content_hash` changed, set the embedding columns to NULL.** Products removed from the JSON are deleted.
3. **Embedding backfill:** for each model, select rows where its column `IS NULL`, embed them in small batches, and update. This step is added when each embedder lands (Nomic in Stage 3, BGE-M3 in Stage 5). Log progress (`Embedded 40/60 with nomic-embed-text…`).
4. If a model's files are missing, log a clear warning and skip that backfill. The stages that need the model return `503` with fix-it guidance ([ADR-0003](0003-search-api-contract-and-debug-trace.md)).

### Schema changes during development

There is no migration tool. A breaking schema change means **resetting the data volume**; the README documents the command (`docker volume rm pgvector-data-search`). The seeder then rebuilds everything.

### Legacy removal

Delete `Endpoints/Products/GetProducts`, `Models/Products/*`, `Data/IProductRepository.cs` and `Data/ProductRepository.cs`. Replace `DatabaseManager` with `DatabaseSeeder`.

## Consequences

- Restarts are instant after the first run. Editing one product re-embeds only that product.
- The first run with both models takes about a minute at 500 products; the log shows progress.
- Blocking startup is simple and predictable for a demo. A production system would seed from a separate job.
- Generated columns and indexes live in SQL, so learners can read them in one file.

## Alternatives considered

| Option | Why not (for this repo) |
|---|---|
| Drop and re-seed every start (current) | Slow restarts; embeddings recomputed needlessly |
| Precomputed seed file with vectors committed | Generated artefacts go stale; large diffs; hides the embedding step |
| EF Core migrations / DbUp / Flyway | The right choice in production; adds tooling that distracts from search |
| Background seeding while the API serves | Stages would return partial results mid-demo |
| Separate table per embedding model | Cleaner in theory; more joins for no teaching gain at this scale |

## Teaching notes

- Content hashing is a cheap and robust way to keep derived data (embeddings) in sync with source data.
- Indexes are part of search design: GIN for text and JSONB, HNSW for vectors, B-tree for exact filters.
- "Embeddings are a cache of your content": treat them as derived data you can always rebuild.

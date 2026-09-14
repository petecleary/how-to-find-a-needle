-- Idempotent schema for the product catalog (ADR-0006). Safe to run on every startup:
-- every statement is IF NOT EXISTS / CREATE OR REPLACE, so a second run is a no-op.
CREATE EXTENSION IF NOT EXISTS vector;

-- array_to_string is only STABLE, and a generated column needs an IMMUTABLE expression.
-- This wrapper just re-declares the same function as IMMUTABLE: text[] categories never
-- change meaning between calls, so the promise is safe, and it lets search_vector below
-- include categories alongside name and description.
CREATE OR REPLACE FUNCTION immutable_array_to_string(text[], text)
RETURNS text LANGUAGE sql IMMUTABLE PARALLEL SAFE
AS $$ SELECT array_to_string($1, $2) $$;

CREATE TABLE IF NOT EXISTS products (
    id              TEXT PRIMARY KEY,
    name            TEXT NOT NULL,
    brand           TEXT NOT NULL,
    categories      TEXT[] NOT NULL,   -- SKOS concept notations (ADR-0013), e.g. "laptop-chargers"
    price           NUMERIC(10,2) NOT NULL,
    currency        TEXT NOT NULL,
    description     TEXT NOT NULL,
    reviews         TEXT[] NOT NULL DEFAULT '{}',
    specs           JSONB NOT NULL DEFAULT '{}',

    -- Stage 2: a weighted full-text document (ADR-0008). setweight ranks a name match (A)
    -- above a brand/category match (B) above a description match (C); reviews are left out
    -- deliberately, to keep them from adding lexical noise to keyword search.
    search_vector   TSVECTOR GENERATED ALWAYS AS (
        setweight(to_tsvector('english', name), 'A') ||
        setweight(to_tsvector('english', brand || ' ' || immutable_array_to_string(categories, ' ')), 'B') ||
        setweight(to_tsvector('english', description), 'C')
    ) STORED,

    -- Stages 3, 4, 6: dense embedding from the configured provider (ADR-0009). Nomic's model
    -- and OpenAI's text-embedding-3-small (dimensions: 768) both fit 768 dimensions.
    embedding_dense      VECTOR(768),
    embedding_model      TEXT,              -- e.g. nomic-embed-text-v1.5-int8; models are never mixed
    -- Stage 5, optional and built last (ADR-0012): BGE-M3's 1024-dim dense vector and its
    -- learned sparse vector over a ~250k token vocabulary.
    embedding_bge_dense  VECTOR(1024),
    embedding_bge_sparse SPARSEVEC(250002),

    content_hash    TEXT NOT NULL,   -- SHA-256 of the text that is embedded (ProductDocument.BuildText)
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- B-tree: exact-match and range filters (Stage 1's brand and price filters).
CREATE INDEX IF NOT EXISTS ix_products_brand     ON products (brand);
CREATE INDEX IF NOT EXISTS ix_products_price     ON products (price);
-- GIN: array overlap (categories && @categories) and JSONB containment (specs @> @specs).
CREATE INDEX IF NOT EXISTS ix_products_categories ON products USING GIN (categories);
CREATE INDEX IF NOT EXISTS ix_products_specs     ON products USING GIN (specs jsonb_path_ops);
-- GIN over the generated tsvector: what Stage 2's @@ operator uses.
CREATE INDEX IF NOT EXISTS ix_products_search    ON products USING GIN (search_vector);
-- HNSW: approximate nearest-neighbour search for Stages 3, 4, 6 (cosine distance) and
-- Stage 5's dense and sparse vectors. At 60-500 rows the planner may still choose a
-- sequential scan over the index — that's correct at this size, and the index is created
-- anyway so the trace shows the production shape (ADR-0006).
CREATE INDEX IF NOT EXISTS ix_products_dense     ON products USING hnsw (embedding_dense vector_cosine_ops);
CREATE INDEX IF NOT EXISTS ix_products_bge_dense ON products USING hnsw (embedding_bge_dense vector_cosine_ops);
CREATE INDEX IF NOT EXISTS ix_products_bge_sparse ON products USING hnsw (embedding_bge_sparse sparsevec_ip_ops);

-- 1. Enable pgvector for similarity search
CREATE EXTENSION IF NOT EXISTS vector;

DROP TABLE IF EXISTS products;

-- 2. Create the electronics product table.
--    TODO: replace with the products.json schema once the curated dataset lands.
CREATE TABLE IF NOT EXISTS products (
    id TEXT PRIMARY KEY,
    name TEXT NOT NULL,
    brand TEXT,
    manufacturer TEXT,
    manufacturer_number TEXT,
    categories TEXT,
    primary_categories TEXT,
    price_min NUMERIC(10,2),
    price_max NUMERIC(10,2),
    currency TEXT,
    condition TEXT,
    availability TEXT,
    date_seen TEXT,
    is_sale BOOLEAN,
    merchant TEXT,
    shipping TEXT,
    source_urls TEXT,
    asins TEXT,
    image_urls TEXT,
    keys TEXT,
    ean TEXT,
    upc TEXT,
    weight TEXT,
    source_urls_2 TEXT,
    date_added TEXT,
    date_updated TEXT,
    
    -- Standard Dense Embeddings (e.g., Nomic: 768 dims)
    embedding_nomic vector(768),

    -- BGE-M3 Multilingual Dense Embedding (1024 dims)
    embedding_bge_dense vector(1024),

    -- BGE-M3 Learned Sparse Vector (vocab weights for lexical hybrid)
    -- Requires pgvector >= 0.7.0 for the native sparsevec type
    embedding_bge_sparse sparsevec(250002)
);

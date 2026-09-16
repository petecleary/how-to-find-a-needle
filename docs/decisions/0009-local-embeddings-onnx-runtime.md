# ADR-0009: Embedding providers & committed embedding files

- **Status:** Accepted
- **Area:** Search
- **Related:** [ADR-0006](0006-database-schema-and-seeding.md), [ADR-0010](0010-vector-search-pgvector.md), [ADR-0013](0013-domain-ontology-and-compatibility.md), [ADR-0015](0015-llm-hosting-and-client.md)

## Context

Stages 3 to 7 need dense text embeddings: for every product when the database is seeded, and for the query on every search. Two kinds of user want different things:

- **The presenter** runs everything on a laptop, offline, and wants to show how an embedding model actually works: tokeniser, neural network, pooling, normalisation.
- **Learners** may not want to download a model, but may have an API key for a hosted provider.

Embedding every product on a fresh clone is slow locally and costs money with a hosted API. Vectors that differ from the ones the golden queries were tested with also make results drift.

## Decision

### One abstraction, a local default

- Embeddings go through **`IEmbeddingGenerator<string, Embedding<float>>`** from `Microsoft.Extensions.AI`, the same family as the chat client in [ADR-0015](0015-llm-hosting-and-client.md).
- A thin **`ISearchEmbedder`** wraps it with `EmbedQueryAsync` and `EmbedDocumentsAsync`, so a caller can't forget a provider's details, such as Nomic's prefixes.
- **The default provider is local:** `NomicOnnxEmbeddingGenerator`, our own implementation over ONNX Runtime, using `nomic-embed-text-v1.5` (int8). A hosted OpenAI provider (`text-embedding-3-small`, asked for 768 dimensions) sits behind the same interface; see the README for its setup.
- Every provider produces **768 dimensions**, so there is one `VECTOR(768)` column plus the model's name. **Vectors from different models are never mixed**: switching provider replaces them ([ADR-0006](0006-database-schema-and-seeding.md)).

```json
"Embeddings": { "Provider": "nomic", "Rebuild": false }
```

### How the Nomic model runs

- **Files:** `assets/models/nomic/model_int8.onnx` and `tokenizer.json`, downloaded as described in `assets/models/README.md`.
- **Tokeniser:** BERT WordPiece, uncased, via `Microsoft.ML.Tokenizers`. Accents are stripped when lower-casing, as the model expects ("portátil" becomes "portatil").
- **Task prefixes are required.** Products are embedded as `"search_document: …"` and queries as `"search_query: …"`. Nomic is an *asymmetric* model: questions and documents are embedded slightly differently.
- **Inference:** `input_ids`, `attention_mask` and `token_type_ids` in; `last_hidden_state` out.
- **Pooling:** the mean of the token vectors, counting only real tokens (the attention mask), then **L2 normalisation** so every vector has length 1.
- **Limit:** 512 tokens; longer text is truncated, and the trace says so.
- **One text per inference call** (see *What to take away*).
- **A missing model** raises a clear error with the path and the README link, which the API returns as `503`.
- **The text embedded for a product** is `"{name}. {brand} {categories}. {description} Reviews: {reviews}"`. The same text feeds the content hash.

### Committed embedding files

- `assets/data/embeddings/{provider}.jsonl`: one line per product, with the vector as base64-encoded little-endian float32.

```json
{"id":"PROD-0012","contentHash":"9f2c…","model":"nomic-embed-text-v1.5-int8","dimensions":768,"vector":"AAAgQf…"}
```

- The seeder loads a line when its content hash and model still match the product, embeds anything stale live, and rewrites the file when `Embeddings:Rebuild` is `true` ([ADR-0006](0006-database-schema-and-seeding.md)).
- **Query embeddings are always computed live.** The files make seeding fast and results reproducible; they don't remove the need for the model.
- A unit test checks that every product has a line with a matching hash, so a catalog change without a rebuild fails fast.

## Consequences

- The demo runs fully offline, and a fresh clone seeds in seconds with exactly the vectors that were tested.
- The mechanics of an embedding model are in the repository, readable, even for learners who pick a hosted provider.
- Golden-query expectations are tuned against Nomic. Another provider ranks differently and may not produce every moment the same way.

## Alternatives considered

| Option | Why not (for this repository) |
|---|---|
| Hosted embeddings only | Breaks the offline demo, hides the mechanics, and costs money |
| Ollama's `nomic-embed-text` | Viable, but hides tokenisation, pooling and normalisation, which is what this decision teaches |
| Always embed products on first run | Slower first start, costs hosted users money, and results can drift from the tested ones |
| Batching products for speed | Changes the vectors (below) |
| CSV for vectors | 768 numbers per row are unwieldy; JSON Lines with base64 is compact and still readable |

## What to take away

- **An embedding model is a tokeniser, a neural network, a pooling step and normalisation.** Each one is a place for bugs to hide.
- **Read the model card.** Asymmetric models such as Nomic need query and document prefixes; leave them out and results quietly get worse.
- **Put embeddings behind an interface, and record which model made every vector.** Vectors from different models are not comparable.
- **Batching changed the answer.** With the int8 model, a query embedded alone and in a batch with an identical text agreed exactly (cosine 1.000000). In a batch with a *longer* text, the cosine dropped to 0.989404, even though padding is masked out of attention and pooling. Quantisation measures value ranges over the whole padded batch. The fix is one text per call. An optimisation that changes results is a bug, and only a test that compares numbers finds it.
- **The tokeniser file matters.** `Microsoft.ML.Tokenizers` wanted a `vocab.txt`; the model ships `tokenizer.json`. Reading the vocabulary out of it took a few lines, but a *different* vocabulary would still have run, and produced plausible, wrong vectors.
- **Rebuilding is deterministic.** Regenerating the embeddings file for 300 products left the 60 original products' vectors byte-for-byte identical.

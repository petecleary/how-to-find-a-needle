# ADR-0009: Embedding providers & committed embedding files

- **Status:** Proposed
- **Date:** 2026-09-13
- **Related:** ADR-0006, ADR-0010, ADR-0011, ADR-0012, ADR-0013, ADR-0015; roadmap Phase 2 (Nomic), Phase 5 (OpenAI)

## Context

Stages 3, 4 and 6 need dense text embeddings, for products (at seeding) and for queries (at every search). Two audiences have different needs:

- **The presenter** runs everything locally on an Apple-silicon Mac with 64 GB of memory. The demo must work **offline**, and the talk should show how an embedding model actually works: tokenizer, network, pooling, normalisation.
- **Learners** may not want to download ONNX models, but may have an OpenAI key.

The Nomic Embed Text v1.5 ONNX model (`onnx/model_int8.onnx`) is already on disk, and `Microsoft.ML.OnnxRuntime` and `Microsoft.ML.Tokenizers` are already referenced.

Embedding every product on a fresh clone is slow with local models and costs money with a hosted API. Vectors that differ from the ones the golden queries were tuned against also make results drift from what was rehearsed.

## Decision

### One abstraction, two providers

- Embeddings go through **`IEmbeddingGenerator<string, Embedding<float>>`** from `Microsoft.Extensions.AI`, the same family as `IChatClient` in [ADR-0015](0015-llm-hosting-and-client.md).
- A thin **`ISearchEmbedder`** wrapper exposes `EmbedQueryAsync` and `EmbedDocumentsAsync`, so provider-specific details (such as Nomic's prefixes) can't be forgotten by callers.

| Provider | Implementation | Model id stored | Needs | Status |
|---|---|---|---|---|
| `nomic` (default) | `NomicOnnxEmbeddingGenerator`, our own `IEmbeddingGenerator` over ONNX Runtime | `nomic-embed-text-v1.5-int8` | model files per `assets/models/README.md` | Built in Phase 2 |
| `openai` | `Microsoft.Extensions.AI.OpenAI` embedding generator, `text-embedding-3-small` with **`dimensions: 768`** | `openai-text-embedding-3-small-768` | API key in user secrets | Wired to the interface; **built and tested later** (no credits during the main build) |

Configuration (`appsettings.json`; secrets via `dotnet user-secrets`):

```json
"Embeddings": {
  "Provider": "nomic",
  "Rebuild": false,
  "OpenAI": { "Model": "text-embedding-3-small", "Dimensions": 768 }
}
```

- Both providers produce **768 dimensions**, so the schema has one `embedding_dense VECTOR(768)` column plus `embedding_model` ([ADR-0006](0006-database-schema-and-seeding.md)).
- **Vectors from different models are never mixed.** Rows record their model, and switching provider replaces them.

### Committed product embedding files

- **Location:** `assets/data/embeddings/{provider}.jsonl`, i.e. `nomic.jsonl`, then `openai.jsonl` when built (and `bge-m3.jsonl` if Stage 5 is built, [ADR-0012](0012-bge-m3-dense-and-sparse.md)).
- **Format:** one line per product, with the vector as little-endian float32, base64-encoded. That's about 1 MB per file at 500 products, and the metadata stays readable.

```json
{"id":"PROD-0012","contentHash":"9f2c…","model":"nomic-embed-text-v1.5-int8","dimensions":768,"vector":"AAAgQf…"}
```

- The **seeder** loads vectors whose `contentHash` and `model` match. It embeds stale or missing products live, and **`Embeddings:Rebuild = true`** re-embeds everything and overwrites the file. The full behaviour is in [ADR-0006](0006-database-schema-and-seeding.md).
- **Query embeddings are always computed live** by the same provider. The files speed up seeding and make results match the rehearsed ones; they don't remove the need for the model or the key. If the provider is unavailable, stages that embed return `503` with guidance, and Stages 1–2 keep working.

### `NomicOnnxEmbeddingGenerator` (singleton)

- **Model:** `assets/models/nomic/model_int8.onnx` (nomic-embed-text-v1.5, 768 dimensions).
- **Tokenizer:** BERT WordPiece (uncased), via `Microsoft.ML.Tokenizers`.
  - **Verify in Phase 2** whether the library loads the model's `tokenizer.json` directly, or needs `vocab.txt` (`BertTokenizer.Create`).
  - Update the models README with whichever file is needed.
- **Task prefixes (required):** `ISearchEmbedder` applies `"search_document: "` to products and `"search_query: "` to queries. The OpenAI provider uses no prefixes.
- **Inference:**
  - Inputs: `input_ids`, `attention_mask`, `token_type_ids` (confirm the names from the model metadata at startup and log them).
  - Output: `last_hidden_state`.
  - Pooling: **mean pooling** over tokens using the attention mask, then **L2 normalisation**.
- **Limits:** truncate at 512 tokens. Batch size 16 for seeding; single queries at request time.
- **Session:** one `InferenceSession`, created lazily and reused (thread-safe for `Run`); CPU with default graph optimisations.
- **Missing model:** a clear exception with the path and a link to `assets/models/README.md`, mapped to `503` ([ADR-0003](0003-search-api-contract-and-debug-trace.md)).

### Shared rules

- **Document text for embedding:** `"{name}. {brand} {categories}. {description} Reviews: {reviews joined}"`. This is the same text that feeds `content_hash` ([ADR-0006](0006-database-schema-and-seeding.md)).
- **Trace:** provider, model id, prefix used (Nomic), token count and truncation (Nomic), duration, and the first 8 dimensions of the query vector (for illustration only).

### Tests

- **Unit:**
  - Mean pooling and L2 normalisation on hand-made tensors; prefixes applied per provider.
  - Base64 float32 round-trip.
  - **Embedding file consistency:** every product in `products.json` has a line in `nomic.jsonl` with a matching `contentHash`. This fails CI when products change without a rebuild.
- Tokenizer round-trip on a known sentence (skipped if the model files are missing).
- **Golden-query expectations are tuned against Nomic.** The OpenAI provider may need its own expectation tweaks when it's tested in Phase 5.

## Consequences

- The presenter runs fully offline. A learner with an OpenAI key can run the demo with no model download (once the OpenAI path is tested).
- Fresh clones seed in seconds from committed vectors, and results match the rehearsed ones.
- Committed files must be rebuilt when products change. The hash guard, the seeder warning and the unit test make staleness loud rather than silent.
- The embedding mechanics lesson stays in the repo through the Nomic implementation, even for learners who choose OpenAI.

## Alternatives considered

| Option | Why not (for this repo) |
|---|---|
| Nomic only | Every learner must download models; no hosted option |
| OpenAI only | Breaks the offline demo, hides the mechanics, and needs credits |
| Ollama embeddings (`nomic-embed-text`) | Viable, but hides tokenisation, pooling and normalisation, which is what this ADR teaches |
| Always embed products live on first run | Slower first start, costs OpenAI users money, and results may drift from rehearsal |
| Precomputed query embeddings too (no model needed at all) | Over-engineering for this repo's audience: needs a query cache and a restricted UI mode |
| Separate columns per provider (side-by-side comparison) | An interesting demo, but scope creep for a 4-week build |
| CSV for vectors | 768 floats per row make CSV unwieldy; JSON Lines with base64 is compact and readable |

## Teaching notes

- An embedding model is a tokenizer + a neural network + a pooling step + normalisation. Each is a place bugs hide.
- Read the model card: asymmetric models like Nomic need query/document prefixes.
- Put embeddings behind an interface, like chat. Then record *which model* made every vector, because vectors from different models aren't comparable.
- Embeddings are derived data. Committing them is fine if each vector carries its model and a hash of its source text.

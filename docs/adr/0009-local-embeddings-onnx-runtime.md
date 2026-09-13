# ADR-0009: Local embeddings with ONNX Runtime

- **Status:** Proposed
- **Date:** 2026-09-13
- **Related:** ADR-0006, ADR-0010, ADR-0012; roadmap Phase 2

## Context

Stages 3–5 need text embeddings. The talk promises **no external API calls**: the demo must work offline on a presenter laptop, and learners shouldn't need API keys. The ONNX model for Nomic Embed Text v1.5 (`onnx/model_int8.onnx`) is already on disk, and `Microsoft.ML.OnnxRuntime` and `Microsoft.ML.Tokenizers` are already referenced.

Embedding models have usage details that are easy to get wrong and quietly degrade results. Nomic's task prefixes are the main example.

## Decision

### `INomicEmbedder` (singleton)

- **Model:** `assets/models/nomic/model_int8.onnx` (nomic-embed-text-v1.5, 768 dimensions).
- **Tokenizer:** BERT WordPiece (uncased), via `Microsoft.ML.Tokenizers`.
  - **Verify in Phase 2** whether the library loads the model's `tokenizer.json` directly, or needs `vocab.txt` (`BertTokenizer.Create`).
  - Update the models README with whichever file is needed.
- **Task prefixes (required):**
  - Documents are embedded as `"search_document: " + text`.
  - Queries are embedded as `"search_query: " + text`.
  - The prefix is a parameter of the API (`EmbedDocumentAsync` / `EmbedQueryAsync`), so callers can't forget it.
- **Inference:**
  - Inputs: `input_ids`, `attention_mask`, `token_type_ids` (confirm the names from the model metadata at startup and log them).
  - Output: `last_hidden_state`.
  - Pooling: **mean pooling** over tokens using the attention mask, then **L2 normalisation**.
  - The full 768 dimensions are used; no Matryoshka truncation.
- **Limits:** truncate input at 512 tokens (product text is short). Batch size 16 for seeding; single queries at request time.
- **Session:** one `InferenceSession`, created lazily on first use and reused. `SessionOptions` use CPU with default graph optimisations. The session is thread-safe for `Run`.
- **Document text for embedding:** `"{name}. {brand} {categories}. {description} Reviews: {reviews joined}"`. This is the same text that feeds `content_hash` ([ADR-0006](0006-database-schema-and-seeding.md)).
- **Missing model:** a clear exception with the path and a link to `assets/models/README.md`. The endpoint maps it to `503` ([ADR-0003](0003-search-api-contract-and-debug-trace.md)).
- **Trace:** model name, file, prefix used, token count (and whether it was truncated), embedding time, and the first 8 dimensions of the query vector (for illustration only).

### Unit tests

- Mean pooling and L2 normalisation on small hand-made tensors.
- Prefixes are applied correctly.
- Tokenizer round-trip on a known sentence (skipped if the model files are missing).

## Consequences

- Fully offline, and deterministic for a given model file.
- CPU int8 inference is fast enough for ~500 short documents (seconds to low minutes) and for interactive queries (milliseconds).
- Tokenizer and model-output details are version-sensitive. Logging the model metadata at startup makes mismatches obvious.

## Alternatives considered

| Option | Why not (for this repo) |
|---|---|
| Hosted embedding API (OpenAI, Voyage, etc.) | Breaks offline and "no keys" promises |
| Ollama embeddings (`nomic-embed-text`) | Viable and simpler, but hides tokenisation, pooling and normalisation, which is what this stage teaches |
| `SmartComponents.LocalEmbeddings` / other wrappers | Hide the mechanics; less control over prefixes and pooling |
| Full-precision `model.onnx` | ~4× larger for negligible demo quality gain |

## Teaching notes

- An embedding model is a tokenizer + a neural network + a pooling step + normalisation. Each is a place bugs hide.
- Read the model card: asymmetric models like Nomic need query/document prefixes.
- Normalised vectors make cosine similarity equal to the dot product. That is why normalisation matters.

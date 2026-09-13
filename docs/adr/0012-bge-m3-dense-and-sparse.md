# ADR-0012: Stage 5 — BGE-M3 dense + sparse

- **Status:** Proposed
- **Date:** 2026-09-13
- **Related:** ADR-0006, ADR-0009, ADR-0011; golden query GQ-07; roadmap Phase 6 (optional, build last)

## Context

Nomic embeddings are English-centric, and Postgres FTS stems English only. A Spanish query for a USB-C charger fails in Stages 2–4.

[BGE-M3](https://huggingface.co/BAAI/bge-m3) is multilingual and multi-function. One model produces a **dense** vector (1024 dimensions) and **learned sparse** weights per token, so a single model gives us hybrid search. The ONNX int8 export we plan to use is [gpahal/bge-m3-onnx-int8](https://huggingface.co/gpahal/bge-m3-onnx-int8) (`model_quantized.onnx`, `tokenizer.json`, `sentencepiece.bpe.model`).

**Prior art:** Pete has run BGE-M3 in memory in .NET before, so this is a known path. The tokenizer (XLM-RoBERTa SentencePiece, 250,002 tokens) and the output tensors of this particular ONNX export still get a quick check before building on them.

## Decision

### Step 0: verification (first task of Stage 5, about an hour; reuse the earlier in-memory BGE-M3 approach where possible)

Prove, in a scratch console app or unit test:
1. **Tokenisation.** Load the tokenizer with `Microsoft.ML.Tokenizers` (`SentencePieceTokenizer` from `sentencepiece.bpe.model`, or `tokenizer.json`). The token IDs for 3 sample sentences (English, Spanish, German) must match the Python `transformers` reference. Record the reference IDs in the test.
2. **Outputs.** Inspect the ONNX output names and shapes. Confirm which output holds the dense (CLS, normalised) vector and which holds the sparse token weights, or whether sparse weights need computing from hidden states.
3. **Dense similarity.** The cosine between an English and a Spanish charger sentence should be clearly higher than between the charger and an unrelated product.

**Outcome rules:**
- All pass → proceed with the design below.
- Tokenizer fails → implement XLM-R SentencePiece pre-processing to match the reference, or record a superseding decision to use a precompute step.
- Update this ADR with the result before continuing.

### Design (assuming the verification passes)

- **`IBgeM3Embedder` (singleton):** returns `(float[] Dense, IReadOnlyDictionary<int, float> Sparse)`.
  - **Dense:** CLS vector, L2-normalised.
  - **Sparse:** for each token, ReLU weight from the model's sparse head. **Keep the maximum weight per token ID**, drop special tokens (`<s>`, `</s>`, `<pad>`, `<unk>`) and zero weights. This matches FlagEmbedding's `lexical_weights`.
  - **No task prefixes.** Unlike Nomic, BGE-M3 doesn't need them.
- **Storage:**
  - `embedding_bge_dense VECTOR(1024)`.
  - `embedding_bge_sparse SPARSEVEC(250002)`, built with `Pgvector.SparseVector`. Watch the **index base**: pgvector's text format is 1-based, while the .NET `SparseVector` API takes 0-based indices. Unit-test the conversion.
  - The HNSW sparse index supports up to 1,000 non-zero elements, which is ample for product text.
- **Retrieval:** two queries, both with the shared filters and depth:
  - Dense: `ORDER BY embedding_bge_dense <=> @dense`.
  - Sparse: `ORDER BY embedding_bge_sparse <#> @sparse`. `<#>` returns the **negative** inner product, so ascending order is correct; the trace shows `score = −value`.
- **Fusion:** reuse `IRankFusion` ([ADR-0011](0011-hybrid-search-rrf.md)) over the dense and sparse lists (k = 60, equal weights). Stage 5 is "hybrid inside one model".
- **Trace:**
  - Tokens for the query, including sub-word pieces, which show how multilingual tokenisation works.
  - The top sparse weights as `token → weight`, which makes learned sparse retrieval visible.
  - Dense distances, per-item RRF formula strings, and model name and timing.
- **Seeder:** loads `assets/data/embeddings/bge-m3.jsonl` (dense vector plus sparse `token → weight` pairs) using the same hash and `Rebuild` rules as [ADR-0009](0009-local-embeddings-onnx-runtime.md). It embeds missing products in batches of 8, since this is a larger model than Nomic.
- **Cross-language golden query (GQ-07):** the language is decided in Phase 2, where Stage 6 covers it through multilingual ontology labels (Spanish proposed). With BGE-M3, a full-sentence variant should rank the correct chargers in its top 5 while Stages 2–4 don't.

## Consequences

- Shows multilingual retrieval and learned sparse vectors with no extra infrastructure.
- It is the heaviest model (int8 is still hundreds of MB), so first-run seeding is slower. The models README must set expectations.
- If the verification shows this export behaves differently from the earlier approach, update this ADR before continuing.

## Alternatives considered

| Option | Why not (for this repo) |
|---|---|
| Multilingual E5 dense only | No sparse output; loses the "learned sparse" lesson |
| SPLADE for sparse + Nomic for dense | Two models, English-only SPLADE; more moving parts |
| ColBERT multi-vector (BGE-M3's third mode) | Needs multi-vector storage and late interaction; too much for a 30-minute talk |
| Run BGE-M3 via a Python sidecar | Adds a language runtime; breaks the ".NET all the way" simplicity |
| Translate queries with an LLM first | Hides retrieval; adds latency and a dependency on Stage 7 infrastructure |

## Teaching notes

- "Sparse" doesn't have to mean BM25. Learned sparse vectors weight *tokens by importance in context*, and they work across languages.
- Tokenisation is part of the model. Get it wrong and every downstream number is wrong without any error.

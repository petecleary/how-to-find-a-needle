# ADR-0012: BGE-M3 dense + sparse (rejected)

- **Status:** Rejected (2026-09-14) by [ADR-0018](0018-scope-and-going-further.md)
- **Date:** 2026-09-13
- **Related:** ADR-0009, ADR-0011, ADR-0013, ADR-0018; golden query GQ-07

## Why this was rejected

This ADR proposed an optional, build-last stage that ran BGE-M3 for multilingual dense and learned sparse retrieval. We decided not to build it:

- **It overlaps Stage 4.** Dense + sparse fused with RRF is hybrid search inside one model. The talk already teaches hybrid search with keyword + vector.
- **Its unique lessons can be told without the code.** Learned sparse weights and multilingual vectors fit the talk's going-further step, next to chunking and the wider vector landscape. Building them would add a second embedding model, 1024-dimension and sparse columns, a seeder path and a UI tab.
- **GQ-07's moment is already covered.** Stage 5 matches the Spanish ontology label ("cargador" → *Chargers*) and expands it into English terms ([ADR-0013](0013-domain-ontology-and-compatibility.md)).
- **The time is better spent on Stage 7.** The pedagogy stage and its baseline comparison carry the thesis ([ADR-0017](0017-pedagogy-engine.md)).

Nothing below is built. No code, schema, route or stage slug refers to it. The original proposal is kept as the record of what was considered, and as a starting point for anyone who wants to build it.

## Teaching notes (for the going-further step)

- "Sparse" doesn't have to mean BM25. Learned sparse vectors weight *tokens by importance in context*, and they work across languages.
- One model can produce dense and sparse vectors in a single pass, so hybrid search doesn't have to mean two systems.
- Multilingual embeddings handle whole sentences in other languages; ontology labels handle only the words someone has labelled.
- Tokenisation is part of the model. Get it wrong and every downstream number is wrong without any error.

---

## Original proposal (not built)

### Context

Nomic embeddings are English-centric, and Postgres FTS stems English only. A Spanish query for a USB-C charger fails in Stages 2–4.

[BGE-M3](https://huggingface.co/BAAI/bge-m3) is multilingual and multi-function. One model produces a **dense** vector (1024 dimensions) and **learned sparse** weights per token, so a single model gives us hybrid search. The ONNX int8 export considered was [gpahal/bge-m3-onnx-int8](https://huggingface.co/gpahal/bge-m3-onnx-int8) (`model_quantized.onnx`, `tokenizer.json`, `sentencepiece.bpe.model`).

**Prior art:** Pete has run BGE-M3 in memory in .NET before, so this is a known path. The tokenizer (XLM-RoBERTa SentencePiece, 250,002 tokens) and the output tensors of this particular ONNX export would still need a quick check before building on them.

### Proposed decision

**Step 0: verification** (about an hour), in a scratch console app or unit test:
1. **Tokenisation.** Load the tokenizer with `Microsoft.ML.Tokenizers` (`SentencePieceTokenizer` from `sentencepiece.bpe.model`, or `tokenizer.json`). The token IDs for 3 sample sentences (English, Spanish, German) must match the Python `transformers` reference.
2. **Outputs.** Inspect the ONNX output names and shapes. Confirm which output holds the dense (CLS, normalised) vector and which holds the sparse token weights, or whether sparse weights need computing from hidden states.
3. **Dense similarity.** The cosine between an English and a Spanish charger sentence should be clearly higher than between the charger and an unrelated product.

**Design:**
- **`IBgeM3Embedder` (singleton):** returns `(float[] Dense, IReadOnlyDictionary<int, float> Sparse)`.
  - **Dense:** CLS vector, L2-normalised.
  - **Sparse:** for each token, ReLU weight from the model's sparse head. Keep the maximum weight per token ID, drop special tokens and zero weights. This matches FlagEmbedding's `lexical_weights`.
  - **No task prefixes.** Unlike Nomic, BGE-M3 doesn't need them.
- **Storage:** `embedding_bge_dense VECTOR(1024)` and `embedding_bge_sparse SPARSEVEC(250002)`, built with `Pgvector.SparseVector`. pgvector's text format is 1-based, while the .NET `SparseVector` API takes 0-based indices. The HNSW sparse index supports up to 1,000 non-zero elements.
- **Retrieval:** dense `ORDER BY embedding_bge_dense <=> @dense`; sparse `ORDER BY embedding_bge_sparse <#> @sparse` (`<#>` returns the **negative** inner product, so ascending order is correct).
- **Fusion:** `IRankFusion` ([ADR-0011](0011-hybrid-search-rrf.md)) over the dense and sparse lists (k = 60, equal weights).
- **Trace:** query tokens including sub-word pieces; top sparse weights as `token → weight`; dense distances, RRF formula strings, model name and timing.
- **Seeder:** `assets/data/embeddings/bge-m3.jsonl` with the same hash and `Rebuild` rules as [ADR-0009](0009-local-embeddings-onnx-runtime.md), embedding missing products in batches of 8.
- **GQ-07:** a full-sentence variant should rank the correct chargers in the top 5 while Stages 2–4 don't.

### Consequences (as proposed)

- Would show multilingual retrieval and learned sparse vectors with no extra infrastructure.
- The heaviest model (int8 is still hundreds of MB), so first-run seeding would be slower.

### Alternatives considered (as proposed)

| Option | Why not (for this repo) |
|---|---|
| Multilingual E5 dense only | No sparse output; loses the "learned sparse" lesson |
| SPLADE for sparse + Nomic for dense | Two models, English-only SPLADE; more moving parts |
| ColBERT multi-vector (BGE-M3's third mode) | Needs multi-vector storage and late interaction; too much for a 30-minute talk |
| Run BGE-M3 via a Python sidecar | Adds a language runtime; breaks the ".NET all the way" simplicity |
| Translate queries with an LLM first | Hides retrieval; adds latency and a dependency on the LLM stages |

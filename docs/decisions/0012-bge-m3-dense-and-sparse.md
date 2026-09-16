# ADR-0012: BGE-M3 dense + sparse (not built)

- **Status:** Rejected
- **Area:** Search
- **Related:** [ADR-0009](0009-local-embeddings-onnx-runtime.md), [ADR-0011](0011-hybrid-search-rrf.md), [ADR-0013](0013-domain-ontology-and-compatibility.md), [ADR-0018](0018-scope-and-going-further.md); golden query GQ-07

## Context

The Nomic embedding model is English-centric, and Postgres full-text search stems English only. A Spanish query for a USB-C charger fails in Stages 2–4.

[BGE-M3](https://huggingface.co/BAAI/bge-m3) is multilingual and multi-function: one model produces a **dense** vector (1,024 dimensions) and **learned sparse** weights for each token. A single model can do hybrid search on its own. It was proposed as an extra stage and considered carefully before being rejected.

## Decision

**Not built.** No code, schema, route or stage refers to it. The reasons:

- **It overlaps Stage 4.** Dense and sparse vectors fused with RRF *is* hybrid search, inside one model. The talk already teaches hybrid search with keyword and vector.
- **Its unique lessons can be told without the code.** Learned sparse weights and multilingual vectors fit the talk's going-further step. Building them would have added a second embedding model, 1,024-dimension and sparse columns, another seeding path and another UI tab.
- **GQ-07's multilingual moment is already covered.** Stage 5 matches the Spanish ontology label "cargador" to *Chargers* and expands it into English terms ([ADR-0013](0013-domain-ontology-and-compatibility.md)).
- **The time was better spent on Stage 7**, whose baseline comparison carries the talk's thesis ([ADR-0017](0017-pedagogy-engine.md)).

### What the design would have been

Kept as a starting point for anyone who wants to build it:

- **Check first:** the tokeniser (XLM-RoBERTa SentencePiece, 250,002 tokens) must produce the same token IDs as the Python reference for sample English, Spanish and German sentences, and the ONNX export's outputs must be identified before any code relies on them.
- **Dense:** the `[CLS]` vector, L2-normalised. **Sparse:** a ReLU weight per token from the model's sparse head, keeping the maximum per token ID and dropping special tokens. No task prefixes.
- **Storage:** `VECTOR(1024)` and `SPARSEVEC(250002)`. pgvector's sparse text format is 1-based while the .NET `SparseVector` API is 0-based, and the HNSW sparse index supports up to 1,000 non-zero elements.
- **Retrieval:** dense with `<=>`; sparse with `<#>`, which returns the *negative* inner product, so ascending order is correct. Fuse the two lists with the existing RRF.

## Consequences

- The repository has one embedding model, and the stepper shows seven stages with no gap.
- Learners who want BGE-M3 code won't find it here; the design above is where to start.

## Alternatives considered

| Option | Why not (for this repository) |
|---|---|
| Build it as an optional, last stage | A conditional tab and a second model in the docs, for lessons the talk can tell in a sentence |
| Multilingual E5 (dense only) | No sparse output, so it loses the learned-sparse lesson too |
| SPLADE for sparse plus Nomic for dense | Two models, and SPLADE is English-only |
| ColBERT multi-vector (BGE-M3's third mode) | Needs multi-vector storage and late interaction; too much for a 30-minute talk |
| Translate queries with an LLM first | Hides retrieval behind a model call, adds latency and couples search to the LLM stages |

## What to take away

- **"Sparse" doesn't have to mean BM25.** Learned sparse vectors weight tokens by importance in context, and work across languages.
- One model can produce dense and sparse vectors in a single pass, so hybrid search doesn't have to mean two systems.
- **Multilingual embeddings handle whole sentences** in other languages; ontology labels handle only the words someone has labelled.
- **Tokenisation is part of the model.** Get it wrong and every number downstream is wrong, with no error.
- Scope is a design decision. Rejecting a stage you could build is sometimes what keeps a pipeline teachable.

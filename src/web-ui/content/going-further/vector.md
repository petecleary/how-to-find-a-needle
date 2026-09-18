Stage 3 makes three choices and hides them well: which model, what gets embedded, and where the vectors live. Each is worth a decision of its own.

### Choosing an embedding model

An [embedding](term:embedding) model is not interchangeable with the next one. Its **dimension** sets your storage and index cost. Its **context window** sets how much text one vector may represent. Its **training languages** decide whether a Spanish question can find an English product — ours is English-centric, which is why GQ-07 needs ontology labels rather than the model ([ADR-0010 · Vector search](adr:0010-vector-search-pgvector)).

Changing model means re-embedding everything, so it is a migration, not a setting. The public leaderboards rank models on benchmarks that are not your catalogue; your [golden queries](term:golden-query) are.

### Chunking

We embed one short product row, so there is nothing to split. Manuals, PDFs, policies and transcripts are the opposite problem, and **how you split them decides what can be found** ([chunking](term:chunking)).

- **Fixed-size** windows of _n_ tokens: simple, and it cuts sentences in half.
- **Sliding window** with an overlap: the cut costs less, and near-duplicates crowd the results.
- **Structure-aware**: split on headings, sections or list items, so a chunk is a unit someone wrote deliberately.
- **Layout-aware**: use the PDF's own columns, tables and captions, which is where most real documents fail.

A chunk is also what gets shown as a citation, so it is an interface decision as much as a retrieval one.

### One model, dense and sparse

BM25 and embeddings are usually two systems fused together. [Learned sparse](term:learned-sparse) models produce a sparse, keyword-like vector where the weights are _learned_, so a token can score words it never literally contained. **BGE-M3** emits [dense and sparse](term:dense-sparse) vectors in a single pass and handles full sentences across languages.

We don't build it: it overlaps Stage 4, and the lesson fits in a paragraph ([ADR-0012 · BGE-M3](adr:0012-bge-m3-dense-and-sparse)). The lesson is that "sparse" needn't mean BM25, and that hybrid search needn't mean two systems.

### The vector landscape

[pgvector](term:pgvector) keeps vectors beside the rows they describe, which is why filters and joins stay easy. Past a few million vectors, or with high write rates, dedicated [vector databases](term:vector-database) start to earn their place.

Wherever they live, the index is the trade. [HNSW](term:hnsw) is approximate: `ef_search` buys recall with latency, and the results you never see are the ones you cannot measure. **Filtering is the hard part at scale** — filter before the search and the index can't help; filter after and you may have too few rows left to return.

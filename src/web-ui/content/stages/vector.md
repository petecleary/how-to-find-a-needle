## What it is

Matching meaning. A local model turns the query and every product into an [embedding](term:embedding), a list of 768 numbers, and [pgvector](term:pgvector) returns the products whose vectors point the same way as the query's.

## How it works

1. **Embed.** Nomic Embed runs on your machine with [ONNX](term:onnx) Runtime: tokenise, run the network, average, normalise. The query gets the prefix `search_query: `.
2. **Filter.** Any filters run in the same SQL statement, before the ordering.
3. **Order.** By [cosine distance](term:cosine-distance), nearest first, using an [HNSW](term:hnsw) index.
4. **Score.** `1 − distance`, the cosine similarity.

## What to look for

**GQ-03**: the 45W barrel charger is in the top 5. It reads exactly like the right answer, but it won't fit the laptop. Similarity is not compatibility.

## Strength

It finds synonyms and paraphrases. **GQ-02**, "power brick for laptop", finds the laptop chargers that keyword search missed.

## Failure mode

It has no idea what fits, so a [near miss](term:near-miss) ranks as well as the right product. A device name in the query pulls the device itself up (**GQ-08**).

## Try this

Type nonsense, such as "purple elephant", and press Enter. You still get results: [nearest-neighbour search](term:nearest-neighbour) has no threshold.

## Read the decision

[ADR-0010 · Vector search (pgvector)](adr:0010-vector-search-pgvector) and [ADR-0009 · Embedding providers](adr:0009-local-embeddings-onnx-runtime)

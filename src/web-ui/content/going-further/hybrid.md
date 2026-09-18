[RRF](term:rrf) fuses two rankings without either model ever reading the query and a product together. That is its strength, and it is also where the next technique starts.

### Re-ranking with a cross-encoder

Retrieval compares two vectors made separately, so nothing ever judged this query _against this product_. A [cross-encoder](term:cross-encoder) does exactly that: query and document go into the model together and one relevance score comes out. It is far more accurate and far too slow to run over a catalogue — so you run it over the top 50 that retrieval already found. That is [re-ranking](term:re-ranking).

**Late interaction** ([ColBERT](term:colbert)) sits between the two: it embeds each token separately and matches token against token at query time, keeping some of a cross-encoder's precision at closer to a bi-encoder's cost.

Look at GQ-04 in Results. Fusion puts the right drill battery above the cordless phone battery, but the trap stays fourth, because it is genuinely first in the keyword list. A re-ranker reading query and product together is what pushes it down ([ADR-0011 · Hybrid search with RRF](adr:0011-hybrid-search-rrf)).

### Normalising scores instead of fusing ranks

RRF throws the scores away and keeps only the ranks, which is why it needs no tuning. The alternative is **score normalisation**: map each retriever's scores onto a common range (min-max, z-score, or a model calibrated on labelled data) and take a weighted sum. It keeps the information that a first-place hit was _far_ ahead rather than barely ahead — and it needs recalibrating whenever a model, a catalogue or a query mix changes. RRF is the version that still works next year.

Even within RRF there are dials: the weight `wᵢ` on each list, and `k`. At `k = 60`, `1/(60+1)` and `1/(60+2)` differ by 0.00027 — being first rather than second barely matters, and appearing in both lists matters a great deal. A smaller `k` sharpens the top of each list; a larger one flattens it.

### Off the shelf

Elasticsearch, OpenSearch, Vespa, Azure AI Search, Weaviate and Qdrant all ship hybrid retrieval, usually with RRF, often with a re-ranking stage built in. Two things are worth knowing before adopting one: **which fusion it uses and whether you can see the numbers**, and whether its "BM25" is BM25. Everything in this stage is a hundred lines of SQL, which is the point — you can build it, or buy it, but either way you should be able to explain what it did.

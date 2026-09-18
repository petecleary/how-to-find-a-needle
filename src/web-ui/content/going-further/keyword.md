Stage 2 is lexical search at its simplest: one `tsvector` column, one ranking function, one language. Every part of that has somewhere further to go.

### Real BM25

We call `ts_rank_cd` ["BM25-style"](term:ts-rank-cd) because it isn't BM25. [BM25](term:bm25) has two ideas Postgres leaves out. [Inverse document frequency](term:idf) makes a rare word worth more than a common one, so "barrel" outweighs "charger" in a catalogue full of chargers. **Term-frequency saturation** stops the tenth mention of a word counting as much as the first, tuned by `k1`, with `b` controlling how hard a long document is penalised for its length.

Lucene, Elasticsearch, OpenSearch and Solr give you all of it, with the parameters exposed. **Check what your "BM25" actually is:** several databases ship a different formula under the familiar name, and the only way to know is to read the documentation for the one you have ([ADR-0008 · Keyword search](adr:0008-keyword-search-bm25-style)).

### What the other stores do

- **SQL Server** has full-text indexing with `CONTAINS` and `FREETEXT`, and its own ranking.
- **MongoDB** has a `$text` index with a simple score; Atlas Search puts Lucene underneath it.
- **SQLite** has FTS5, which does implement BM25.
- **`LIKE` and regular expressions** match characters, not words. `LIKE '%charg%'` finds "charger" and "recharging" and "supercharged", never [stems](term:stemming) "charging" to "charg" on purpose, can't rank, and scans the table. It is not search; it is filtering that looks like search.

### Analysers, and the language problem

Postgres's `english` configuration is an **analyser**: it lower-cases, splits into [lexemes](term:lexeme), drops [stop words](term:stop-word) and stems. Each of those is a choice, and each is language-specific. A Spanish query stemmed as English finds nothing; a German compound noun needs a decomposer; Chinese needs a tokeniser before any of it can start. Production systems detect the language first, then pick the analyser — or index the same text several times, once per language.

The other direction is to stop stemming words at all and let a model decide which tokens matter. That is Stage 3, and [learned sparse retrieval](term:learned-sparse) in its going-further tab.

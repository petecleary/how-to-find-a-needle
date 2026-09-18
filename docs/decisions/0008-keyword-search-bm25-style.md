# ADR-0008: Stage 2 — Keyword search (BM25-style)

- **Status:** Accepted
- **Area:** Search
- **Related:** [ADR-0004](0004-pipeline-composition.md), [ADR-0006](0006-database-schema-and-seeding.md), [ADR-0011](0011-hybrid-search-rrf.md), [ADR-0013](0013-domain-ontology-and-compatibility.md); golden queries GQ-02, GQ-03, GQ-04, GQ-07

## Context

Lexical search is the workhorse of search systems. People often call it "BM25", but PostgreSQL's built-in ranking functions are **not** BM25, and a teaching repository has to be accurate about that.

We want one clear implementation that runs with no extra infrastructure, and a map of the options learners will meet elsewhere.

## Decision

### PostgreSQL full-text search, labelled "BM25-style"

```sql
SELECT id, name, brand, categories, price, specs,
       ts_rank_cd(search_vector, q) AS score
FROM products, websearch_to_tsquery('english', @query) AS q
WHERE search_vector @@ q
  -- + the shared filters (ADR-0007)
ORDER BY score DESC, id
LIMIT @candidateDepth;
```

- **Document:** the generated `search_vector` column, weighted A (name), B (brand and categories), C (description) and D (reviews) ([ADR-0006](0006-database-schema-and-seeding.md)).
- **Query parsing:** `websearch_to_tsquery` accepts natural input (quotes, `or`, `-exclude`) and never throws on odd syntax. The English configuration stems words and drops stop words: "batteries" becomes `'batteri'`, and "for" and "my" disappear.
- **Matching:** `@@` requires **every** term. That strictness is part of the lesson (GQ-02).
- **Ranking:** `ts_rank_cd`, cover density: it rewards query terms that appear often and close together, weighted by field.
- **A hook for Stage 5:** the service also accepts synonym groups from the ontology. Each group becomes an OR of phrases (`phraseto_tsquery('power brick') || phraseto_tsquery('power adapter') || …`), AND-ed with the rest of the query. The trace shows the final `tsquery`.
- **Trace:** the SQL, the parsed `tsquery` (so stemming is visible), and each result's matched lexemes and score.

### Why "BM25-style", not BM25

| BM25 ingredient | Postgres `ts_rank_cd` |
|---|---|
| Term frequency with **saturation** (k1) | Frequency and proximity counted, no saturation curve |
| **Inverse document frequency**: rare terms count for more | ❌ No corpus statistics; every term weighs the same |
| **Document length normalisation** (b) | Optional normalisation flags, not BM25's formula |
| Field weights | ✅ `setweight` A/B/C/D |

### Options you'll meet elsewhere

| Option | What it is | When you'd choose it |
|---|---|---|
| `LIKE` / `ILIKE` / regex | Substring or pattern match | Exact codes such as SKUs; no ranking, no stemming, usually no index |
| `pg_trgm` | Trigram similarity | Typo tolerance and fuzzy names; complements full-text search |
| ParadeDB `pg_search` | Real BM25 inside Postgres | True BM25 without leaving Postgres |
| SQL Server full-text | `CONTAINS`, `FREETEXTTABLE` | You're on SQL Server |
| MongoDB Atlas Search | Lucene BM25 | You're on MongoDB |
| Elasticsearch / OpenSearch | Lucene BM25, analysers, synonyms | Search-heavy products at scale, with rich tuning |

## Consequences

- No extra infrastructure, and the whole index is visible in `init.sql`.
- With no IDF, a common word like "charger" counts as much as a rare one. That makes hybrid search's improvement easier to see, and we say why.
- English-only stemming means a Spanish query (GQ-07) finds nothing here. That is expected, and sets up the ontology's multilingual labels.

## Alternatives considered

| Option | Why not (for this repository) |
|---|---|
| ParadeDB `pg_search` for real BM25 | Changes the Postgres image and adds an extension to explain; it is the natural upgrade path |
| Hand-rolled BM25 in C# over full-text candidates | Educational, but a second ranking to maintain |
| `plainto_tsquery` | Less forgiving of natural input than `websearch_to_tsquery` |

## What to take away

- Lexical search matches **words, not meaning**. It is fast and precise for exact terms, and blind to synonyms: nothing in the catalog says "power brick", so GQ-02 returns nothing useful.
- **Check what your "BM25" actually is.** Many databases ship a different ranking under a familiar name.
- **Show people the parsed query.** Stemming and stop words explain many surprising results.
- **Keyword search rewards using the catalog's words.** "power adapter for my laptop" puts the official Blackbird charger in the top 3, because its description says "laptop power adapter". "charger for my laptop" means the same to a person and ranks it lower. The ontology's synonyms are how you stop depending on the exact word.
- **Which fields you index is a relevance decision.** A cordless phone battery's review says "Cordless phone battery arrived quickly". Once reviews were indexed, even at the lowest weight, it became the top keyword result for "cordless drill battery": a textbook keyword trap.
- **Rewording to fix one stage can break another.** Moving "no drill needed" earlier in the phone battery's description also made it the top keyword result, but pulled it into vector search's top 3 as well. Words that match a query lexically also move the embedding towards it.

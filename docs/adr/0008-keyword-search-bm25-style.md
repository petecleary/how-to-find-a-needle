# ADR-0008: Stage 2 — Keyword search (BM25-style)

- **Status:** Accepted (Phase 2, 2026-09-14). Amended 2026-09-14 by [ADR-0018](0018-scope-and-going-further.md): BGE-M3 removed and stages renumbered, with no change in behaviour; code comments follow in the Phase 2 rework.
- **Date:** 2026-09-13
- **Related:** ADR-0004, ADR-0006, ADR-0011; golden queries GQ-02, GQ-03; roadmap Phase 2

## Context

Lexical search is the workhorse of search systems. The talk calls this stage "BM25", but PostgreSQL's built-in ranking (`ts_rank`, `ts_rank_cd`) is **not** BM25. A teaching repo has to be accurate about that.

We want one clear implementation that learners can run with no extra infrastructure. Learners should also understand the options they would meet elsewhere.

## Decision

### Implementation: PostgreSQL full-text search, labelled "BM25-style"

```sql
SELECT id, name, brand, categories, price, specs,
       ts_rank_cd(search_vector, q) AS score
FROM products, websearch_to_tsquery('english', @query) AS q
WHERE search_vector @@ q
  /* + shared filter fragments (ADR-0007) */
ORDER BY score DESC, id
LIMIT @depth;
```

- **Parsing:** `websearch_to_tsquery('english', …)` accepts natural input (quotes, `or`, `-exclude`) and never throws on user syntax.
- **Document:** the `search_vector` generated column, weighted A (name), B (brand + categories), C (description), D (reviews) ([ADR-0006](0006-database-schema-and-seeding.md)). Reviews were added in Phase 2, at the lowest weight.
- **Ranking:** `ts_rank_cd` (cover density) with default weights; no normalisation flag to start with. The talk explains the difference.
- **Matching:** `@@` requires *all* terms by default (AND semantics). This strictness is part of the lesson (GQ-02 synonym miss).
- **Expansion hook for Stage 5:** `IKeywordSearch` also accepts optional synonym groups from the ontology ([ADR-0013](0013-domain-ontology-and-compatibility.md)).
  - Each group is OR-ed (`phraseto_tsquery(@t1) || phraseto_tsquery(@t2) …`) and AND-ed with the rest of the query.
  - `websearch_to_tsquery` can't express grouped ORs, so expanded queries are assembled from these parameterised fragments. The trace shows the final `tsquery`.
- **Trace:**
  - The SQL.
  - The parsed `tsquery` (via `SELECT websearch_to_tsquery(...)::text`), which shows stemming and stop-word removal: "batteries" → `'batteri'`.
  - For each result, the matched lexemes (via `ts_headline` or a lexeme intersection), plus the score.

### Why this is "BM25-style", not BM25 (documented in the trace notes and the talk)

| BM25 ingredient | Postgres `ts_rank_cd` |
|---|---|
| Term frequency with **saturation** (k1) | Frequency and proximity counted, no saturation curve |
| **Inverse document frequency** (rare terms matter more) | ❌ No corpus statistics; every term weighs the same |
| **Document length normalisation** (b) | Optional via normalisation flags (e.g. `1`, `2`), not BM25's formula |
| Field weights | ✅ `setweight` A/B/C/D |

### Alternatives learners will meet (prose and table only, no code)

| Option | What it is | When you'd choose it |
|---|---|---|
| `LIKE` / `ILIKE` / regex in SQL or code | Substring or pattern match | Tiny datasets, exact codes (SKUs); no ranking, no stemming, usually no index |
| `pg_trgm` | Trigram similarity | Typo tolerance and fuzzy names; complements FTS |
| **ParadeDB `pg_search`** | Real BM25 inside Postgres (Tantivy) | You want true BM25 without leaving Postgres |
| **SQL Server full-text** (`CONTAINS`, `FREETEXTTABLE`) | Built-in FTS with ranking | You're on SQL Server |
| **MongoDB** `$text` / **Atlas Search** | `$text` basic scoring; Atlas Search is Lucene BM25 | You're on MongoDB |
| **Elasticsearch / OpenSearch** | Lucene BM25, analyzers, synonyms | Search-heavy products, large scale, rich relevance tuning |

## Consequences

- There is no extra infrastructure, and the index is visible in `init.sql`.
- The lack of IDF means very common words like "charger" can dominate. That makes the Stage 4 (Hybrid) improvement easier to show, and we are honest about why.
- English-only stemming makes GQ-07 (cross-language) fail here. That is expected, and sets up Stage 5's multilingual ontology labels.

## Alternatives considered

| Option | Why not (for this repo) |
|---|---|
| ParadeDB `pg_search` for real BM25 | Changes the Postgres image and adds an extension to explain; noted as the upgrade path |
| Hand-rolled BM25 in C# over FTS candidates | Educational, but more code and a second ranking to maintain; can be a stretch goal |
| Showing regex and FTS side by side in code | More code for a point the comparison table makes clearly |
| `plainto_tsquery` | Less forgiving of natural input than `websearch_to_tsquery` |

## Teaching notes

- Lexical search matches *words*, not *meaning*. It is fast and precise for exact terms, and blind to synonyms.
- Always check what your "BM25" actually is. Many databases ship a different ranking under a familiar name.
- Show learners the tokenised query. Stemming and stop words explain many surprising results.

**For the talk (found while building, Phase 2):**
- **Keyword search rewards the shopper for using the catalog's words.**
  - "power adapter for my laptop" (GQ-01) puts the official Blackbird charger in the top 3, because its description says "laptop power adapter". "charger for my laptop" ranked it 5th, behind barrel chargers whose descriptions say "laptop charger".
  - Same intent, different words, different winner. The ontology's synonyms (Stage 5) are how you stop depending on the exact word.
- **A device name helps keyword search and hurts vector search.** "charger for my Blackbird Aerobook 14" finds the official charger, because its description names the Aerobook. Stage 3 on the same query ranks laptops and bags first (GQ-08, ADR-0010).
- **Cover density in action (GQ-03).** Before reviews were indexed, "cordless drill battery" matched the phone battery pack, but it came 5th. Drills whose descriptions put "cordless … drill … battery" closer together outranked it. `ts_rank_cd` scores proximity, not meaning.
- **Reviews are where shoppers' words live.** The phone battery's review says "Cordless phone battery arrived quickly". Once reviews were indexed (at the lowest weight, D), the phone battery became keyword search's **#1** result for "cordless drill battery", a textbook keyword trap. Which fields you index is a relevance decision, not just a storage one.
- **Rewording the product didn't work.** Moving "no drill needed" earlier in its description also made it #1 in keyword search, but it pulled the phone battery into vector search's top 3 as well. Words that match a query lexically also move the embedding towards it.

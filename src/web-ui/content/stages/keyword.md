## What it is

Matching words. Postgres [full-text search](term:fts) turns each product into a [tsvector](term:tsvector) and the query into a [tsquery](term:tsquery), then ranks the products that contain every word. We call it "BM25-style", because it isn't [BM25](term:bm25).

## How it works

1. **Parse.** The query is [stemmed](term:stemming) and [stop words](term:stop-word) are dropped: "batteries" becomes the [lexeme](term:lexeme) `batteri`, and "for" and "my" disappear.
2. **Match.** `@@` keeps the products whose text contains **every** lexeme.
3. **Rank.** [ts_rank_cd](term:ts-rank-cd) scores how often and how close together the lexemes appear, with the name weighted above the description and reviews.

## What to look for

**GQ-02**, "power brick for laptop": no product says "power brick", so nothing matches. **GQ-04**, "cordless drill battery": a cordless _phone_ battery ranks near the top, because it shares the words.

## Strength

Fast and exact for the words people really type: product names, model numbers, "USB-C". There is no model to run.

## Failure mode

Blind to meaning. A synonym the catalogue never uses returns nothing, and shared words beat the right product. With no [IDF](term:idf), a common word counts as much as a rare one.

## Try this

Choose **GQ-03**, change "adapter" to "charger" and press Enter. Same intent, different words, and the order changes.

## Read the decision

[ADR-0008 · Keyword search (BM25-style)](adr:0008-keyword-search-bm25-style)

## What it is

Keyword and vector search together. Their scores can't be compared, so [Reciprocal Rank Fusion](term:rrf) combines their **ranks** instead.

## How it works

1. **Retrieve twice.** Keyword and vector search each return up to 50 candidates ([candidate depth](term:candidate-depth)), with the same filters, at the same time.
2. **Fuse.** Each product scores `Σ 1 / (60 + rank)` over the lists it appears in.
3. **Order.** By that sum. A product in both lists beats one that is first in only one of them.

## What to look for

**GQ-03**: keyword search put the cordless phone battery near the top, and hybrid lifts the drill battery above it. Under the hood, the RRF step shows every sum, such as `1/(60+1) + 1/(60+2) = 0.03252`.

## Strength

It keeps exact-word hits and meaning hits without calibrating scores, and the maths fits on a slide. With k = 60, being 1st rather than 2nd barely matters; appearing in both lists matters a lot.

## Failure mode

Better relevance, not correctness. In **GQ-01** the incompatible 45W barrel charger is still near the top, because both retrievers like it.

## Try this

Choose **GQ-01** and look at the badges in Results: each product's keyword rank, vector rank and the RRF sum they make.

## Read the decision

[ADR-0011 · Hybrid search with RRF](adr:0011-hybrid-search-rrf)

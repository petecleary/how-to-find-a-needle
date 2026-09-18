## What it is

Search by the attributes you already know. The filters become [parameterised SQL](term:parameterised-sql), and the database returns exactly the products that match them.

## How it works

1. **Build.** Each filter adds a fixed `WHERE` fragment. Every value is a parameter, never pasted into the SQL.
2. **Expand categories.** A broader [concept](term:skos-concept) includes its narrower ones, so `chargers` also matches `laptop-chargers`.
3. **Match specs.** `specs @> {"voltageV": 18}` is [JSONB containment](term:jsonb-containment), so numbers stay numbers.
4. **Order.** By price, then ID. There is no score.

## What to look for

Choose **GQ-01**: Brakk, at most £100, 18V. Six products come back, and the query box is ignored.

## Strength

Perfect [precision](term:precision) when the shopper already speaks in attributes. It is also the fastest stage.

## Failure mode

People don't speak in attributes. "Something to charge my laptop" isn't a filter, so this stage can't use it.

## Try this

Type any query and press Enter. The results don't change, and Under the hood says the query was ignored.

## Read the decision

[ADR-0007 · Structured search](adr:0007-structured-search)

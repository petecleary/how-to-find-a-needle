# ADR-0005: Curated dataset & golden queries

- **Status:** Accepted
- **Area:** Data
- **Related:** [ADR-0006](0006-database-schema-and-seeding.md), [ADR-0013](0013-domain-ontology-and-compatibility.md)

## Context

The whole talk depends on each stage returning a **visibly different** result for the same query. A scraped product dataset is noisy, has no compatibility facts, and won't reliably produce the moments the talk needs: a keyword trap, a synonym miss, a similar-but-incompatible product.

We also need a repeatable definition of "this stage behaves as the talk claims", so that improving one ranking can't quietly break the demo.

## Decision

### A synthetic catalog: 60 hand-written products, 300 in total

`src/PI.SearchApi/assets/data/products.json`:

- **Fictional brands** (*Blackbird* laptops, *Voltline* chargers, *Kestrel* SSDs and memory, *Brakk* and *Tornio* power tools), so no claim is made about a real product. Prices in GBP.
- **Product groups chosen for their domain rules:** laptops and chargers (connector and wattage), cordless tools and batteries (battery platform), laptop SSDs and memory (interface and memory generation), and keyword-trap items with no rule at all.
- **The curated core (`PROD-0001` to `PROD-0060`) is hand-written** to create each talk moment. Descriptions deliberately share wording: a 45W barrel charger and a 65W USB-C charger are both "a black rectangular laptop power adapter".
- **240 generated products (`PROD-1001` onwards) surround it** (below), so rankings aren't trivially small.

```json
{
  "id": "PROD-0012",
  "name": "Voltline 65W USB-C GaN Charger",
  "brand": "Voltline",
  "categories": ["laptop-chargers", "usb-c-pd-chargers", "phone-chargers"],
  "price": 49.99,
  "currency": "GBP",
  "description": "Compact 65W gallium-nitride power adapter with a single USB-C Power Delivery port...",
  "reviews": ["Charges my ultrabook from flat in under two hours."],
  "specs": { "connector": "usb-c", "wattageW": 65, "powerDelivery": true }
}
```

- `categories` is an array of ontology concept notations; the first one supplies the UI icon.
- **Units live in the key name** (`wattageW`, `voltageV`), so values are numbers, never strings like `"65W"`.
- **Specs are what the domain rules compare.** Accessories carry what they provide (`connector`, `wattageW`); devices carry what they need (`chargingPort`, `minChargerWattageW`).
- **The catalog and the ontology are separate.** Product facts live here; the taxonomy, synonyms and rules live in `domain-ontology.ttl`, which never names a product. Validation tests check that every category and vocabulary value in the catalog exists in the ontology.

### Catalog growth: `tools/PI.CatalogGenerator`

A small C# console app: `dotnet run --project tools/PI.CatalogGenerator`.

- **Templates, not an LLM.** Product lines × variants (capacity, wattage, size, colour), with prices and reviews picked by a seeded random number generator. The output is the same every run, reviewable in a diff, and needs no API key.
- **The curated core is copied byte for byte.** Each run replaces only the generated products after it.
- **Guards for what tests can't see.** It refuses to reuse a curated name, to use the brands the talk moments rely on, or to add a Brakk 18V product at £100 or less (GQ-01 expects exactly six).
- **Look-alikes are few, and mostly near misses.** Chargers, SSDs, memory and batteries get a handful each; most generated products are audio, peripherals, cables and bags. Every generated laptop charger is a near miss for the Blackbird Aerobook 14, such as a 60W USB-C charger that falls 5W short.
- After running it, re-embed once (`Embeddings:Rebuild = true`) and commit the catalog and the embeddings file together ([ADR-0009](0009-local-embeddings-onnx-runtime.md)).

### Golden queries: the talk as tests

`golden-queries.json` records each talk moment and what every stage should do with it. Expectations use product IDs and **loose rank bounds** ("in the top 5", "absent from the top 10", "flagged Incompatible"), never exact scores.

| ID | Query | Moment |
|---|---|---|
| GQ-01 | filters only: Brakk, 18V, under £100 | **Structured search wins:** exactly six products, no ranking needed |
| GQ-02 | "power brick for laptop" | **Synonym miss:** keyword search finds nothing; vector search finds the charger but ranks power banks above it; the ontology's synonyms rescue the keyword side |
| GQ-03 | "power adapter for my laptop" + target device | **Similarity ≠ compatibility:** vector search ranks the 45W barrel charger 2nd; the ontology flags it on connector and wattage |
| GQ-04 | "cordless drill battery" | **Keyword trap:** keyword search ranks a cordless *phone* battery 1st; hybrid lifts the drill battery above it; the ontology marks the phone battery out of concept |
| GQ-05 | "18V battery" + a Brakk drill | **Platform, not voltage:** a Tornio 20V MAX battery looks similar; the ontology rejects it |
| GQ-06 | "SSD upgrade for my laptop" + target device | **Interface, not shape:** a SATA M.2 2280 SSD reads like the NVMe one the laptop needs; the ontology flags it |
| GQ-07 | "cargador USB-C para portátil" | **Beyond English:** the Spanish label "cargador" matches *Chargers*, and English terms find the chargers |
| GQ-08 | "charger for my Blackbird Aerobook 14" | **The device-name trap:** vector search ranks the laptop and a Blackbird sleeve above the chargers; the ontology treats the name as context |
| GQ-09 | "65W USB-C charger", no device | **No device, still checked:** the ontology reads "USB-C" and "65W" as requirements, flags the barrel and 45W chargers, and says which laptops each charger fits |

Golden queries do three jobs: they are the **integration tests**, the **UI presets**, and the **talk steps**.

GQ-03, GQ-05 and GQ-06 take the device from a "my device" picker (`context.targetProductId`) rather than naming it, so each isolates one failure. Naming the device is a failure of its own, and GQ-08 shows it.

### When a moment stops happening

Change the **wording of the data**, not the algorithm and not the assertion. Change an assertion only when the claim itself was wrong, and record why.

## Consequences

- The thesis is written as data plus tests, so the demo is deterministic and checkable.
- Synthetic data can be accused of being rigged. The answer is to say so: the data is curated to *isolate* each failure, and the same failures happen in real catalogs.
- Embeddings are not fully under our control. Several expectations were reworded after running real models, as the lessons below describe.

## Alternatives considered

| Option | Why not (for this repository) |
|---|---|
| A scraped or Kaggle product dataset | No compatibility facts, noisy text, can't reliably produce each moment |
| Real brands and specs | Risks inaccurate claims about real products |
| An LLM-generated catalog | Fast, but uncontrolled and not reproducible; templates did the distractors just as well |
| A ~500-product catalog | Tried first. So many compatible look-alikes that the curated products fell out of view and most rank bounds failed; 300 keeps the moments visible |
| Exact-score assertions | Brittle across models and quantisation; rank bounds are enough |

## What to take away

- **Evaluation data is part of the search system.** Without golden queries, "better search" is an opinion.
- Normalise units at ingestion. Structured search and ontology rules both depend on it.
- **Each golden query should isolate one failure.** "battery for Brakk 18V drill" mixed a brand pull, a device word and the platform near miss. "18V battery" with a target device shows only the near miss.
- **Real embeddings rewrote our queries, and that is what golden queries are for.** The first drafts named the device in the query, and vector search ranked every Blackbird product above the chargers. We didn't loosen the tests; we asked what each query was meant to prove, and gave the device-name failure its own query.
- **Sometimes the expectation is wrong.** GQ-04 first expected hybrid search to push the phone battery out of its top 3 *and* keyword search to rank it highly. Under RRF those pull against each other ([ADR-0011](0011-hybrid-search-rrf.md)).
- **Scale changes rankings, not moments.** Growing the catalog to 500 broke 7 of 23 checks while every moment still happened: "the official charger is in the top 3" failed because six generated compatible chargers competed with it. A test that pins one product to a tight rank is partly a test of catalog size.
- **"Power brick" sits closer to "power bank" than to "laptop charger".** Add power banks and vector search ranks them first. Meaning is fuzzy in both directions.
- **Distractor wording matters too.** Generated SSDs that said "for desktops and laptops", and memory reviews saying "easy upgrade", outranked the curated SATA near miss for "SSD upgrade for my laptop". Real catalogs are full of such accidental matches.

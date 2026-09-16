# ADR-0005: Curated dataset & golden queries

- **Status:** Proposed
- **Date:** 2026-09-13
- **Related:** ADR-0006, ADR-0013; roadmap Phase 1 (and Phase 5 for growth)

## Context

The whole talk depends on every stage returning a **visibly different** result for the same query. A scraped dataset (such as the Datafiniti/Kaggle electronics CSV) is noisy and lacks compatibility facts. It also won't reliably produce the moments the talk needs: a keyword trap, a synonym miss, a similar-but-incompatible item.

We also need a repeatable definition of "this stage behaves as the talk claims", so that changing one ranking doesn't quietly break the demo.

## Decision

### 1. A hand-curated synthetic catalog, `assets/data/products.json`

- **Size:** 60 hand-written items (Phase 1), grown to **300** in Phase 5 by a generator that adds plausible *distractors* around the curated core (see "Catalog growth" below). The curated core stays hand-written. The first target was ~500; it was cut to 300 so the generated look-alikes don't bury the curated moments (see Teaching notes).
- **Fictional brands and products** (confirmed): for example *Blackbird* laptops, *Voltline* chargers, *Kestrel* SSDs and memory, *Brakk* and *Tornio* power tools. This avoids making false spec claims about real products, and avoids trademark issues. Prices are in GBP.
- **Product groups** (each chosen to carry a domain rule):

| Product group | Domain rule the ontology defines |
|---|---|
| Laptops + chargers / USB-C power | connector type (USB-C PD vs barrel), minimum wattage |
| Cordless tools + batteries + battery chargers | voltage **platform** (e.g. Brakk 18V vs Tornio 20V MAX: similar numbers, different platforms) |
| Laptop upgrades: M.2 SSDs + SO-DIMM memory | SSD interface (NVMe vs SATA, both sold as "M.2 2280"), memory generation (DDR4 vs DDR5) |
| Keyword-trap items | none (they exist to fool lexical matching) |

- **The Datafiniti CSV is dropped.** It isn't in the repo and nothing references it after the Phase 0 clean-up.

### 2. Product schema

```json
{
  "id": "PROD-0012",
  "name": "Voltline 65W USB-C GaN Charger",
  "brand": "Voltline",
  "categories": ["laptop-chargers", "usb-c-pd-chargers", "phone-chargers"],
  "price": 49.99,
  "currency": "GBP",
  "description": "Compact 65W gallium-nitride power adapter with a single USB-C Power Delivery port...",
  "reviews": [
    "Charges my ultrabook from flat in under two hours.",
    "Small enough to leave in the laptop bag."
  ],
  "specs": {
    "connector": "usb-c",
    "wattageW": 65,
    "powerDelivery": true
  }
}
```

- `categories` is an **array**, because real catalogs list one product in several places. Each entry is a `skos:notation` from the ontology's taxonomy ([ADR-0013](0013-domain-ontology-and-compatibility.md)).
  - Order matters only for display: the first category supplies the UI icon.
  - Whether a product is a *device* (and so can be a target device) comes from its categories, not from a separate field.
- `specs` holds display and filter attributes with **normalised units in the key name** (`wattageW`, `voltageV`), so values are numbers rather than strings like "65W".
- **Specs are also what the domain rules compare** ([ADR-0013](0013-domain-ontology-and-compatibility.md)).
  - Accessories carry what they provide (`connector`, `wattageW`, `interface`, `memoryType`, `platform`).
  - Devices carry what they need (`chargingPort`, `minChargerWattageW`, `m2SlotInterface`, `memoryType`, `batteryPlatform`).
  - Values for vocabulary-backed specs use the ontology's notations (`"usb-c"`, `"nvme"`).
- **The ontology never names a product.** `products.json` holds the product facts; `domain-ontology.ttl` holds the taxonomy, synonyms and class-level rules. The two meet through category notations and spec names, and catalog validation tests enforce that contract.
- Descriptions and reviews are written to deliberately create near-miss wording (for example, "black rectangular laptop power adapter" on both the 65W USB-C and the 45W barrel charger).

### 3. Golden queries, `assets/data/golden-queries.json`

Each golden query records the talk moment it demonstrates and the **expected outcome per stage**. Expectations use product IDs and loose rank bounds (for example, "in top 3", "absent from top 10", "flagged Incompatible"), never exact scores.

| ID | Query (draft) | Moment it demonstrates |
|---|---|---|
| GQ-01 | "power adapter for my laptop" (+ target device) | **Similarity ≠ compatibility**: Vector ranks the 45W barrel charger highly; Ontology flags it Incompatible (connector and wattage) |
| GQ-02 | "power brick for laptop" | **Synonym miss**: Keyword finds nothing useful (catalog says "adapter"/"charger"); Vector finds the charger, but ranks power banks above it; Stage 5 synonym expansion rescues the keyword side |
| GQ-03 | "cordless drill battery" | **Keyword trap**: Keyword ranks a *cordless phone battery* 1st; Vector ranks the drill battery first; Hybrid lifts the drill battery above the trap, which stays close behind (3rd with 60 products, 4th with 300); Ontology marks it OutOfConcept |
| GQ-04 | filters only: brand = Brakk, voltageV = 18, maxPrice = 100 | **Structured wins**: exact, fast, no ranking needed |
| GQ-05 | "18V battery" (+ target device) | **Platform compatibility**: the Tornio 20V MAX battery looks similar; Ontology rejects it (platform) |
| GQ-06 | "SSD upgrade for my laptop" (+ target device) | **Interface compatibility**: a SATA M.2 2280 SSD reads almost identically to the NVMe one the laptop needs; Ontology flags it |
| GQ-07 | cross-language (e.g. "cargador USB-C para portátil") | **Multilingual**: Keyword and Vector alone are weak. Stage 5 matches the Spanish ontology label ("cargador" → *Chargers*) and expands to English terms, so the right chargers appear. Full-sentence cross-language retrieval with a multilingual embedding model is discussed in the talk, not built ([ADR-0018](0018-scope-and-going-further.md)) |
| GQ-08 | "charger for my Blackbird Aerobook 14" (no target device) | **The device name trap**: Vector ranks the named laptop and a Blackbird sleeve above the chargers; Stage 5 recognises the device name as context, uses it as the target device, and puts the compatible chargers first ([ADR-0013](0013-domain-ontology-and-compatibility.md)) |
| GQ-09 | "65W USB-C charger" (no target device) | **No device, still checked** (added 2026-09-16): Stage 5 reads "USB-C" and "65W" as requirements, flags the barrel and 45W chargers with reasons, and lists which catalog laptops each charger fits ([ADR-0013](0013-domain-ontology-and-compatibility.md)) |

Queries GQ-01, GQ-05 and GQ-06 take their target device from `context.targetProductId`, as if from a "my device" picker, and don't name it (changed in Phase 2, after running the golden queries against real embeddings):
- With "Blackbird Aerobook 14" or "Brakk 18V drill" in the query text, vector search ranked every product of that brand (laptops, bags, other tools) above the accessories the moment is about. Each query should isolate one moment, so the device-name failure has its own golden query, GQ-08.
- GQ-05 avoids the word "drill" so that its moment is only about the battery platform.
- GQ-01 says "power adapter", which the catalog uses. With "charger for my laptop", keyword search ranked the official charger 5th; the exact wording is the point of its keyword expectation.

Shape:

```json
{
  "id": "GQ-01",
  "title": "Similarity is not compatibility",
  "request": { "query": "power adapter for my laptop", "context": { "targetProductId": "PROD-0001" } },
  "expectations": {
    "vector":   [{ "productId": "PROD-0014", "rank": { "max": 5 } }],
    "ontology": [{ "productId": "PROD-0014", "compatibility": "Incompatible" },
                 { "productId": "PROD-0012", "compatibility": "Compatible", "rank": { "max": 3 } }]
  }
}
```

Golden queries have three uses:
1. They are **integration test cases** ([ADR-0002](0002-solution-structure-and-orchestration.md)).
2. They are **UI presets** (`GET /api/demo/queries`, [ADR-0003](0003-search-api-contract-and-debug-trace.md)).
3. They drive the **talk-mode stage steps** in the UI ([ADR-0014](0014-web-ui-architecture.md)).

### Catalog growth (Phase 5)

`tools/PI.CatalogGenerator` is a C# console app (no new language or package in the repo). Run it from the repository root with `dotnet run --project tools/PI.CatalogGenerator`.

- **Templates, not an LLM.** Fixed product lines × variants (capacity, wattage, size, colour), with prices and reviews picked by a seeded `Random`. The output is deterministic, reviewable in a diff and needs no API key.
- **The curated core is copied byte for byte.** Hand-written products keep IDs below `PROD-1001`; generated products take `PROD-1001` onwards, after them in the same file. Each run replaces only the generated part.
- **Guards the tests can't see.** The generator refuses to write a catalog that reuses a curated name or ID, uses a brand reserved for the curated moments (Blackbird, Voltline), or adds a Brakk 18V product at £100 or less (GQ-04 asserts exactly six).
- **Look-alikes are few and mostly near misses.** Rule categories (chargers, SSDs, memory, batteries) get a handful of products each; the rest is filler (audio, peripherals, cables, bags). For the Aerobook 14, every generated laptop charger is a near miss, such as a 60W USB-C charger that misses by 5W.
- After running it, re-embed once with `Embeddings:Rebuild = true` and commit `products.json` and `embeddings/nomic.jsonl` together.

### Authoring workflow

1. Write golden queries first. Each one states the moment it needs.
2. Author the products that create that moment (target, correct answer, near-miss, trap).
3. Add filler products so rankings aren't trivially small.
4. Run the golden-query tests. If a stage doesn't behave as claimed, adjust the *data* (wording, specs), never the assertions. Record significant data tweaks in the product's `description` history, not in code.

## Consequences

- The demo becomes deterministic and testable: the thesis is written as data plus tests.
- Synthetic data can be accused of being rigged. We address that openly in the talk: the data is curated to *isolate* each failure mode, and the same failure modes happen in real catalogs.
- The embeddings' actual behaviour is not fully under our control. Some golden expectations may need wording iterations once Stage 3 exists (Phase 2).

## Alternatives considered

| Option | Why not (for this repo) |
|---|---|
| Datafiniti/Kaggle CSV | No compatibility facts, noisy text, can't reliably produce each talk moment |
| Real brands and real specs | Risk of inaccurate claims about real products; trademark noise |
| LLM-generated catalog in bulk | Fast, but uncontrolled and not reproducible; templates did the distractors just as well (Phase 5) |
| Growing to ~500 products | Tried first. So many compatible look-alikes that most rank-bound expectations failed, and the talk's products left the top of the list; 300 keeps the moments visible |
| Exact-score assertions | Brittle across model and quantisation changes; rank bounds are sufficient |

## Teaching notes

- Evaluation data is part of the search system. Without golden queries, "better search" is an opinion.
- Normalise units at ingestion (`wattageW: 65`, not `"65W"`). Structured search and ontologies both depend on it.

**For the talk (found while building, Phase 2):**
- **Real embeddings rewrote our golden queries, and that's the point of having them.** The first drafts named the device ("charger for my Blackbird Aerobook 14"), and three expectations failed. Vector search ranked Blackbird laptops and bags above the chargers, because the brand was the loudest thing in the query.
  - We didn't loosen the tests. We asked what each query was meant to prove.
  - The talk moments now take the device from a picker.
  - The device-name failure got its own golden query (GQ-08), and Stage 5 learned to handle it.
- **Each golden query should isolate one failure mode.** "battery for Brakk 18V drill" mixed three things: a brand pull, a device-type word ("drill") and the platform near miss. "18V battery" with a target device shows only the platform near miss.
- **Sometimes the expectation, not the data, was wrong.** GQ-03 first expected hybrid search to push the phone battery out of its top 3 *and* keyword search to rank it in its top 3.
  - Under RRF those two pull against each other (ADR-0011).
  - We changed the hybrid expectation to "ranked below the drill battery", which is what fusion honestly achieves, and left the removal to the ontology.
  - Changing an assertion is allowed when the claim itself was wrong, never just to make a test pass.
- **Wording is part of the experiment.** "charger for my laptop" and "power adapter for my laptop" mean the same thing to a person. They produce different keyword winners (ADR-0008).

**For the talk (found while growing the catalog, Phase 5):**
- **Scale changes rankings, not moments.** At 500 products, 7 of 23 golden-query checks failed, but every moment still happened. Checks such as "the official Blackbird charger is in the top 3" failed because six generated compatible chargers competed with it. A test that pins one product to a tight rank is really a test of catalog size.
- **"Power brick" is closer to "power bank" than to "laptop charger".** With power banks in the catalog, vector search ranks them above every laptop charger. Keyword search still finds nothing, and Stage 5's synonym expansion (power brick → power adapter, laptop chargers) still puts a charger first. Meaning is fuzzy in both directions.
- **Retrieve deep, page late, and read every page.** Stage 5 keeps flagged items but sorts them last. At 300 products the incompatible barrel chargers for GQ-08 sit around rank 60, behind every out-of-concept product. They are still retrieved and flagged, just not on page one, so the golden-query client now reads all pages.
- **A device name removed from the query stops retrieving the device.** In the 60-product core, "charger for my" still retrieved the Aerobook itself; at 300 it doesn't. That is Stage 5 working, not failing.
- **Distractor wording matters too.** Generated SSDs first said "for desktops and laptops" and outranked the curated SATA near miss for "SSD upgrade for my laptop"; memory reviews saying "easy upgrade" did the same. Real catalogs are full of such accidental matches.

# ADR-0005: Curated dataset & golden queries

- **Status:** Proposed
- **Date:** 2026-09-13
- **Related:** ADR-0006, ADR-0013; roadmap Phase 1 (and Phase 5 for growth)

## Context

The whole talk depends on every stage returning a **visibly different** result for the same query. A scraped dataset (such as the Datafiniti/Kaggle electronics CSV) is noisy and lacks compatibility facts. It also won't reliably produce the moments the talk needs: a keyword trap, a synonym miss, a similar-but-incompatible item.

We also need a repeatable definition of "this stage behaves as the talk claims", so that changing one ranking doesn't quietly break the demo.

## Decision

### 1. A hand-curated synthetic catalog, `assets/data/products.json`

- **Size:** start with about 60 items (Phase 1). A generator script grows it to about 500 in Phase 5 by adding plausible *distractors* around the curated core. The curated core stays hand-written.
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
| GQ-01 | "charger for my Blackbird Aerobook 14" (+ target device) | **Similarity ≠ compatibility**: Vector ranks the 45W barrel charger highly; Ontology flags it Incompatible (connector and wattage) |
| GQ-02 | "power brick for laptop" | **Synonym miss**: Keyword finds nothing useful (catalog says "adapter"/"charger"); Vector succeeds; Stage 6 synonym expansion rescues the keyword side |
| GQ-03 | "cordless drill battery" | **Keyword trap**: Keyword ranks a *cordless phone battery* highly; Vector and Hybrid correct it |
| GQ-04 | filters only: brand = Brakk, voltageV = 18, maxPrice = 100 | **Structured wins**: exact, fast, no ranking needed |
| GQ-05 | "battery for Brakk 18V drill" (+ target device) | **Platform compatibility**: the Tornio 20V MAX battery looks similar; Ontology rejects it (platform) |
| GQ-06 | "SSD upgrade for my Blackbird Aerobook 14" (+ target device) | **Interface compatibility**: a SATA M.2 2280 SSD reads almost identically to the NVMe one the laptop needs; Ontology flags it |
| GQ-07 | cross-language (e.g. "cargador USB-C para portátil") | **Multilingual**: BGE-M3 finds the right chargers; Nomic (English-centric) is weaker |

Shape:

```json
{
  "id": "GQ-01",
  "title": "Similarity is not compatibility",
  "request": { "query": "charger for my Blackbird Aerobook 14", "context": { "targetProductId": "PROD-0001" } },
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
| LLM-generated catalog in bulk | Fast, but uncontrolled; fine for distractors (Phase 5), not for the curated core |
| Exact-score assertions | Brittle across model and quantisation changes; rank bounds are sufficient |

## Teaching notes

- Evaluation data is part of the search system. Without golden queries, "better search" is an opinion.
- Normalise units at ingestion (`wattageW: 65`, not `"65W"`). Structured search and ontologies both depend on it.

# ADR-0013: Stage 6 — Domain ontology: taxonomy, synonyms & rules

- **Status:** Proposed
- **Date:** 2026-09-13
- **Related:** ADR-0003, ADR-0004, ADR-0005, ADR-0007, ADR-0008, ADR-0011, ADR-0016, ADR-0017; golden queries GQ-01, GQ-02, GQ-03, GQ-05, GQ-06, GQ-07; roadmap Phase 1 (ontology file), Phase 2 (stage)

## Context

Stages 2–5 answer *"what is relevant?"* using words and vectors. They don't know what things **are**, what people **call** them, or which **domain rules** apply:
- A "power brick" is a charger.
- A cordless *phone* battery isn't a power-tool battery.
- A charger has to match a laptop's port and power needs.

The talk's triad makes this the "Ontology: how is it related and constrained?" layer.

We want a **domain-level ontology**: a small, stable model of concepts, their names and their rules. It never describes individual products. Facts about specific products belong in the catalog. Storing them as triples would turn this into a knowledge graph or graph database, which is a different talk.

## Decision

### One Turtle file, `assets/data/domain-ontology.ttl`, three parts, no product IDs

**1. Taxonomy (SKOS concept scheme).** The product categories.
- `skos:broader` / `skos:narrower`, for example `Power › Chargers › Laptop chargers / USB-C PD chargers`, `Computers › Laptops`, `Storage › SSDs › NVMe SSDs / SATA SSDs`, `Power tools › Batteries`, `Telephony › Phone batteries`.
- Each concept has a `skos:notation` slug (`laptop-chargers`). **Products reference concepts by these slugs** in their `categories` array ([ADR-0005](0005-curated-dataset-and-golden-queries.md)).
- `skos:definition` on each concept, used later for grounded explanations ([ADR-0017](0017-pedagogy-engine.md)).
- `ex:icon` names a Lucide icon for the UI.
- Device types are marked `ex:isDeviceType true`, which is how `GET /api/demo/devices` knows which products can be a target device.

**2. Synonyms and labels.**
- `skos:prefLabel`, `skos:altLabel` ("power brick", "AC adapter", "PSU") and `skos:hiddenLabel` (common misspellings).
- **Language-tagged labels** (`"Cargadores"@es`), so query understanding works beyond English (GQ-07).

**3. Value vocabularies and class-level domain rules.**
- Value vocabularies are small concept schemes for constrained spec values: connectors (`usb-c`, `barrel-5.5mm`), storage interfaces (`nvme`, `sata`), memory types (`ddr4-sodimm`, `ddr5-sodimm`), battery platforms (`brakk-18v`, `tornio-20v-max`). Each has labels and synonyms ("Type-C" → `usb-c`).
- Rules are stated **once per pair of product types**. Each compares a spec on the accessory with a spec on the device.

```turtle
@prefix ex:   <https://needle.example/ontology#> .
@prefix skos: <http://www.w3.org/2004/02/skos/core#> .

# --- Taxonomy -------------------------------------------------------------
ex:Chargers a skos:Concept ; skos:broader ex:Power ;
    skos:notation "chargers" ; ex:icon "plug" ;
    skos:prefLabel "Chargers"@en , "Cargadores"@es ;
    skos:altLabel "power adapter"@en , "AC adapter"@en , "power brick"@en , "PSU"@en ;
    skos:hiddenLabel "chager"@en ;
    skos:definition "A device that supplies electrical power to run or recharge another device."@en .

ex:LaptopChargers a skos:Concept ; skos:broader ex:Chargers ;
    skos:notation "laptop-chargers" ;
    skos:prefLabel "Laptop chargers"@en ; skos:altLabel "laptop power supply"@en .

ex:Laptops a skos:Concept ; skos:broader ex:Computers ;
    skos:notation "laptops" ; ex:isDeviceType true ;
    skos:prefLabel "Laptops"@en , "Portátiles"@es ; skos:altLabel "notebook"@en , "ultrabook"@en .

# --- Value vocabulary -----------------------------------------------------
ex:UsbC a skos:Concept ; skos:inScheme ex:Connectors ; skos:notation "usb-c" ;
    skos:prefLabel "USB-C"@en ; skos:altLabel "Type-C"@en , "USB Type-C"@en .

# --- Domain rule (class level) --------------------------------------------
ex:ChargerFitsLaptop a ex:CompatibilityRule ;
    ex:accessoryType ex:LaptopChargers ;
    ex:deviceType    ex:Laptops ;
    ex:check [ ex:accessorySpec "connector" ; ex:operator ex:equals ;         ex:deviceSpec "chargingPort" ;
               ex:valueScheme ex:Connectors ;
               skos:definition "The charger's plug must fit the laptop's charging port."@en ] ,
             [ ex:accessorySpec "wattageW"  ; ex:operator ex:greaterOrEqual ; ex:deviceSpec "minChargerWattageW" ;
               skos:definition "The charger must supply at least the power the laptop needs."@en ] .
```

- Initial rules:

  | Rule | Accessory spec | Operator | Device spec | Value scheme |
  |---|---|---|---|---|
  | Charger fits laptop | `connector` | = | `chargingPort` | `ex:Connectors` |
  | | `wattageW` | ≥ | `minChargerWattageW` | — (numeric) |
  | SSD fits laptop | `interface` | = | `m2SlotInterface` | `ex:StorageInterfaces` |
  | Memory fits laptop | `memoryType` | = | `memoryType` | `ex:MemoryTypes` |
  | Battery fits tool | `platform` | = | `batteryPlatform` | `ex:BatteryPlatforms` |

- Operators are deliberately few: `equals`, `greaterOrEqual`, `lessOrEqual`, `in`. **`ex:valueScheme`** marks a check as vocabulary-backed: its accessory and device values are compared **as concepts** in that scheme, so `"Type-C"` and `"usb-c"` are equal. A check with no `ex:valueScheme` compares raw values (numbers, for `wattageW`). Catalog validation tests use `ex:valueScheme` to know which spec values must resolve to a known notation or label ([roadmap Phase 1](roadmap.md#phase-1--data)).
- **Products carry the values** in their `specs` (for example a laptop's `chargingPort: "usb-c"`, `minChargerWattageW: 65`). The ontology carries the **rules**. Adding a product never requires changing the ontology; adding a new *type* of product does.

### Engine

- **dotNetRDF** (`dotNetRdf.Core`). `IOntology` (singleton) loads the TTL into an in-memory graph at startup and queries it with the Leviathan SPARQL engine.
- **SPARQL lookups in `.rq` files** under `assets/data/queries/`, so learners read them as queries:
  - `labels.rq`: every label → concept, loaded once into an in-memory matcher.
  - `taxonomy.rq`: the concept tree for `GET /api/taxonomy`.
  - `narrower.rq`: transitive narrower concepts (`skos:broader*`).
  - `rules.rq`: every compatibility rule and its checks (accessory/device type, spec names, operator, value scheme).
- **Rule checks run in C#** with a small generic evaluator (one method per operator) over the specs of the candidate and the target device. The rules are data from the TTL; the C# never names a rule.
- **No OWL reasoner and no stored instance triples.** Subsumption uses SPARQL property paths, which keeps behaviour predictable and explainable.
- **A minimal loader ships in Phase 1** ([roadmap](roadmap.md#phase-1--data)): `Pipeline/Ontology/DomainOntology.cs` loads the TTL and exposes read-only concept, narrower-concept, vocabulary-value and rule lookups, so Phase 1's catalog validation tests can check `products.json` against the real ontology instead of a duplicate parser. It is not registered in DI. Phase 2 ([roadmap](roadmap.md#phase-2--search-apis-stages-16)) adds the label matcher, the `rules-for-types` lookup, `GET /api/taxonomy` and rule evaluation on top of the same loader.

### `GET /api/taxonomy`

Returns the concept tree for the UI's category filter ([ADR-0014](0014-web-ui-architecture.md)):

```json
[{ "notation": "power", "label": "Power", "labels": { "en": "Power" }, "icon": "zap",
   "narrower": [{ "notation": "chargers", "label": "Chargers",
                  "labels": { "en": "Chargers", "es": "Cargadores" },
                  "altLabels": ["power adapter", "AC adapter", "power brick", "PSU"],
                  "definition": "A device that supplies…", "icon": "plug", "narrower": [] }] }]
```

The same file drives navigation (filters), query understanding (Stage 6), validation (catalog tests, request validators) and explanation (Stages 7–8).

### Stage 6 pipeline (`IOntologySearch`)

Each step is its own trace step ([ADR-0003](0003-search-api-contract-and-debug-trace.md), [ADR-0004](0004-pipeline-composition.md)).

1. **Understand.** Match the normalised query against all labels in any language: longest match first, no overlaps, phrases of 1–3 words.
   - Output: the matched taxonomy concepts and value concepts, e.g. "power brick for laptop" → *Chargers*, *Laptops*.
   - This is lexical and simple, and the trace shows exactly which phrase matched which label.
2. **Expand** (`options.expandSynonyms`, default on). For each matched taxonomy concept, collect the labels of the concept and its narrower concepts, capped at 10 terms per concept.
   - **Keyword:** the matched phrase becomes an OR group (`phraseto_tsquery('power brick') || phraseto_tsquery('ac adapter') || …`), AND-ed with the rest of the query ([ADR-0008](0008-keyword-search-bm25-style.md)).
   - **Vector:** the query is embedded with the concepts' preferred labels appended: `"power brick for laptop (chargers, laptop chargers)"`.
3. **Retrieve.** Keyword + Vector with the expansions, fused with RRF. This is the Stage 4 pipeline with better input ([ADR-0011](0011-hybrid-search-rrf.md)).
4. **Classify.** A candidate is `InConcept` if any of its categories is a matched concept or narrower than one, and `OutOfConcept` otherwise. With no matched concept it is `NoConcept`.
   - Example: the cordless *phone* battery is under *Telephony*, not *Power tools › Batteries*, so it is `OutOfConcept` (GQ-03).
5. **Constrain** (`options.applyConstraints`, default on). Resolve the target device:
   - `context.targetProductId` if given;
   - otherwise the longest match of a device product's name (a product in an `ex:isDeviceType` category) in the query text;
   - otherwise none.

   Then, for each candidate, find the rules for (candidate categories, device categories) and evaluate every check:
   - `Compatible`: all checks pass.
   - `Incompatible`: any check fails.
   - `Unknown`: a needed spec is missing, or there is no target device.
   - `NotEvaluated`: no rule applies.

   Reasons quote the rule definition and the values, e.g. *"The charger's plug must fit the laptop's charging port: charger has 5.5 mm barrel, Blackbird Aerobook 14 needs USB-C."*

**Ordering, with flagged items kept:** unflagged items first, then `OutOfConcept`, then `Incompatible`. Within each group items keep their fused rank. **Nothing is silently removed.**

**Trace:**
- Matched phrases → concepts.
- Expanded terms and the resulting tsquery and embedding text.
- Classification per candidate, with the broader-chain that justified it.
- Target-device resolution method.
- The rules found (SPARQL text and bindings) and every check with its values and result.

**Toggles for the talk:** Stage 4 → Stage 6 with only `expandSynonyms` (recall improves) → Stage 6 with both (precision and correctness improve).

### Tests

- **Unit:**
  - Label matcher: longest match, multilingual labels, hidden-label misspellings.
  - Transitive narrower expansion and the term cap.
  - The tsquery OR-group builder.
  - Rule evaluator: each operator; pass, fail and missing-spec cases; synonym-equal values ("Type-C" = `usb-c`).
  - Classification, including the broader chain.
  - Taxonomy endpoint shape.
- **Catalog validation (Phase 1):**
  - Every product category is a taxonomy notation.
  - Every vocabulary-backed spec value is a known notation or label.
  - Every device and accessory type has the specs its rules need.
- **Integration:**
  - GQ-01 and GQ-06: incompatible items flagged.
  - GQ-02: expansion rescues the keyword side.
  - GQ-03: phone battery `OutOfConcept`.
  - GQ-05: platform mismatch flagged.
  - GQ-07: a Spanish label match ("cargador") expands to English charger terms and finds the right chargers.

## Consequences

- The ontology is small, readable and product-agnostic: roughly a page of Turtle per product family. Catalog growth to ~500 products needs no ontology edits.
- Stage 6 shows the ontology improving **recall** (synonym and narrower expansion) as well as **precision and correctness** (classification and domain rules). The similarity ≠ compatibility example still lands, explained by a general rule instead of a stored fact.
- Spec naming in `products.json` becomes part of the contract with the ontology, enforced by tests.
- Label matching is lexical and brittle ("brick" alone won't match "power brick"). The trace makes this visible, and the talk names entity recognition as the next step.
- The rule language is intentionally tiny. Rules that need more (e.g. "USB-C PD *profile* supports 20V") would need a richer model, which is noted as the boundary where SHACL or a knowledge graph starts to earn its keep.

## Alternatives considered

| Option | Why not (for this repo) |
|---|---|
| Per-product compatibility facts in Turtle (previous draft) | That is a knowledge graph; it duplicates catalog data and pushes toward a graph database |
| Graph database (Neo4j, GraphDB) | Extra infrastructure for a problem a small domain model solves |
| SHACL shapes for constraints | The standard for validating RDF *instance* data, so it needs product triples; a good "going further" note |
| OWL classes + reasoner | Powerful but opaque; SKOS matches how people name and browse things, and is easier to explain |
| Compatibility rules hard-coded in C# | Hides "knowledge as data"; rules can't be listed, explained or reused by the UI and LLM stages |
| Separate "Ontology expansion" stage | A ninth stage; toggles inside Stage 6 show the same before and after more compactly |
| LLM for query understanding | Exactly the black box the talk argues against; explicit labels are inspectable and deterministic |
| Remove incompatible or out-of-concept items | Hides the most important result: *why* something was demoted |

## Teaching notes

- **Ontology ≠ knowledge graph.** An ontology describes the *domain* (concepts, names, rules); a knowledge graph describes the *things*. Start with the ontology; many search problems stop there.
- SKOS gives you taxonomy, synonyms and multilingual labels in a standard, tiny vocabulary.
- One domain model serves many jobs: navigation, query understanding, validation and explanation.
- Similarity is a guess, and a rule is knowledge. Keep demoted results and their reasons visible.

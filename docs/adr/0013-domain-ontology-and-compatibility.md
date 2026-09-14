# ADR-0013: Stage 5 — Ontology: SKOS taxonomy, vocabularies & domain rules

- **Status:** Accepted (Phase 2, 2026-09-14). Amended 2026-09-14 by [ADR-0018](0018-scope-and-going-further.md): SKOS-first framing, BGE-M3 removed and stages renumbered, with no change in behaviour (code updated in the Phase 2 rework). Amended again 2026-09-14 to add `GET /api/vocabularies`, built and verified the same day.
- **Date:** 2026-09-13
- **Related:** ADR-0003, ADR-0004, ADR-0005, ADR-0007, ADR-0008, ADR-0011, ADR-0016, ADR-0017, ADR-0018; golden queries GQ-01, GQ-02, GQ-03, GQ-05, GQ-06, GQ-07; roadmap Phase 1 (ontology file), Phase 2 (stage)

## Context

Stages 2–4 answer *"what is relevant?"* using words and vectors. They don't know what things **are**, what people **call** them, or which **domain rules** apply:
- A "power brick" is a charger.
- A cordless *phone* battery isn't a power-tool battery.
- A charger has to match a laptop's port and power needs.

The talk's triad makes this the "Ontology: how is it related and constrained?" layer.

**Start with SKOS.** Most of what search needs from a domain model is vocabulary: what things are called (including synonyms and other languages), how categories nest, and which values a spec may take. Developers already build this, usually as enums, lookup tables and scattered JSON config. [SKOS](https://www.w3.org/TR/skos-reference/) (Simple Knowledge Organization System) is the W3C standard for it. It lives in a Turtle file that any language can read with an off-the-shelf RDF parser. The talk presents this stage as taxonomy and vocabulary first, not as graph theory or formal logic.

**Then take one step beyond it.** SKOS can say that a laptop charger is a kind of charger. It can't say that a charger must fit a laptop's port. Compatibility needs rules, so we add the smallest rule vocabulary that shows the idea, and we say plainly that this is where SKOS ends and where OWL, SHACL and knowledge graphs begin ([ADR-0018](0018-scope-and-going-further.md)).

We want a **domain-level ontology**: a small, stable model of concepts, their names and their rules. It never describes individual products. Facts about specific products belong in the catalog. Storing them as triples would turn this into a knowledge graph or graph database, which is a different talk.

## Decision

### One Turtle file, `assets/data/domain-ontology.ttl`, three parts, no product IDs

**1. Taxonomy (SKOS concept scheme).** The product categories.
- `skos:broader` / `skos:narrower`, for example `Power › Chargers › Laptop chargers / USB-C PD chargers`, `Computers › Laptops`, `Storage › SSDs › NVMe SSDs / SATA SSDs`, `Power tools › Batteries`, `Telephony › Phone batteries`.
- Each concept has a `skos:notation` slug (`laptop-chargers`). **Products reference concepts by these slugs** in their `categories` array ([ADR-0005](0005-curated-dataset-and-golden-queries.md)).
- `skos:definition` on each concept, used later for grounded explanations ([ADR-0017](0017-pedagogy-engine.md)).
- `ex:icon` names a Lucide icon for the UI.
- Device types are marked `ex:isDeviceType true`, which is how `GET /api/demo/devices` knows which products can be a target device.

**2. Synonyms and labels (SKOS).**
- `skos:prefLabel`, `skos:altLabel` ("power brick", "AC adapter", "PSU") and `skos:hiddenLabel` (common misspellings).
- **Language-tagged labels** (`"Cargadores"@es`), so query understanding works beyond English (GQ-07).

**3. Value vocabularies (SKOS) and class-level domain rules (beyond SKOS).**
- Value vocabularies are small concept schemes for constrained spec values: connectors (`usb-c`, `barrel-5.5mm`), storage interfaces (`nvme`, `sata`), memory types (`ddr4-sodimm`, `ddr5-sodimm`), battery platforms (`brakk-18v`, `tornio-20v-max`). Each has labels and synonyms ("Type-C" → `usb-c`).
- Rules are stated **once per pair of product types**. Each compares a spec on the accessory with a spec on the device.
- **The rules are not SKOS.** `ex:CompatibilityRule`, `ex:check` and the operators are this repo's own small vocabulary, like `ex:icon` and `ex:isDeviceType`. They are written in RDF so they live in the same file and are read with the same SPARQL, but no standard defines them. This is the deliberate edge of SKOS in the talk: the point where a real system would reach for SHACL or OWL.

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
    ex:accessoryType ex:Chargers ;          # every charger, not only laptop chargers (changed in Phase 2)
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
  - `vocabularies.rq`: every value vocabulary, with its name and its values' preferred labels, for `GET /api/vocabularies`.
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

The same file drives navigation (filters), query understanding (Stage 5), validation (catalog tests, request validators) and explanation (Stages 6–7: concept definitions, and labels chosen for the audience, [ADR-0017](0017-pedagogy-engine.md)).

### `GET /api/vocabularies`

Returns every value vocabulary, for the UI's spec filters ([ADR-0014](0014-web-ui-architecture.md)). Added 2026-09-14 so the filters are built from the ontology rather than hard-coded:

```json
[{ "notation": "connectors", "label": "Connectors",
   "specs": ["chargingPort", "connector"],
   "values": [{ "notation": "usb-c", "label": "USB-C", "labels": { "en": "USB-C" },
                "altLabels": ["Type-C", "USB Type-C"] }] }]
```

- **Values** come from `vocabularies.rq` (each scheme's English name, each value's preferred labels) and `labels.rq` (synonyms). Hidden labels (misspellings) are never returned: they help match queries, and a filter never shows them.
- **`specs` comes from the rules.** Every check with an `ex:valueScheme` names the accessory spec and the device spec that hold values from that vocabulary, so "connectors" lists `connector` (on chargers) and `chargingPort` (on laptops).
  - A filter sends `filters.specs: { "connector": "usb-c" }`, which Stage 1's JSONB containment matches because products store notations ([ADR-0007](0007-structured-search.md)).
  - A vocabulary that no rule uses still appears, with an empty `specs` list.
- Vocabularies and values are ordered by notation, so the response is stable.
- Brand and price aren't ontology data. They describe products, so any options for those filters would come from the catalog, not from this endpoint.

### Editing the ontology

`IOntology` loads the TTL once, at startup, from the copy the build places next to the API. To see an edit (a new category, synonym, value or rule), stop the AppHost and run `aspire run` again. The build copies the edited file, the API loads it, and a browser refresh shows the new categories and filter values, with no UI code change. Catalog validation tests check that products still use known categories and values.

### Stage 5 pipeline (`IOntologySearch`)

Each step is its own trace step ([ADR-0003](0003-search-api-contract-and-debug-trace.md), [ADR-0004](0004-pipeline-composition.md)).

1. **Understand.** Match the normalised query against all labels in any language: longest match first, no overlaps, phrases of 1–3 words.
   - *Normalised* means lower-cased, accents folded ("portátil" = "portatil") and simple English plurals folded on both sides ("chargers" = "charger", "batteries" = "battery"). Without plural folding, "charger" would never match the preferred label "Chargers". It is still lexical, and the trace says so.
   - Both taxonomy concepts (categories) and value concepts (e.g. `usb-c`) are matched and shown. Only taxonomy concepts are expanded and used for classification; a matched value phrase stays in the rest of the query.
   - **Resolve the target device first** (moved from step 5 in Phase 2):
     - use `context.targetProductId` if given;
     - otherwise the longest device product name (a product in an `ex:isDeviceType` category) that appears in the query, matched as a whole folded token sequence;
     - otherwise none.
   - **A device name is context, not intent.** If the query names the target device ("charger for my Blackbird Aerobook 14"), those words are claimed before label matching and removed from what keyword and vector search receive. The trace shows the name that was removed. *Why:* with the device name left in the text, vector search ranked the laptop itself, other Blackbird laptops and a Blackbird sleeve above every charger (GQ-08).
   - **The device's own type is context too.** A matched taxonomy concept that is a device type, and that the target device belongs to ("laptop" when the device is a laptop, "drill" when it's a drill), is a *context concept*. It stays in the query text but isn't expanded and isn't used for classification. *Why:* otherwise every laptop or drill is `InConcept` and unflagged, and outranks the accessories the shopper asked for.
   - Only the remaining *wanted concepts* are expanded (step 2) and classified against (step 4).
   - Output: the matched taxonomy concepts and value concepts, e.g. "power brick for laptop" → *Chargers*, *Laptops*.
   - This is lexical and simple, and the trace shows exactly which phrase matched which label.
2. **Expand** (`options.expandSynonyms`, default on). For each matched taxonomy concept, collect the labels of the concept and its narrower concepts, capped at 10 terms per concept.
   - Term order before the cap: the matched phrase, then English preferred and alternative labels (the concept first, then its narrower concepts), then labels in other languages. Hidden labels (misspellings) help match queries, not documents, so they aren't expanded.
   - **Keyword:** the matched phrase becomes an OR group (`phraseto_tsquery('power brick') || phraseto_tsquery('ac adapter') || …`), AND-ed with the rest of the query ([ADR-0008](0008-keyword-search-bm25-style.md)).
   - **Vector:** the query is embedded with the concepts' preferred labels appended: `"power brick for laptop (chargers, laptop chargers)"`.
3. **Retrieve.** Keyword + Vector with the expansions, fused with RRF. This is the Stage 4 pipeline with better input ([ADR-0011](0011-hybrid-search-rrf.md)).
4. **Classify.** A candidate is `InConcept` if any of its categories is a matched concept or narrower than one, and `OutOfConcept` otherwise. With no matched concept it is `NoConcept`.
   - Example: the cordless *phone* battery is under *Telephony*, not *Power tools › Batteries*, so it is `OutOfConcept` (GQ-03).
5. **Constrain** (`options.applyConstraints`, default on). Using the target device resolved in step 1, for each candidate find the rules for (candidate categories, device categories) and evaluate every check:
   - `Compatible`: all checks pass.
   - `Incompatible`: any check fails.
   - `Unknown`: a needed spec is missing, or there is no target device.
   - `NotEvaluated`: no rule applies.

   Reasons quote the rule definition and the values, e.g. *"The charger's plug must fit the laptop's charging port: charger has 5.5 mm barrel, Blackbird Aerobook 14 needs USB-C."*

**Ordering, with flagged items kept:** unflagged items first, then `OutOfConcept`, then `Incompatible`. Within each group items keep their fused rank. **Nothing is silently removed.**
- `options.applyConstraints` switches off both demotions and the rule checks. `signals.conceptMatch` is still reported, so the presenter can show the classification before turning it on.
- An item that is both `OutOfConcept` and `Incompatible` goes in the `Incompatible` group.
- The target device itself goes in the `OutOfConcept` group, with the reason "This is your target device". Shoppers asking for a charger for their laptop don't want the laptop, but it isn't hidden.
- Device resolution and context concepts apply when `expandSynonyms` is on; with both toggles off, retrieval receives the query as typed.

**Trace:**
- Matched phrases → concepts.
- Expanded terms and the resulting tsquery and embedding text.
- Classification per candidate, with the broader-chain that justified it.
- Target-device resolution method.
- The rules found (SPARQL text and bindings) and every check with its values and result.

**Toggles for the talk:** Stage 4 → Stage 5 with only `expandSynonyms` (recall improves) → Stage 5 with both (precision and correctness improve).

### Tests

- **Unit:**
  - Label matcher: longest match, multilingual labels, hidden-label misspellings.
  - Transitive narrower expansion and the term cap.
  - The tsquery OR-group builder.
  - Rule evaluator: each operator; pass, fail and missing-spec cases; synonym-equal values ("Type-C" = `usb-c`).
  - Classification, including the broader chain.
  - Taxonomy endpoint shape.
  - Value vocabularies: every scheme except the taxonomy, synonyms without hidden labels, and spec keys taken from the rules.
- **Catalog validation (Phase 1):**
  - Every product category is a taxonomy notation.
  - Every vocabulary-backed spec value is a known notation or label.
  - Every device and accessory type has the specs its rules need.
- **Integration:**
  - `GET /api/vocabularies` returns connectors with both spec keys and the "Type-C" synonym.
  - GQ-01 and GQ-06: incompatible items flagged.
  - GQ-02: expansion rescues the keyword side.
  - GQ-03: phone battery `OutOfConcept`.
  - GQ-05: platform mismatch flagged.
  - GQ-07: a Spanish label match ("cargador") expands to English charger terms and finds the right chargers.

## Consequences

- The model is mostly standard SKOS, which developers can read in minutes and reuse from any language. The rules are the one home-made part, and this ADR, the trace and the talk say so.
- The ontology is small, readable and product-agnostic: roughly a page of Turtle per product family. Catalog growth to ~500 products needs no ontology edits.
- Stage 5 shows the ontology improving **recall** (synonym and narrower expansion) as well as **precision and correctness** (classification and domain rules). The similarity ≠ compatibility example still lands, explained by a general rule instead of a stored fact.
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
| Separate "Ontology expansion" stage | An eighth stage; toggles inside Stage 5 show the same before and after more compactly |
| LLM for query understanding | Exactly the black box the talk argues against; explicit labels are inspectable and deterministic |
| Remove incompatible or out-of-concept items | Hides the most important result: *why* something was demoted |
| Hard-code category and spec filter values in the UI | Duplicates the ontology in a second language and drifts from it; a new value would need a UI change |
| Return value vocabularies inside `GET /api/taxonomy` | Categories and spec values do different jobs; two small responses are easier to read and render than one mixed tree |
| Hot-reload the TTL with a file watcher | Swaps the ontology while requests are running, which is hidden state; re-running the AppHost takes seconds and is visible |
| Present SKOS and the rules as one undifferentiated "ontology" | Hides where the standard ends; learners should know which part they can reuse as it is |

## Teaching notes

- **Ontology ≠ knowledge graph.** An ontology describes the *domain* (concepts, names, rules); a knowledge graph describes the *things*. Start with the ontology; many search problems stop there.
- SKOS gives you taxonomy, synonyms and multilingual labels in a standard, tiny vocabulary.
- **SKOS replaces code you already write.** Enums for categories, lookup tables for synonyms and JSON files of allowed values become one standard file that .NET, Python, Java or TypeScript can load.
- **One file, two payoffs.** Today it drives the app: navigation, filters and query expansion. With an LLM it becomes context: the definitions and labels of the *matched* concepts go into the prompt, so explanations use the domain's own terms (Stages 6–7). Only matched concepts go in, which keeps the prompt small.
- **Grounding in definitions reduces invented terminology; it doesn't prevent it.** Stage 6's validation catches the rest (ADR-0016).
- **Labels hang off concepts.** Falling back from a missing Spanish label to the English one is a lookup on the same concept, not a join or a `COALESCE`. Large domains can split into several Turtle files loaded into one graph. Both are mentioned in the talk, not built.
- **Where SKOS ends: constraints.** SKOS names and organises; it doesn't state rules. Our small rule vocabulary is the first step past it, and OWL, SHACL and knowledge graphs are the next ([ADR-0018](0018-scope-and-going-further.md)).
- One domain model serves many jobs: navigation, query understanding, validation and explanation.
- **Filters are data too.** When the category tree and the allowed spec values come from the ontology, a value added to a vocabulary appears in the filters, the validation tests and the rule checks at once, with no UI or C# change.
- Similarity is a guess, and a rule is knowledge. Keep demoted results and their reasons visible.

**For the talk (found while building, Phase 2):**
- **"A device name is context, not intent."** People search the way they think: "charger for my Blackbird Aerobook 14". Stages 2–4 can't tell what you *want* from what you *own*. The device name is the most distinctive part of the query, so it wins: in Stage 3 the Aerobook itself ranks 2nd and a Blackbird laptop sleeve 5th, while the compatible Voltline charger is 7th (GQ-08). Stage 5 understands the query before retrieving: it recognises the device, removes it from the search text, uses it as the target device, and the compatible chargers come first.
- **Show the trace of GQ-08 in Stage 5.** Put `deviceMention: "Blackbird Aerobook 14"` next to `queryWithoutDevice: "charger for my"`. That one line is query understanding.
- **"Laptop" can be context too.** In "power adapter for my laptop" (GQ-01) with an Aerobook as the target, "laptop" describes what you own. Treating it as a wanted category would put every laptop above the chargers.
- **Honest limits to mention.**
  - Device matching is exact: "my Aerobook" alone isn't found, and product names like "Brakk 18V Combi Drill (Body Only)" are rarely typed in full.
  - Fuzzy entity recognition (aliases, model numbers) is the next step. It could be more ontology or catalog data, such as product aliases, without an LLM.
- **A rule scoped too narrowly is a silent gap.**
  - "Charger fits laptop" first applied only to *Laptop chargers*.
  - With the device-name trap fixed, GQ-08's third result was the Voltline 20W USB-C *phone* charger, unflagged. No rule said anything about a phone charger and a laptop.
  - Widening the rule to every *Charger* — one line of Turtle, no C# — flagged it: "must supply at least the power the laptop needs: 20W; needs at least 65W".
  - Lesson: state a rule at the most general concept it's true for. A shopper can plug any charger into a laptop.
- **Why the other golden queries don't name the device:** GQ-01, GQ-05 and GQ-06 take the device from a "my device" picker (`targetProductId`), so each isolates one moment. GQ-08 exists to show the device-name trap on its own.

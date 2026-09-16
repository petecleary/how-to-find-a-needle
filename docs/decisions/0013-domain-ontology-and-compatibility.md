# ADR-0013: Stage 5 — Ontology: SKOS taxonomy, vocabularies & domain rules

- **Status:** Accepted
- **Area:** Search
- **Related:** [ADR-0003](0003-search-api-contract-and-debug-trace.md), [ADR-0004](0004-pipeline-composition.md), [ADR-0005](0005-curated-dataset-and-golden-queries.md), [ADR-0008](0008-keyword-search-bm25-style.md), [ADR-0011](0011-hybrid-search-rrf.md), [ADR-0016](0016-rag-grounding-and-citations.md), [ADR-0017](0017-pedagogy-engine.md), [ADR-0018](0018-scope-and-going-further.md); golden queries GQ-01 to GQ-03, GQ-05 to GQ-08

## Context

Stages 2–4 answer *"what is relevant?"* with words and vectors. They don't know what things **are**, what people **call** them, or which **rules** apply:

- A "power brick" is a charger.
- A cordless *phone* battery is not a power-tool battery.
- A charger has to fit a laptop's port and supply enough power.

This is the talk's second question: *how is it related and constrained?*

**Start with SKOS.** Most of what search needs from a domain model is vocabulary: names and synonyms (in several languages), how categories nest, and which values a spec may take. Developers already build this as enums, lookup tables and JSON config. [SKOS](https://www.w3.org/TR/skos-reference/), the Simple Knowledge Organization System, is the W3C standard for it, stored in a Turtle file any language can read.

**Then take one step beyond it.** SKOS can say a laptop charger is a kind of charger. It can't say a charger must fit a laptop's port. Compatibility needs rules, so we add the smallest rule vocabulary that shows the idea, and say plainly that this is where SKOS ends.

The model describes the **domain**, never individual products. Product facts stay in the catalog. Storing them as triples would make this a knowledge graph, which is a different talk.

## Decision

### One Turtle file, three parts, no product IDs

`src/PI.SearchApi/assets/data/domain-ontology.ttl`:

1. **Taxonomy (SKOS).** Categories linked with `skos:broader`, such as *Power › Chargers › Laptop chargers*. Each concept has a `skos:notation` slug (`laptop-chargers`) that products use in `categories`, a `skos:definition`, an icon name for the UI, and `ex:isDeviceType true` if products in it can be a target device.
2. **Labels and synonyms (SKOS).** `skos:prefLabel`, `skos:altLabel` ("power brick", "AC adapter", "PSU") and `skos:hiddenLabel` (misspellings such as "chager"), **tagged by language** (`"Cargadores"@es`).
3. **Value vocabularies (SKOS) and domain rules (beyond SKOS).** Small concept schemes for constrained values: connectors (`usb-c`, `barrel-5.5mm`), storage interfaces, memory types and battery platforms, each with synonyms ("Type-C" = `usb-c`). Then **rules, stated once per pair of product types**, each comparing a spec on the accessory with a spec on the device.

```turtle
ex:Chargers a skos:Concept ; skos:broader ex:Power ;
    skos:notation "chargers" ;
    skos:prefLabel "Chargers"@en , "Cargadores"@es ;
    skos:altLabel "power adapter"@en , "AC adapter"@en , "power brick"@en , "PSU"@en , "cargador"@es ;
    skos:hiddenLabel "chager"@en ;
    skos:definition "A device that supplies electrical power to run or recharge another device."@en .

ex:ChargerFitsLaptop a ex:CompatibilityRule ;
    ex:accessoryType ex:Chargers ;
    ex:deviceType    ex:Laptops ;
    ex:check [ ex:accessorySpec "connector" ; ex:operator ex:equals ;         ex:deviceSpec "chargingPort" ;
               ex:valueScheme ex:Connectors ;
               skos:definition "The charger's plug must fit the laptop's charging port."@en ] ,
             [ ex:accessorySpec "wattageW"  ; ex:operator ex:greaterOrEqual ; ex:deviceSpec "minChargerWattageW" ;
               skos:definition "The charger must supply at least the power the laptop needs."@en ] .
```

| Rule | Accessory spec | Operator | Device spec |
|---|---|---|---|
| Charger fits laptop | `connector` | = | `chargingPort` |
| | `wattageW` | ≥ | `minChargerWattageW` |
| SSD fits laptop | `interface` | = | `m2SlotInterface` |
| Memory fits laptop | `memoryType` | = | `memoryType` |
| Battery fits tool | `platform` | = | `batteryPlatform` |

- **The rules are not SKOS.** `ex:CompatibilityRule`, `ex:check` and the operators are this repository's own tiny vocabulary, written in RDF so they sit in the same file and are read with the same SPARQL. No standard defines them. This is the edge of SKOS, where a real system would reach for SHACL or OWL.
- **Operators are few:** equals, greater-or-equal, less-or-equal, in. A check with `ex:valueScheme` compares values *as concepts*, so "Type-C" equals `usb-c`; a check without one compares raw numbers.
- **Products carry the values; the ontology carries the rules.** Adding a product never needs an ontology change. Adding a new *type* of product does.

### Engine

- **dotNetRDF** loads the Turtle into an in-memory graph at startup and queries it with SPARQL.
- **The SPARQL lives in `.rq` files** in `assets/data/queries/` (labels, taxonomy, narrower concepts, vocabularies, rules), so it reads as queries, not strings.
- **Rule checks run in C#**, with one small method per operator. The rules are data from the Turtle; the C# never names a rule.
- **No OWL reasoner and no product triples.** Category nesting uses SPARQL property paths, which keeps behaviour predictable and explainable.
- **One file, many jobs:** the UI's category and spec filters (`GET /api/taxonomy`, `GET /api/vocabularies`), query understanding in this stage, catalog validation tests, and the definitions and words used by the LLM stages. Edit the file, restart the AppHost, and the filters change with no UI code.

### The Stage 5 pipeline

Each step is its own trace step.

1. **Understand.**
   - **Resolve the target device first:** `context.targetProductId`, or else the longest whole device name found in the query. *A device name is context, not intent:* its words are removed from what keyword and vector search receive, and the trace shows what was removed.
   - **Match labels:** longest phrase first (up to three words), no overlaps, in every language, after lower-casing, folding accents ("portátil" = "portatil") and folding simple plurals ("chargers" = "charger"). It is lexical, and the trace says so.
   - **The device's own type is context too.** "laptop" in "power adapter for my laptop", when the device is a laptop, isn't a wanted category; otherwise every laptop would outrank the chargers.
2. **Expand** (`expandSynonyms`). Each wanted concept becomes its labels and its narrower concepts' labels, up to ten terms. Keyword search gets an OR group of phrases (`'power brick' | 'power adapter' | 'laptop charger' …`), AND-ed with the rest of the query. Vector search gets the concepts' names appended: `"power brick for laptop (chargers, laptop chargers)"`.
3. **Retrieve.** Keyword and vector search with the better input, fused with RRF: the Stage 4 pipeline ([ADR-0011](0011-hybrid-search-rrf.md)).
4. **Classify.** `InConcept` if a category is a wanted concept or narrower than one, `OutOfConcept` otherwise, `NoConcept` when nothing was wanted. The phone battery sits under *Telephony*, not *Power tools › Batteries*.
5. **Constrain** (`applyConstraints`). For each candidate, find the rules for its type and the device's type, and run every check: `Compatible` (all pass), `Incompatible` (any fail), `Unknown` (a spec is missing, or there is no device), `NotEvaluated` (no rule applies). Reasons quote the rule and the values: *"The charger's plug must fit the laptop's charging port. Voltline 45W Barrel Charger has 5.5mm barrel; Blackbird Aerobook 14 needs USB-C."*

**Nothing is silently removed.** Unflagged items come first, then out-of-concept items and the target device itself, then incompatible items, each group in fused order. With `applyConstraints` off, nothing is moved and the classification is still shown, so the presenter can show each effect in turn.

### Without a target device

Most shoppers don't pick a device first. Two things keep the rules useful when they haven't:

- **Requirements stated in the query.** In *Understand*, values the rules compare become requirements, for the rules of the wanted category: a matched value concept ("USB-C", "Type-C", "Brakk 18V") becomes `connector = usb-c`, and a number with a unit ("65W", "65 W") becomes `wattageW ≥ 65`, with the unit telling which spec it measures and the rule's own operator. In *Constrain*, with no device, each candidate is checked against them, the stated value standing in for the device's: *"✗ The charger's plug must fit the laptop's charging port. Voltline 45W Barrel Charger has 5.5mm barrel; you asked for USB-C."* A check the query says nothing about stays `Unknown`, because nobody said how much power is needed. `compatibility.source` is `Query`.
- **Which devices it fits.** Every product a rule applies to also gets `compatibility.fits`: the same device checks, run against every catalog device of the type the rule names, such as *"Fits 6 of 13 laptops"*. It's context: it never changes the status or the order.

A target device always decides. Stated values are still shown in the trace, and any the device contradicts ("45W" for a laptop that needs 65W) are listed as conflicts. With neither a device nor a stated value, products stay `Unknown`, now with their fits list. GQ-09 ("65W USB-C charger") shows it, and GQ-07's barrel charger is flagged too, because the Spanish query says "USB-C".

## Consequences

- The model is mostly standard SKOS, readable in minutes and reusable from any language. The rules are the one home-made part, and this record, the trace and the talk all say so.
- Stage 5 improves **recall** (synonyms and narrower concepts) as well as **precision and correctness** (classification and rules). "Similarity is not compatibility" is explained by a general rule, not a stored fact.
- Spec names in the catalog are part of the contract with the ontology, and tests enforce it.
- Label matching is brittle: "brick" alone doesn't match "power brick", and "my Aerobook" doesn't match "Blackbird Aerobook 14". Fuzzy entity recognition is the next step.
- Stated requirements only cover values the rules already compare. "18V battery" states nothing checkable, because the battery rule compares platforms, not volts: exactly the near miss GQ-05 is about.
- The rule language is deliberately tiny. A rule like "this USB-C PD profile supports 20V" would need a richer model: where SHACL or a knowledge graph starts to earn its keep.

## Alternatives considered

| Option | Why not (for this repository) |
|---|---|
| Per-product compatibility facts in Turtle | That's a knowledge graph; it duplicates the catalog and pushes toward a graph database |
| A graph database (Neo4j, GraphDB) | Infrastructure for a problem a small domain model solves |
| SHACL shapes | The standard for validating RDF *instance* data, so it needs product triples; a going-further topic |
| OWL classes and a reasoner | Powerful but opaque; SKOS matches how people name and browse things |
| Rules hard-coded in C# | Can't be listed, explained or reused by the UI and the LLM stages |
| An LLM for query understanding | Exactly the black box the talk argues against; labels are inspectable and deterministic |
| Remove incompatible items | Hides the most important result: *why* something was demoted |
| Hard-code filter values in the UI | Duplicates the ontology and drifts from it |

## What to take away

- **An ontology is not a knowledge graph.** An ontology describes the *domain* (concepts, names, rules); a knowledge graph describes the *things*. Many search problems stop at the ontology.
- **SKOS replaces code you already write.** Category enums, synonym tables and lists of allowed values become one standard file.
- **One file, two payoffs.** It drives the app (filters, query expansion, validation) and, for the LLM stages, becomes context: only the matched concepts' definitions and labels go into the prompt.
- **State a rule at the most general concept it is true for.** "Charger fits laptop" first applied only to *laptop* chargers, so a 20W *phone* charger came third for GQ-08, unflagged. Widening the rule to every charger (one line of Turtle, no C#) flagged it: *"The charger must supply at least the power the laptop needs. Voltline 20W USB-C Phone Charger has 20W; Blackbird Aerobook 14 needs at least 65W."*
- **"A device name is context, not intent."** People search the way they think: "charger for my Blackbird Aerobook 14". Stages 2–4 can't tell what you *want* from what you *own*, so the device name wins. Stage 5 recognises it, removes it from the search text, uses it as the target device, and the compatible chargers come first. In the trace, `deviceMention: "Blackbird Aerobook 14"` next to `queryWithoutDevice: "charger for my"` is query understanding in one line.
- **The knowledge is in the rules, not in the device.** "65W USB-C charger" flags the barrel charger with no device at all: the stated values stand in for the device's specs in the same checks. A picked device is one source of requirements; the query is another.
- **Keep demoted results visible, and page far enough to see them.** Similarity is a guess; a rule is knowledge. In the 300-product catalog, GQ-08's flagged chargers rank 52nd to 67th, after every out-of-concept product. They are still there, still flagged with reasons, which is why the UI reads every page.

# ADR-0013: Stage 6 — Domain ontology & compatibility

- **Status:** Proposed
- **Date:** 2026-09-13
- **Related:** ADR-0004, ADR-0005, ADR-0011, ADR-0016; golden queries GQ-01, GQ-05, GQ-06; roadmap Phase 1 (vocabulary and data), Phase 2 (stage)

## Context

Stages 2–5 answer *"what is relevant?"*. None of them can answer *"will it work with my device?"*. Compatibility is a matter of **explicit constraints**: connector type, minimum wattage, voltage platform, SSD interface, memory type. Constraints need explicit knowledge, not similarity.

The talk's triad makes this the "Ontology: how is it related and constrained?" layer. RDF/Turtle with SPARQL is the teaching vehicle.

## Decision

### Knowledge sources: Turtle for knowledge, JSON for the catalog

Two files, each in the format that suits its job, joined by product ID:

| File | Holds | Read by |
|---|---|---|
| `assets/data/products.json` | Catalog records: names, prices, descriptions, reviews, display and filter specs ([ADR-0005](0005-curated-dataset-and-golden-queries.md)) | Postgres seeder → stages 1–5 |
| `assets/data/domain-ontology.ttl` | **Vocabulary** and the **compatibility facts** for every device and accessory, hand-written | dotNetRDF → stage 6 |

- **Vocabulary:**
  - Classes: `ex:Product`, `ex:Device`, `ex:Accessory`, `ex:Laptop`, `ex:Charger`, `ex:Ssd`, `ex:MemoryModule`, `ex:PowerTool`, `ex:Battery`.
  - Properties: `ex:connectorType`, `ex:wattageW`, `ex:requiresConnector`, `ex:requiresMinWattageW`, `ex:storageInterface`, `ex:supportsStorageInterface`, `ex:memoryType`, `ex:supportsMemoryType`, `ex:voltagePlatform`, `ex:compatibleWith`, `ex:requires`, `ex:partOf`.
  - Named individuals: connectors (`ex:UsbC`, `ex:Barrel5_5mm`), storage interfaces (`ex:NvmePcie4`, `ex:SataM2`), memory types (`ex:Ddr4SoDimm`, `ex:Ddr5SoDimm`), platforms (`ex:Brakk18V`, `ex:Tornio20VMax`).
  - `rdfs:label` and `skos:altLabel` for devices, used to link device names in query text to entities.
- **Facts:** one block per product IRI (`ex:PROD-0012` matches `"id": "PROD-0012"`). This is the file the audience sees on the ontology slide.
- **Keeping the files consistent:** a catalog validation test (roadmap Phase 1) fails if:
  - a TTL product IRI has no matching catalog id, or a catalog device or accessory has no TTL facts;
  - a value present in both files (for example `wattageW` or connector) disagrees.
- **Why this split:** Turtle is the natural, readable format for relationships and constraints, and writing it by hand is part of the lesson. JSON stays the natural format for records that Postgres and the embedders consume. The overlap is small and covered by a test.

```turtle
@prefix ex:   <https://needle.example/ontology#> .
@prefix rdfs: <http://www.w3.org/2000/01/rdf-schema#> .

ex:Charger rdfs:subClassOf ex:Accessory .
ex:UsbC a ex:ConnectorType ; rdfs:label "USB-C" .

# compatibility facts, keyed by products.json ids
ex:PROD-0001 a ex:Laptop ; rdfs:label "Corvid Aerobook 14" ;
    ex:requiresConnector ex:UsbC ; ex:requiresMinWattageW 65 ;
    ex:supportsStorageInterface ex:NvmePcie4 ; ex:supportsMemoryType ex:Ddr5SoDimm .
ex:PROD-0012 a ex:Charger ; ex:connectorType ex:UsbC ; ex:wattageW 65 .
ex:PROD-0014 a ex:Charger ; ex:connectorType ex:Barrel5_5mm ; ex:wattageW 45 .
```

### Engine

- **dotNetRDF** (`dotNetRdf.Core`). The graph is loaded once into an in-memory `Graph`/`TripleStore` by `IKnowledgeGraph` (singleton), and queried with the Leviathan SPARQL engine.
- Rules are **SPARQL queries in `.rq` files** under `assets/data/rules/`, one per constraint: `charger-connector.rq`, `charger-wattage.rq`, `battery-platform.rq`, `ssd-interface.rq`, `memory-type.rq`, `asserted-compatibility.rq`. They are loaded at startup, so learners read the rules as queries rather than C# logic.
- **Asserted vs derived:**
  - An explicit `ex:compatibleWith` triple is *asserted* and always wins.
  - Otherwise compatibility is *derived* from the rule queries.
  - The trace labels which applied.
- No OWL reasoner. RDFS subclass handling uses SPARQL property paths (`rdf:type/rdfs:subClassOf*`). This keeps behaviour predictable and explainable.

### Resolving the target device

1. `context.targetProductId`, if supplied (the UI device picker). This is the most reliable.
2. Otherwise, **entity linking**: a case-insensitive match of device `rdfs:label` / `skos:altLabel` against the query text. The longest match wins.
3. No target → every candidate gets `compatibility.status = "Unknown"` with the reason *"No target device identified: compatibility can't be evaluated."* This is honest, and a teaching point in itself.

The trace records which method resolved the target.

### Evaluation (`IOntologyEvaluator`)

- **Input:** Hybrid candidates ([ADR-0004](0004-pipeline-composition.md)).
- **For each candidate that is an accessory in a relevant class**, run the applicable rule queries (bound to `?device` and `?accessory`). Each rule yields pass or fail plus a reason, for example:
  - *"Connector mismatch: device requires USB-C (`ex:PROD-0001 ex:requiresConnector ex:UsbC`), charger provides 5.5mm barrel (`ex:PROD-0014 ex:connectorType ex:Barrel5_5mm`)."*
  - *"Insufficient power: 45W < required 65W."*
- **Status:**
  - `Compatible`: every applicable rule passes, or an asserted `compatibleWith` exists.
  - `Incompatible`: any rule fails.
  - `Unknown`: no rules apply or facts are missing.
  - `NotEvaluated`: the candidate is not an accessory, e.g. another laptop.
- **Ordering, and rejected items are kept:** Compatible items keep their Hybrid order and come first, then Unknown, then **Incompatible items, which stay in the results, flagged with their reasons**. They are not silently removed. The trace also lists the rejected items explicitly.
- **Trace:** the target resolution, each SPARQL query text with its bindings, the triples that justified each decision, and the counts per status.

### Tests

- **Unit:** TTL ↔ catalog consistency; each rule against small in-memory graphs (pass, fail and missing-fact cases); target-device label linking.
- **Integration:** GQ-01, GQ-05 and GQ-06 expectations.

## Consequences

- Shows the core argument: the vector-similar 45W barrel charger is flagged Incompatible with a readable, triple-backed reason.
- Rules as `.rq` files are easy to read and extend, though SPARQL is new to many learners. The talk introduces just enough.
- Label-based entity linking is simple and brittle, and the trace makes that clear. Real systems use proper entity resolution.
- Stages 7–8 get structured, explainable facts to ground on ([ADR-0016](0016-rag-grounding-and-citations.md)).

## Alternatives considered

| Option | Why not (for this repo) |
|---|---|
| Compatibility rules in C# `if` statements | Works, but hides the "knowledge as data" lesson and can't be queried |
| Project facts from `products.json` into RDF at startup | A single source of truth, but hides the Turtle authoring the talk wants to show; the small overlap is covered by a consistency test instead |
| OWL reasoner (e.g. HermiT via a Java service) | Powerful, but heavy and opaque for a 30-minute talk |
| Graph database (Neo4j + Cypher) | Extra infrastructure; RDF/SPARQL better represents shared vocabularies |
| Remove incompatible items | Hides the most important result of the talk: *why* something was rejected |
| Let the LLM judge compatibility | Exactly the failure the talk warns about; constraints must be explicit |

## Teaching notes

- Search finds candidates; knowledge constrains them. Similarity is a guess, and a constraint is a fact.
- Keep rejected results and their reasons visible. That is where users (and developers) learn.
- Use each format for what it is good at: Turtle for relationships and constraints, JSON and SQL for catalog records. Then test that they agree.

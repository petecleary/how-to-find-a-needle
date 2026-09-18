[SKOS](term:skos) names and organises things; it doesn't state rules. Our small [domain-rule](term:domain-rule) vocabulary is the first step past that boundary. These are the next ones.

### OWL and reasoners

[OWL](term:owl) is the W3C language for saying what a class _is_, formally enough that software can draw conclusions from it. Declare that a `LaptopCharger` is anything with a `chargingPort` and a `wattage`, and a [reasoner](term:reasoner) will classify products you never labelled, infer that a narrower concept inherits its parent's constraints, and tell you when two statements contradict each other.

The trade is familiar: inference you didn't write is inference you have to debug. Our rules run in C# and every check appears in the trace with its _has_ and _needs_ values. A reasoner is more powerful and less inspectable ([ADR-0013 · Domain ontology](adr:0013-domain-ontology-and-compatibility)).

### SHACL, for the data

OWL describes meaning; [SHACL](term:shacl) validates **shapes**. "Every laptop has exactly one `chargingPort`, from this vocabulary." "Every charger states a wattage in watts." Run it over the catalogue and you get a report of what's missing, before a shopper finds out by getting the wrong answer.

This is where our compatibility rules would grow next, and it is the closest thing in the ontology world to a test suite.

### Knowledge graphs and graph databases

**An ontology is not a knowledge graph.** An ontology describes the _domain_ — kinds of thing, their names, how they relate. A knowledge graph describes the _things_: this product, that manufacturer, this firmware version, who supplies whom. We keep the domain in [Turtle](term:turtle) and the products in Postgres, on purpose.

You need the graph when the question needs **multiple hops**: "which of my orders contain a part that this recall affects?" Neo4j, or a triple store queried with [SPARQL](term:sparql), answers that in one traversal where SQL needs a join per hop. A [graph database](term:graph-database) is the right tool for a question shaped like a path — and many search problems stop, correctly, at the ontology.

### Growing what's here

A large domain splits across several Turtle files loaded into one graph; concepts get versioned and deprecated rather than deleted; and `skos:exactMatch` links your concepts to public vocabularies so two organisations can mean the same thing. All of it is standard SKOS, and none of it needs a new database.

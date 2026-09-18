## What it is

Search that knows the domain. A [SKOS](term:skos) [taxonomy](term:taxonomy) names the categories, their synonyms in several languages and the allowed spec values. One step beyond SKOS, a few [domain rules](term:domain-rule) say what fits what. Stage 5 runs both in four steps — understand the query and any device you own, expand it with the taxonomy's synonyms, classify each candidate against the concept you asked for, then constrain it against the rules — turning _looks similar_ into _actually fits_.

## How it works

1. **Understand.** Match query phrases to SKOS labels, and spot the [target device](term:target-device) you own.
2. **Expand.** Add [synonyms](term:alt-label) and [narrower concepts](term:broader-narrower), then run hybrid search again.
3. **Classify.** Is each candidate in the concept you asked for?
4. **Constrain.** Check the class-level rules against your device. Failures are flagged and moved down, never removed. With no device, the rules check what your query asks for ("USB-C", "65W") instead, and each product says which catalogue devices it fits.

All four steps read the same taxonomy: plain SKOS in `domain-ontology.ttl`. This is the start of the Power branch:

```turtle
ex:Power a skos:Concept ; skos:topConceptOf ex:Taxonomy ;
    skos:notation "power" ;
    skos:prefLabel "Power"@en .

ex:Chargers a skos:Concept ; skos:broader ex:Power ;
    skos:notation "chargers" ;
    skos:prefLabel "Chargers"@en , "Cargadores"@es ;
    skos:altLabel "power adapter"@en , "power brick"@en , "cargador"@es ;
    skos:definition "A device that supplies electrical power to run or recharge another device."@en .

ex:LaptopChargers a skos:Concept ; skos:broader ex:Chargers ;
    skos:notation "laptop-chargers" ;
    skos:prefLabel "Laptop chargers"@en .
```


## What to look for

**GQ-03**: the 45W barrel charger was **#2 in Hybrid**. Here it is flagged, with the two checks it failed: its plug and its wattage.

## Strength

It understands _compatible_, not just _similar_. The rules are data in a Turtle file, so a new product needs no new code, and a Spanish query (**GQ-07**) finds English chargers through the ontology's labels. That file is also a release artefact in its own right: one `domain-ontology.ttl`, versioned and shipped on its own schedule, is a domain model several systems can share — a catalogue importer, a recommendations service, another team's tool — without agreeing on anything beyond the file, which matters once "the domain model" has to outlive one API.

The **Filters** panel is built from this same file, through `GET /api/taxonomy`: `prefLabel` is the checkbox label, `skos:broader` nests the tree (ticking _Chargers_ includes _Laptop chargers_), `skos:definition` is the tooltip, and `skos:notation` is the value the filter sends. No category is hard-coded in the UI.

## Failure mode

Only as good as the [ontology](term:ontology). Labels match words exactly ("brick" alone isn't "power brick"), and a missing spec gives **Unknown**, never a guess.

## Try this

Turn off **Apply constraints** and watch the barrel charger climb back up. Then choose **GQ-02** and turn off **Expand synonyms**: "power brick" loses its keyword matches. Choose **GQ-09** with no device: "65W USB-C" is enough to flag the barrel charger.

## Read the decision

[ADR-0013 · Ontology, vocabularies and domain rules](adr:0013-domain-ontology-and-compatibility)

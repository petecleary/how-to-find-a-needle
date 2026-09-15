## What it is

Search that knows the domain. A [SKOS](term:skos) [taxonomy](term:taxonomy) names the categories, their synonyms in several languages and the allowed spec values. One step beyond SKOS, a few [domain rules](term:domain-rule) say what fits what.

## How it works

1. **Understand.** Match query phrases to SKOS labels, and spot the [target device](term:target-device) you own.
2. **Expand.** Add [synonyms](term:alt-label) and [narrower concepts](term:broader-narrower), then run hybrid search again.
3. **Classify.** Is each candidate in the concept you asked for?
4. **Constrain.** Check the class-level rules against your device. Failures are flagged and moved down, never removed.

## What to look for

**GQ-01**: the 45W barrel charger was **#2 in Hybrid**. Here it is flagged, with the two checks it failed: its plug and its wattage.

## Strength

It understands _compatible_, not just _similar_. The rules are data in a Turtle file, so a new product needs no new code, and a Spanish query (**GQ-07**) finds English chargers through the ontology's labels.

## Failure mode

Only as good as the [ontology](term:ontology). Labels match words exactly ("brick" alone isn't "power brick"), and a missing spec gives **Unknown**, never a guess.

## Try this

Turn off **Apply constraints** and watch the barrel charger climb back up. Then choose **GQ-02** and turn off **Expand synonyms**: "power brick" loses its keyword matches.

## Read the decision

[ADR-0013 · Ontology, vocabularies and domain rules](adr:0013-domain-ontology-and-compatibility)

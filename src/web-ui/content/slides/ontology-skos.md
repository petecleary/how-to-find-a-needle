- **prefLabel** is the preferred name; **[altLabel](term:alt-label)** holds its synonyms.
- Language tags: one concept, labels in any language.
- **[broader / narrower](term:broader-narrower):** a hierarchy, where a concept may have two parents.
- **related:** associations that aren't parent and child.

```turtle
ex:Chargers a skos:Concept ;
    skos:prefLabel "Chargers"@en , "Cargadores"@es ;
    skos:altLabel  "power adapter"@en , "power brick"@en , "cargador"@es ;
    skos:broader   ex:Power .
```

using PI.SearchApi.Contracts;

namespace PI.SearchApi.Pipeline.Ontology;

// Stage 6, step 4 — Classify: in or out of concept
//
// What:     A candidate is InConcept if any of its categories is a concept the query named, or
//           narrower than one (skos:broader*). Otherwise it's OutOfConcept. If the query named
//           no category, every candidate is NoConcept.
// Strength: Fixes the keyword trap with structure, not wording: a cordless *phone* battery sits
//           under Telephony, not Power tools › Batteries, however many words it shares.
// Failure:  Only as good as the categories products are filed under, and only when the query
//           actually names a category the label matcher recognises.
// Decision: docs/adr/0013-domain-ontology-and-compatibility.md
public sealed class ConceptClassifier(IOntology ontology)
{
    public ConceptClassification Classify(IReadOnlyList<string> categories, IReadOnlyList<string> matchedConcepts)
    {
        if (matchedConcepts.Count == 0)
        {
            return new ConceptClassification(ConceptMatch.NoConcept, null, []);
        }

        foreach (var category in categories)
        {
            foreach (var concept in matchedConcepts)
            {
                if (ontology.IsNarrowerOrSelf(category, concept))
                {
                    // The chain that justifies it, e.g. laptop-chargers → chargers.
                    var fullChain = ontology.BroaderChain(category);
                    var chain = fullChain.Take(fullChain.ToList().IndexOf(concept) + 1).ToList();

                    return new ConceptClassification(ConceptMatch.InConcept, concept, chain);
                }
            }
        }

        return new ConceptClassification(ConceptMatch.OutOfConcept, null, []);
    }
}

/// <summary>The classification of one candidate, with the broader chain that justified an InConcept result.</summary>
public sealed record ConceptClassification(ConceptMatch Match, string? MatchedConcept, IReadOnlyList<string> Chain);

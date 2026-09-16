using PI.SearchApi.Pipeline.Keyword;

namespace PI.SearchApi.Pipeline.Ontology;

// Stage 5, step 2 — Expand: synonyms and narrower concepts
//
// What:     For each phrase that named a wanted category, gathers that concept's labels and the
//           labels of every concept beneath it (capped at 10 terms per concept). Keyword search gets
//           them as OR groups; vector search gets the query, minus any device name, with the
//           concepts' names appended.
// Strength: Improves recall with knowledge instead of guesses: "power brick" also searches for
//           "AC adapter" and "laptop chargers", because the ontology lists them as the same thing.
// Failure:  Expansion only knows what someone wrote down. Too many terms dilutes precision, which
//           is why the term list is capped and the classify step follows.
// Decision: docs/decisions/0013-domain-ontology-and-compatibility.md
public sealed class QueryExpander(IOntology ontology)
{
    /// <summary>ADR-0013: at most 10 terms per matched concept.</summary>
    public const int MaxTermsPerConcept = 10;

    public QueryExpansion Expand(QueryUnderstanding understanding)
    {
        var groups = new List<ExpandedPhrase>();

        foreach (var match in understanding.Matches.Where(understanding.IsWanted))
        {
            var concepts = match.TaxonomyConcepts.Where(c => understanding.WantedConcepts.Contains(c)).ToList();
            var terms = new List<string>();

            foreach (var concept in concepts)
            {
                foreach (var term in TermsFor(match.Phrase, concept))
                {
                    if (!terms.Contains(term, StringComparer.OrdinalIgnoreCase))
                    {
                        terms.Add(term);
                    }
                }
            }

            groups.Add(new ExpandedPhrase(match.Phrase, concepts, terms));
        }

        var keyword = new KeywordExpansion([.. groups.Select(g => g.Terms)], understanding.RemainingText);

        // Vector side: the query without the device name, with the wanted concepts' English names (and their
        // narrower concepts') appended, e.g. "power brick for laptop (chargers, laptop chargers, …, laptops)".
        var conceptNames = understanding.WantedConcepts
            .SelectMany(ConceptAndNarrower)
            .Select(concept => ontology.TryGetConcept(concept, out var c) ? c.PrefLabels.GetValueOrDefault("en") : null)
            .OfType<string>()
            .Select(name => name.ToLowerInvariant())
            .Distinct()
            .ToList();

        var baseText = string.IsNullOrWhiteSpace(understanding.QueryWithoutDevice)
            ? understanding.Query // the query was only a device name: nothing else to embed
            : understanding.QueryWithoutDevice;

        var embeddingText = conceptNames.Count == 0
            ? baseText
            : $"{baseText} ({string.Join(", ", conceptNames)})";

        return new QueryExpansion(groups, keyword, embeddingText);
    }

    private IEnumerable<string> TermsFor(string phrase, string conceptNotation)
    {
        var concepts = ConceptAndNarrower(conceptNotation).ToList();

        var labels = ontology.Labels
            .Where(l => l.IsTaxonomyConcept && l.Kind != LabelKind.Hidden && concepts.Contains(l.ConceptNotation))
            .ToList();

        // Order before the cap (ADR-0013): the phrase itself, then English labels (concept first, then
        // narrower concepts; preferred before alternative), then labels in other languages.
        var english = concepts.SelectMany(c => labels
            .Where(l => l.ConceptNotation == c && l.Language == "en")
            .OrderBy(l => l.Kind));
        var otherLanguages = concepts.SelectMany(c => labels
            .Where(l => l.ConceptNotation == c && l.Language != "en")
            .OrderBy(l => l.Kind));

        return new[] { phrase }
            .Concat(english.Select(l => l.Label))
            .Concat(otherLanguages.Select(l => l.Label))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(MaxTermsPerConcept);
    }

    // The concept first, then the concepts beneath it in a stable order.
    private IEnumerable<string> ConceptAndNarrower(string notation) =>
        new[] { notation }.Concat(ontology.NarrowerOrSelf(notation).Where(n => n != notation).Order(StringComparer.Ordinal));
}

/// <summary>Stage 5's rewritten query for both retrievers, plus what was expanded (for the trace).</summary>
public sealed record QueryExpansion(IReadOnlyList<ExpandedPhrase> Phrases, KeywordExpansion Keyword, string EmbeddingText);

/// <summary>One matched phrase and the terms it expanded to.</summary>
public sealed record ExpandedPhrase(string Phrase, IReadOnlyList<string> Concepts, IReadOnlyList<string> Terms);

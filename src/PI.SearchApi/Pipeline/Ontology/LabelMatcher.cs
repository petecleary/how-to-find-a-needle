namespace PI.SearchApi.Pipeline.Ontology;

// Stage 6, step 1 — Understand: match query phrases to SKOS labels
//
// What:     Looks up every 1–3 word phrase of the query in an index of every ontology label
//           (preferred, alternative and hidden; every language). Longest phrases claim their
//           words first, and no word belongs to two matches.
// Strength: Deterministic and inspectable: the trace shows exactly which phrase matched which
//           label. A Spanish "cargador" finds Chargers because the ontology says so, not a model.
// Failure:  Purely lexical. "brick" alone won't match "power brick", and an unlisted synonym
//           matches nothing. Entity recognition is the next step up; an LLM is the black box
//           this talk argues against for this job.
// Decision: docs/adr/0013-domain-ontology-and-compatibility.md
public sealed class LabelMatcher
{
    /// <summary>ADR-0013: phrases of 1–3 words.</summary>
    public const int MaxPhraseWords = 3;

    // folded phrase ("power brick") → every label with that folded spelling, across concepts.
    private readonly Dictionary<string, List<ConceptLabel>> _labelsByPhrase;

    public LabelMatcher(IOntology ontology)
    {
        _labelsByPhrase = ontology.Labels
            .Select(label => (Key: TextNormaliser.FoldPhrase(label.Label), Label: label))
            .Where(entry => entry.Key.Length > 0 && entry.Key.Split(' ').Length <= MaxPhraseWords)
            .GroupBy(entry => entry.Key)
            .ToDictionary(group => group.Key, group => group.Select(entry => entry.Label).ToList());
    }

    public QueryUnderstanding Understand(string query)
    {
        var tokens = TextNormaliser.Tokenise(query);
        var claimed = new bool[tokens.Count];
        var matches = new List<LabelMatch>();

        // Longest first: "cordless phone battery" must claim "battery" before the one-word
        // label "battery" (→ Batteries) gets a chance to.
        for (var size = Math.Min(MaxPhraseWords, tokens.Count); size >= 1; size--)
        {
            for (var start = 0; start + size <= tokens.Count; start++)
            {
                if (claimed.AsSpan(start, size).Contains(true))
                {
                    continue;
                }

                var span = tokens.Skip(start).Take(size).ToList();
                var key = string.Join(' ', span.Select(t => t.Folded));

                if (!_labelsByPhrase.TryGetValue(key, out var labels))
                {
                    continue;
                }

                claimed.AsSpan(start, size).Fill(true);
                matches.Add(new LabelMatch(string.Join(' ', span.Select(t => t.Original)), start, size, labels));
            }
        }

        matches.Sort((a, b) => a.TokenStart.CompareTo(b.TokenStart));

        return new QueryUnderstanding(query, tokens, matches);
    }
}

/// <summary>A query phrase and every ontology label it matched.</summary>
/// <param name="Phrase">The words as typed, e.g. "power brick".</param>
public sealed record LabelMatch(string Phrase, int TokenStart, int TokenCount, IReadOnlyList<ConceptLabel> Labels)
{
    /// <summary>Taxonomy concept notations this phrase names (categories), e.g. "chargers".</summary>
    public IReadOnlyList<string> TaxonomyConcepts =>
        [.. Labels.Where(l => l.IsTaxonomyConcept).Select(l => l.ConceptNotation).Distinct()];

    /// <summary>True when the phrase names at least one product category (not just a spec value).</summary>
    public bool IsTaxonomyMatch => Labels.Any(l => l.IsTaxonomyConcept);
}

/// <summary>What Stage 6 understood about a query.</summary>
public sealed record QueryUnderstanding(string Query, IReadOnlyList<Token> Tokens, IReadOnlyList<LabelMatch> Matches)
{
    /// <summary>Every taxonomy concept matched anywhere in the query, in query order.</summary>
    public IReadOnlyList<string> TaxonomyConcepts => [.. Matches.SelectMany(m => m.TaxonomyConcepts).Distinct()];

    /// <summary>
    /// The query with taxonomy-matched phrases removed, e.g. "power brick for laptop" → "for". Value
    /// phrases like "USB-C" stay in (ADR-0013): they aren't expanded, so keyword search still needs them.
    /// </summary>
    public string RemainingText
    {
        get
        {
            var covered = new bool[Tokens.Count];

            foreach (var match in Matches.Where(m => m.IsTaxonomyMatch))
            {
                covered.AsSpan(match.TokenStart, match.TokenCount).Fill(true);
            }

            return string.Join(' ', Tokens.Where((_, i) => !covered[i]).Select(t => t.Original));
        }
    }
}

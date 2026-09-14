namespace PI.SearchApi.Pipeline.Ontology;

// Stage 6, step 1 — Understand: match query phrases to SKOS labels
//
// What:     Looks up every 1–3 word phrase of the query in an index of every ontology label
//           (preferred, alternative and hidden; every language). Longest phrases claim their
//           words first, and no word belongs to two matches. Words naming the target device are
//           claimed before matching even starts: they are context, not something to look for.
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

    /// <param name="deviceMention">Tokens naming the target device; they are never matched against labels.</param>
    public QueryUnderstanding Understand(string query, TokenSpan? deviceMention = null)
    {
        var tokens = TextNormaliser.Tokenise(query);
        var claimed = new bool[tokens.Count];
        var matches = new List<LabelMatch>();

        // "Brakk 18V Combi Drill" contains the labels "Brakk 18V" and "combi drill", but as a device name it
        // describes what the shopper owns. Claiming it first stops those words matching anything.
        if (deviceMention is { } mention)
        {
            claimed.AsSpan(mention.Start, mention.Count).Fill(true);
        }

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

        return new QueryUnderstanding(query, tokens, matches) { DeviceMention = deviceMention };
    }
}

/// <summary>A query phrase and every ontology label it matched.</summary>
/// <param name="Phrase">The words as typed, e.g. "power brick".</param>
public sealed record LabelMatch(string Phrase, int TokenStart, int TokenCount, IReadOnlyList<ConceptLabel> Labels)
{
    /// <summary>Taxonomy concept notations this phrase names (categories), e.g. "chargers".</summary>
    public IReadOnlyList<string> TaxonomyConcepts =>
        [.. Labels.Where(l => l.IsTaxonomyConcept).Select(l => l.ConceptNotation).Distinct()];
}

/// <summary>What Stage 6 understood about a query: what the shopper wants, and what is only context.</summary>
public sealed record QueryUnderstanding(string Query, IReadOnlyList<Token> Tokens, IReadOnlyList<LabelMatch> Matches)
{
    /// <summary>The tokens that name the target device, if the query names it.</summary>
    public TokenSpan? DeviceMention { get; init; }

    /// <summary>
    /// Matched device-type concepts the target device belongs to, e.g. "laptops" in "charger for my laptop"
    /// when the device is a laptop. They describe what the shopper owns, so they aren't expanded or classified against.
    /// </summary>
    public IReadOnlyList<string> ContextConcepts { get; init; } = [];

    /// <summary>Every taxonomy concept matched anywhere in the query, in query order.</summary>
    public IReadOnlyList<string> TaxonomyConcepts => [.. Matches.SelectMany(m => m.TaxonomyConcepts).Distinct()];

    /// <summary>The categories the shopper is looking for: matched taxonomy concepts that aren't context.</summary>
    public IReadOnlyList<string> WantedConcepts => [.. TaxonomyConcepts.Where(c => !ContextConcepts.Contains(c))];

    /// <summary>The device name as typed, e.g. "Blackbird Aerobook 14"; null if the query doesn't name the device.</summary>
    public string? DeviceMentionText =>
        DeviceMention is { } mention ? string.Join(' ', Tokens.Skip(mention.Start).Take(mention.Count).Select(t => t.Original)) : null;

    /// <summary>True when the phrase names at least one wanted category.</summary>
    public bool IsWanted(LabelMatch match) => match.TaxonomyConcepts.Any(c => !ContextConcepts.Contains(c));

    /// <summary>The query with the device name removed: what vector search should embed. "charger for my Blackbird Aerobook 14" → "charger for my".</summary>
    public string QueryWithoutDevice => JoinTokens(IsDeviceToken);

    /// <summary>
    /// The query with the device name and wanted-category phrases removed: what keyword search AND-s with the
    /// expanded OR groups. "power brick for laptop" → "for". Value phrases ("USB-C") and context concepts ("laptop"
    /// when you own one) stay in: they aren't expanded, so keyword search still needs them as typed.
    /// </summary>
    public string RemainingText
    {
        get
        {
            var wanted = new bool[Tokens.Count];

            foreach (var match in Matches.Where(IsWanted))
            {
                wanted.AsSpan(match.TokenStart, match.TokenCount).Fill(true);
            }

            return JoinTokens(i => wanted[i] || IsDeviceToken(i));
        }
    }

    private bool IsDeviceToken(int index) =>
        DeviceMention is { } mention && index >= mention.Start && index < mention.Start + mention.Count;

    private string JoinTokens(Func<int, bool> exclude) =>
        string.Join(' ', Tokens.Where((_, i) => !exclude(i)).Select(t => t.Original));
}

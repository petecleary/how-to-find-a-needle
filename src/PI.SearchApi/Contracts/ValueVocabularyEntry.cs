namespace PI.SearchApi.Contracts;

/// <summary>One allowed value in a <see cref="ValueVocabulary"/>, e.g. USB-C.</summary>
public sealed record ValueVocabularyEntry
{
    /// <summary>The value products store in their specs and a filter sends, e.g. "usb-c".</summary>
    public required string Notation { get; init; }

    /// <summary>The English preferred label, e.g. "USB-C".</summary>
    public required string Label { get; init; }

    /// <summary>Preferred labels by language tag.</summary>
    public required IReadOnlyDictionary<string, string> Labels { get; init; }

    /// <summary>Synonyms (skos:altLabel), e.g. "Type-C". Hidden labels (misspellings) are never included.</summary>
    public required IReadOnlyList<string> AltLabels { get; init; }
}

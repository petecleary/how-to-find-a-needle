namespace PI.SearchApi.Contracts;

/// <summary>How a candidate's categories relate to the concepts Stage 6 found in the query (ADR-0013).</summary>
public enum ConceptMatch
{
    /// <summary>One of its categories is a matched concept, or narrower than one.</summary>
    InConcept,

    /// <summary>Concepts were matched, and none of its categories falls under them.</summary>
    OutOfConcept,

    /// <summary>The query matched no taxonomy concept, so there is nothing to classify against.</summary>
    NoConcept,
}

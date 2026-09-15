namespace PI.SearchApi.Pipeline.Rag;

/// <summary>The product IDs a text cites, and which of them weren't in the evidence set.</summary>
public sealed record CitationCheck(IReadOnlyList<string> Citations, IReadOnlyList<string> UnknownIds);

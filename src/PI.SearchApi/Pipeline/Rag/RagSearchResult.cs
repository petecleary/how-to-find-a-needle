namespace PI.SearchApi.Pipeline.Rag;

/// <summary>The results and trace for the JSON response, plus the evidence set an answer will be generated from.</summary>
public sealed record RagSearchResult(StageResult Result, EvidenceSet Evidence);

namespace PI.SearchApi.Pipeline.Rag;

/// <summary>One check run on generated text, for the trace.</summary>
/// <param name="IsHeuristic">True when the check guesses from wording rather than proving something, so the trace can say so.</param>
public sealed record ValidationCheck(string Name, bool Passed, string Detail, bool IsHeuristic = false);

namespace PI.SearchApi.Contracts;

/// <summary>
/// The sections an answer stream writes, in order (ADR-0016, ADR-0017). Stage 6 writes only the answer;
/// Stage 7 writes the answer, then the explanation.
/// </summary>
public static class AnswerSections
{
    /// <summary>The grounded, cited summary.</summary>
    public const string Answer = "answer";

    /// <summary>Stage 7's audience-aware explanation of that answer.</summary>
    public const string Explanation = "explanation";
}

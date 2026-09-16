namespace PI.SearchApi.Pipeline.Pedagogy;

/// <summary>One section of an explanation: its heading as written, the canonical heading it matched (if any) and its text.</summary>
/// <param name="Heading">The canonical heading ("Near miss"), or the heading as written when it isn't one of the five.</param>
public sealed record ExplanationSection(string Heading, bool IsKnown, string Body);

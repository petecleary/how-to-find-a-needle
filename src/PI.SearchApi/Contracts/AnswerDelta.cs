namespace PI.SearchApi.Contracts;

/// <summary>One chunk of streamed markdown, shown as it arrives. It hasn't been validated yet: that happens in <see cref="AnswerFinal"/>.</summary>
public sealed record AnswerDelta
{
    /// <summary><c>answer</c> or <c>explanation</c> (<see cref="AnswerSections"/>).</summary>
    public required string Section { get; init; }

    public required string Text { get; init; }
}

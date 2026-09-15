namespace PI.SearchApi.Contracts;

/// <summary>One failed validation rule in a <see cref="ValidationProblem"/>: the request field and why it failed.</summary>
public sealed record ValidationProblemError(string Name, string Reason);

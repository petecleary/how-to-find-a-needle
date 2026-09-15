namespace PI.SearchApi.Contracts;

/// <summary>The product a section of the explanation cites; <see cref="ProductId"/> is null when it cites none.</summary>
public sealed record ExplanationProduct(string? ProductId);

using System.Text.Json;

namespace PI.SearchApi.Pipeline;

/// <summary>
/// The product fields every stage reads back from the database: enough to display a result and
/// to evaluate Stage 5's rules (which compare specs), without the long description or reviews.
/// </summary>
public sealed record ProductSummary(
    string Id,
    string Name,
    string Brand,
    IReadOnlyList<string> Categories,
    decimal Price,
    IReadOnlyDictionary<string, JsonElement> Specs);

using System.Text.Json.Serialization;

namespace PI.SearchApi.Data;

/// <summary>
/// One product as authored in <c>products.json</c> (ADR-0005). <see cref="Specs"/> keeps its
/// raw JSON shape because spec names and value types vary by product type — the catalog
/// validation tests and the seeder read out of it by name.
/// </summary>
public sealed record CatalogProduct(
    string Id,
    string Name,
    string Brand,
    IReadOnlyList<string> Categories,
    decimal Price,
    string Currency,
    string Description,
    IReadOnlyList<string> Reviews,
    IReadOnlyDictionary<string, System.Text.Json.JsonElement> Specs);

/// <summary>The top-level shape of <c>products.json</c>: a schema reference plus the catalog.</summary>
public sealed record ProductCatalog(
    [property: JsonPropertyName("$schema")] string? Schema,
    IReadOnlyList<CatalogProduct> Products);

using System.Text.Json;
using System.Text.Json.Serialization;

namespace PI.SearchApi.Data;

/// <summary>
/// Reads <c>products.json</c> and <c>golden-queries.json</c> with strict deserialisation, so a
/// typo in either file fails loudly instead of silently dropping a field. Used by the seeder
/// and by the catalog validation tests (tests/PI.SearchApi.CLAUDE.md).
/// </summary>
public static class CatalogLoader
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    };

    public static IReadOnlyList<CatalogProduct> LoadProducts(string dataDirectory)
    {
        var path = Path.Combine(dataDirectory, "products.json");
        var json = File.ReadAllText(path);
        var catalog = JsonSerializer.Deserialize<ProductCatalog>(json, Options)
            ?? throw new InvalidOperationException($"{path} deserialised to null.");

        return catalog.Products;
    }

    public static IReadOnlyList<GoldenQuery> LoadGoldenQueries(string dataDirectory)
    {
        var path = Path.Combine(dataDirectory, "golden-queries.json");
        var json = File.ReadAllText(path);

        return JsonSerializer.Deserialize<IReadOnlyList<GoldenQuery>>(json, Options)
            ?? throw new InvalidOperationException($"{path} deserialised to null.");
    }
}

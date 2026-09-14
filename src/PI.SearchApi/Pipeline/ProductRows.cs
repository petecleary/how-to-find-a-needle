using System.Text.Json;
using Npgsql;

namespace PI.SearchApi.Pipeline;

/// <summary>
/// The product columns every stage selects, and how to read them back. Sharing one column list
/// means every stage returns products in the same shape.
/// </summary>
public static class ProductRows
{
    /// <summary>Columns for <see cref="ProductSummary"/>. specs is read as text and parsed here.</summary>
    public const string Columns = "id, name, brand, categories, price, specs::text AS specs";

    public static ProductSummary Read(NpgsqlDataReader reader)
    {
        var specsJson = reader.GetString(reader.GetOrdinal("specs"));
        var specs = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(specsJson) ?? [];

        return new ProductSummary(
            reader.GetString(reader.GetOrdinal("id")),
            reader.GetString(reader.GetOrdinal("name")),
            reader.GetString(reader.GetOrdinal("brand")),
            reader.GetFieldValue<string[]>(reader.GetOrdinal("categories")),
            reader.GetDecimal(reader.GetOrdinal("price")),
            specs);
    }
}

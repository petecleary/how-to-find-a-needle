using Npgsql;
using PI.SearchApi.Pipeline.Ontology;

namespace PI.SearchApi.Pipeline;

/// <summary>
/// Reads specific products by ID, and the products that can be a target device. Used by the demo
/// endpoints and by Stage 5, which needs the target device's specs to check domain rules.
/// </summary>
public sealed class ProductLookup(NpgsqlDataSource dataSource, IOntology ontology)
{
    private const string ByIdSql = $"""
        SELECT {ProductRows.Columns}
        FROM products
        WHERE id = @id;
        """;

    // A product is a device when one of its categories is, or is narrower than, a concept marked
    // ex:isDeviceType true in the ontology (laptops, drills). The ontology decides; SQL just filters.
    private const string DevicesSql = $"""
        SELECT {ProductRows.Columns}
        FROM products
        WHERE categories && @deviceCategories
        ORDER BY name, id;
        """;

    public async Task<ProductSummary?> GetByIdAsync(string id, CancellationToken ct)
    {
        await using var command = dataSource.CreateCommand(ByIdSql);
        command.Parameters.AddWithValue("id", id);

        await using var reader = await command.ExecuteReaderAsync(ct);
        return await reader.ReadAsync(ct) ? ProductRows.Read(reader) : null;
    }

    public async Task<IReadOnlyList<ProductSummary>> GetDevicesAsync(CancellationToken ct)
    {
        var deviceCategories = ontology.Concepts
            .Where(c => c.IsDeviceType)
            .SelectMany(c => ontology.NarrowerOrSelf(c.Notation))
            .Distinct()
            .ToArray();

        await using var command = dataSource.CreateCommand(DevicesSql);
        command.Parameters.AddWithValue("deviceCategories", deviceCategories);

        var devices = new List<ProductSummary>();
        await using var reader = await command.ExecuteReaderAsync(ct);

        while (await reader.ReadAsync(ct))
        {
            devices.Add(ProductRows.Read(reader));
        }

        return devices;
    }
}

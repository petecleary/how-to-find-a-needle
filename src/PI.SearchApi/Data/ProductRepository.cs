using Npgsql;
using PI.SearchApi.Models.Products;

namespace PI.SearchApi.Data;

public sealed class ProductRepository(string connectionString) : IProductRepository
{
    private const string SelectProductsSql = @"
        SELECT
            id,
            name,
            brand,
            manufacturer,
            categories,
            primary_categories,
            price_min,
            price_max,
            currency,
            merchant,
            image_urls
        FROM products
        ORDER BY id
        LIMIT @take OFFSET @skip;";

    public async Task<IReadOnlyList<Product>> GetProductsAsync(int page, int pageSize, CancellationToken cancellationToken = default)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new NpgsqlCommand(SelectProductsSql, connection);
        command.Parameters.AddWithValue("@take", pageSize);
        command.Parameters.AddWithValue("@skip", (page - 1) * pageSize);

        var products = new List<Product>();

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            products.Add(new Product(
                Id: reader.GetString(0),
                Name: reader.GetString(1),
                Brand: reader.IsDBNull(2) ? null : reader.GetString(2),
                Manufacturer: reader.IsDBNull(3) ? null : reader.GetString(3),
                Categories: reader.IsDBNull(4) ? null : reader.GetString(4),
                PrimaryCategories: reader.IsDBNull(5) ? null : reader.GetString(5),
                PriceMin: reader.IsDBNull(6) ? null : reader.GetDecimal(6),
                PriceMax: reader.IsDBNull(7) ? null : reader.GetDecimal(7),
                Currency: reader.IsDBNull(8) ? null : reader.GetString(8),
                Merchant: reader.IsDBNull(9) ? null : reader.GetString(9),
                ImageUrls: reader.IsDBNull(10) ? null : reader.GetString(10)));
        }

        return products;
    }
}

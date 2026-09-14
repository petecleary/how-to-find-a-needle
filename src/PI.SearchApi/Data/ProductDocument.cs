using System.Security.Cryptography;
using System.Text;

namespace PI.SearchApi.Data;

/// <summary>
/// Builds the text that gets embedded for a product, and hashes it. "Embeddings are a cache of
/// your content" (ADR-0006): <see cref="ContentHash"/> is what the seeder compares to decide
/// whether a product's vectors are still valid, or need re-embedding.
/// </summary>
public static class ProductDocument
{
    /// <summary>
    /// The exact text Stage 3+ embeds for a product (ADR-0009): name, brand, categories,
    /// description and reviews, in that order. Price and specs are deliberately excluded —
    /// they're compared structurally (Stage 1) and by rule (Stage 5), not by meaning.
    /// </summary>
    public static string BuildText(CatalogProduct product)
    {
        var categories = string.Join(' ', product.Categories);
        var reviews = string.Join(' ', product.Reviews);

        return $"{product.Name}. {product.Brand} {categories}. {product.Description} Reviews: {reviews}";
    }

    /// <summary>Lowercase hex SHA-256 of <see cref="BuildText"/>.</summary>
    public static string ContentHash(CatalogProduct product)
    {
        var bytes = Encoding.UTF8.GetBytes(BuildText(product));
        var hash = SHA256.HashData(bytes);

        return Convert.ToHexStringLower(hash);
    }
}

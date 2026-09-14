using System.Text.Json;
using PI.SearchApi.Data;
using Xunit;

namespace PI.SearchApi.Tests.Data;

public sealed class ProductDocumentTests
{
    private static CatalogProduct MakeProduct(
        string description = "A charger.", decimal price = 49.99m, string specsJson = "{}") =>
        new(
            "PROD-0001",
            "Voltline 65W USB-C GaN Charger",
            "Voltline",
            ["laptop-chargers"],
            price,
            "GBP",
            description,
            ["Charges fast."],
            JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(specsJson)!);

    [Fact]
    public void ContentHash_SameInput_IsStable()
    {
        var product = MakeProduct();

        var first = ProductDocument.ContentHash(product);
        var second = ProductDocument.ContentHash(product);

        Assert.Equal(first, second);
    }

    [Fact]
    public void ContentHash_DescriptionChanges_Changes()
    {
        var original = MakeProduct(description: "A 65W USB-C charger.");
        var edited = MakeProduct(description: "A 100W USB-C charger.");

        Assert.NotEqual(ProductDocument.ContentHash(original), ProductDocument.ContentHash(edited));
    }

    [Fact]
    public void ContentHash_PriceChanges_DoesNotChange()
    {
        // Price isn't part of the embedded text (ADR-0009), so a price-only edit must not
        // force a re-embed: the seeder relies on this to only null vectors that actually
        // went stale.
        var original = MakeProduct(price: 49.99m);
        var repriced = MakeProduct(price: 39.99m);

        Assert.Equal(ProductDocument.ContentHash(original), ProductDocument.ContentHash(repriced));
    }

    [Fact]
    public void ContentHash_SpecsChange_DoesNotChange()
    {
        var original = MakeProduct(specsJson: """{"wattageW": 65}""");
        var respecced = MakeProduct(specsJson: """{"wattageW": 100}""");

        Assert.Equal(ProductDocument.ContentHash(original), ProductDocument.ContentHash(respecced));
    }

    [Fact]
    public void BuildText_IncludesNameBrandCategoriesDescriptionAndReviews()
    {
        var product = MakeProduct();

        var text = ProductDocument.BuildText(product);

        Assert.Contains(product.Name, text);
        Assert.Contains(product.Brand, text);
        Assert.Contains("laptop-chargers", text);
        Assert.Contains(product.Description, text);
        Assert.Contains("Charges fast.", text);
    }
}

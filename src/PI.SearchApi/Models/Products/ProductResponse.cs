namespace PI.SearchApi.Models.Products;

public sealed record ProductResponse(
    string Id,
    string Name,
    string? Brand,
    string? Manufacturer,
    string? Categories,
    string? PrimaryCategories,
    decimal? PriceMin,
    decimal? PriceMax,
    string? Currency,
    string? Merchant,
    string? ImageUrls);

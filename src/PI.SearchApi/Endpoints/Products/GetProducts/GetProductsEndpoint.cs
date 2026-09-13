using FastEndpoints;
using FluentValidation;
using PI.SearchApi.Data;
using PI.SearchApi.Models.Products;

namespace PI.SearchApi.Endpoints.Products.GetProducts;

public sealed class GetProductsRequest
{
    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 20;
}

public sealed class GetProductsRequestValidator : Validator<GetProductsRequest>
{
    public GetProductsRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}

public sealed class GetProductsEndpoint(IProductRepository productRepository) : Endpoint<GetProductsRequest, List<ProductResponse>>
{
    public override void Configure()
    {
        Get("/api/products");
        AllowAnonymous();
    }

    public override async Task HandleAsync(GetProductsRequest req, CancellationToken ct)
    {
        var products = await productRepository.GetProductsAsync(req.Page, req.PageSize, ct);

        var response = products
            .Select(p => new ProductResponse(
                p.Id,
                p.Name,
                p.Brand,
                p.Manufacturer,
                p.Categories,
                p.PrimaryCategories,
                p.PriceMin,
                p.PriceMax,
                p.Currency,
                p.Merchant,
                p.ImageUrls))
            .ToList();

        await Send.OkAsync(response, ct);
    }
}

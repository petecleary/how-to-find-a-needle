using PI.SearchApi.Models.Products;

namespace PI.SearchApi.Data;

public interface IProductRepository
{
    Task<IReadOnlyList<Product>> GetProductsAsync(int page, int pageSize, CancellationToken cancellationToken = default);
}

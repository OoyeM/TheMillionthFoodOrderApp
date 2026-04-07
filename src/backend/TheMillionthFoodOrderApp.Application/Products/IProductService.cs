namespace TheMillionthFoodOrderApp.Application.Products;

public interface IProductService
{
    Task<ProductResponse> CreateProductAsync(CreateProductRequest request, CancellationToken cancellationToken = default);
    Task<ProductResponse> UpdateProductAsync(Guid id, UpdateProductRequest request, CancellationToken cancellationToken = default);
    Task<ProductResponse> CreateComboProductAsync(CreateComboProductRequest request, CancellationToken cancellationToken = default);
    Task<ProductResponse> UpdateComboProductAsync(Guid id, UpdateComboProductRequest request, CancellationToken cancellationToken = default);
    Task DeleteProductAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ProductResponse> GetProductAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProductListItemResponse>> GetProductsAsync(CancellationToken cancellationToken = default);
}

namespace TheMillionthFoodOrderApp.Domain.Products;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(Product product, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads the product with <paramref name="id"/> (including translations),
    /// applies <paramref name="mutate"/>, and saves — all within a single tracked context.
    /// Returns the mutated product, or null if not found.
    /// </summary>
    Task<Product?> UpdateAsync(Guid id, Action<Product> mutate, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

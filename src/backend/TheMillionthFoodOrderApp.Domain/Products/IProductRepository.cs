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

    /// <summary>
    /// Loads the product with <paramref name="id"/> (without translations),
    /// applies <paramref name="mutate"/>, and saves. Use for scalar-only mutations (e.g. sort order).
    /// Returns the mutated product, or null if not found.
    /// </summary>
    Task<Product?> UpdateScalarAsync(Guid id, Action<Product> mutate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all products belonging to <paramref name="categoryId"/>,
    /// ordered by <see cref="Product.SortOrderInCategory"/> ascending, with translations included.
    /// </summary>
    Task<IReadOnlyList<Product>> GetByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the highest <see cref="Product.SortOrderInCategory"/> value among products
    /// in <paramref name="categoryId"/>, or -1 if the category has no products.
    /// </summary>
    Task<int> GetMaxSortOrderInCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all products whose IDs appear in <paramref name="ids"/>, with translations included.
    /// </summary>
    Task<IReadOnlyList<Product>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true if the product with <paramref name="productId"/> is referenced
    /// as a component in any combo product.
    /// </summary>
    Task<bool> IsComponentOfAnyComboAsync(Guid productId, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

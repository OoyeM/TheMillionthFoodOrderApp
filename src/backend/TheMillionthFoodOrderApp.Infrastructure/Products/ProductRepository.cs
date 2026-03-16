using Microsoft.EntityFrameworkCore;
using TheMillionthFoodOrderApp.Domain.Products;
using TheMillionthFoodOrderApp.Infrastructure.Persistence;

namespace TheMillionthFoodOrderApp.Infrastructure.Products;

/// <summary>
/// Brand-scoped product repository. Injects <see cref="BrandDbContext"/> directly
/// (registered as scoped via factory delegate in DI).
/// </summary>
public sealed class ProductRepository(BrandDbContext dbContext) : IProductRepository
{
    /// <inheritdoc/>
    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.Products
            .Include(p => p.Translations)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default)
        => await dbContext.Products
            .Include(p => p.Translations)
            .OrderBy(p => p.CreatedAt)
            .ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
        => await dbContext.Products.AddAsync(product, cancellationToken);

    /// <inheritdoc/>
    /// <remarks>
    /// Loads the entity with translations, applies <paramref name="mutate"/>, and saves in one call.
    /// Returns null if not found.
    /// </remarks>
    public async Task<Product?> UpdateAsync(Guid id, Action<Product> mutate, CancellationToken cancellationToken = default)
    {
        var product = await dbContext.Products
            .Include(p => p.Translations)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (product is null)
            return null;

        mutate(product);
        await dbContext.SaveChangesAsync(cancellationToken);
        return product;
    }

    /// <inheritdoc/>
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => await dbContext.SaveChangesAsync(cancellationToken);
}

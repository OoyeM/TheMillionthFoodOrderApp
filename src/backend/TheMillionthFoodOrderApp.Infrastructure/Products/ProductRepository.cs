using Microsoft.EntityFrameworkCore;
using TheMillionthFoodOrderApp.Domain.Products;
using TheMillionthFoodOrderApp.Infrastructure.Persistence;
using Wolverine;

namespace TheMillionthFoodOrderApp.Infrastructure.Products;

/// <summary>
/// Brand-scoped product repository. Injects <see cref="BrandDbContext"/> directly
/// (registered as scoped via factory delegate in DI).
/// </summary>
public sealed class ProductRepository(BrandDbContext dbContext, IMessageBus messageBus) : IProductRepository
{
    /// <inheritdoc/>
    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.Products
            .Include(p => p.Translations)
            .Include(p => p.ComboItems)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Product>> GetAllAsync(CancellationToken cancellationToken = default)
        => await dbContext.Products
            .Include(p => p.Translations)
            .Include(p => p.ComboItems)
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
        // Load product WITHOUT translations to avoid EF Core change tracking conflicts
        // when the domain method clears and re-adds the child collection.
        var product = await dbContext.Products
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (product is null)
            return null;

        // Wrap in an explicit transaction so the DELETE + INSERT are atomic.
        // ExecuteDeleteAsync commits immediately and bypasses the change tracker,
        // so without a transaction, a failure in mutate() or SaveChangesAsync()
        // would leave the product with zero translations.
        await dbContext.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            await dbContext.ProductTranslations
                .Where(t => t.ProductId == id)
                .ExecuteDeleteAsync(cancellationToken);

            await dbContext.ComboItems
                .Where(ci => ci.ComboProductId == id)
                .ExecuteDeleteAsync(cancellationToken);

            mutate(product);

            dbContext.ProductTranslations.AddRange(product.Translations);

            if (product.ComboItems.Count > 0)
                dbContext.ComboItems.AddRange(product.ComboItems);

            var events = DomainEventDispatcher.CollectAndClear(dbContext);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            await DomainEventDispatcher.PublishAsync(events, messageBus);
        });

        return product;
    }

    /// <inheritdoc/>
    public async Task<Product?> UpdateScalarAsync(Guid id, Action<Product> mutate, CancellationToken cancellationToken = default)
    {
        var product = await dbContext.Products
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (product is null)
            return null;

        mutate(product);

        var events = DomainEventDispatcher.CollectAndClear(dbContext);
        await dbContext.SaveChangesAsync(cancellationToken);
        await DomainEventDispatcher.PublishAsync(events, messageBus);

        return product;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Product>> GetByCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default)
        => await dbContext.Products
            .Include(p => p.Translations)
            .Include(p => p.ComboItems)
            .Where(p => p.MenuCategoryId == categoryId)
            .OrderBy(p => p.SortOrderInCategory)
            .ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<int> GetMaxSortOrderInCategoryAsync(Guid categoryId, CancellationToken cancellationToken = default)
    {
        var max = await dbContext.Products
            .Where(p => p.MenuCategoryId == categoryId)
            .MaxAsync(p => (int?)p.SortOrderInCategory, cancellationToken);

        return max ?? -1;
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Product>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken cancellationToken = default)
    {
        var idList = ids.ToList();
        return await dbContext.Products
            .Include(p => p.Translations)
            .Include(p => p.ComboItems)
            .Where(p => idList.Contains(p.Id))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> IsComponentOfAnyComboAsync(Guid productId, CancellationToken cancellationToken = default)
        => await dbContext.ComboItems
            .Where(ci => ci.ComponentProductId == productId)
            .Join(dbContext.Products, ci => ci.ComboProductId, p => p.Id, (ci, p) => p)
            .AnyAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var events = DomainEventDispatcher.CollectAndClear(dbContext);
        await dbContext.SaveChangesAsync(cancellationToken);
        await DomainEventDispatcher.PublishAsync(events, messageBus);
    }
}

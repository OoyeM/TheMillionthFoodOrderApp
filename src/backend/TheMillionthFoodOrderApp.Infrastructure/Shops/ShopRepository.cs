using Microsoft.EntityFrameworkCore;
using TheMillionthFoodOrderApp.Domain.Shops;
using TheMillionthFoodOrderApp.Infrastructure.Persistence;

namespace TheMillionthFoodOrderApp.Infrastructure.Shops;

/// <summary>
/// Brand-scoped shop repository. Uses <see cref="BrandDbContextFactory"/> to resolve
/// the correct brand database for the current HTTP request. Because <see cref="BrandDbContext"/>
/// is not registered as a long-lived DI service (it is resolved on-demand), each public method
/// manages its own unit-of-work: a single <c>await using</c> context that loads, mutates, and saves
/// within one call. This avoids the change-tracking issues that would arise from splitting
/// load and save across two independently created contexts.
/// </summary>
public sealed class ShopRepository(BrandDbContextFactory dbContextFactory) : IShopRepository
{
    /// <inheritdoc/>
    public async Task<Shop?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var dbContext = dbContextFactory.CreateDbContext();
        return await dbContext.Shops.FindAsync([id], cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<Shop?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        await using var dbContext = dbContextFactory.CreateDbContext();
        return await dbContext.Shops
            .FirstOrDefaultAsync(s => s.Slug == slug, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Shop>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var dbContext = dbContextFactory.CreateDbContext();
        return await dbContext.Shops
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Persists the shop immediately. Callers must NOT call <see cref="SaveChangesAsync"/> separately
    /// after <see cref="AddAsync"/> — the add and save are atomic within this method.
    /// </remarks>
    public async Task AddAsync(Shop shop, CancellationToken cancellationToken = default)
    {
        await using var dbContext = dbContextFactory.CreateDbContext();
        await dbContext.Shops.AddAsync(shop, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Executes a load-mutate-save cycle within a single brand context. The <paramref name="mutate"/>
    /// delegate receives the tracked entity and applies changes; the context then saves.
    /// Returns null if no shop with the given <paramref name="id"/> exists.
    /// </remarks>
    public async Task<Shop?> UpdateAsync(Guid id, Action<Shop> mutate, CancellationToken cancellationToken = default)
    {
        await using var dbContext = dbContextFactory.CreateDbContext();
        var shop = await dbContext.Shops.FindAsync([id], cancellationToken);
        if (shop is null)
            return null;

        mutate(shop);
        await dbContext.SaveChangesAsync(cancellationToken);
        return shop;
    }

}

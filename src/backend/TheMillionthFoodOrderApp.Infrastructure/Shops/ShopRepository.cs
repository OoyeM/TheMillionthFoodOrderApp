using Microsoft.EntityFrameworkCore;
using TheMillionthFoodOrderApp.Domain.Shops;
using TheMillionthFoodOrderApp.Infrastructure.Persistence;
using Wolverine;

namespace TheMillionthFoodOrderApp.Infrastructure.Shops;

/// <summary>
/// Brand-scoped shop repository. Injects <see cref="BrandDbContext"/> directly
/// (registered as scoped via factory delegate in DI).
/// </summary>
public sealed class ShopRepository(BrandDbContext dbContext, IMessageBus messageBus) : IShopRepository
{
    /// <inheritdoc/>
    public async Task<Shop?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.Shops
            .Include(s => s.OpeningHours)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    /// <inheritdoc/>
    public async Task<Shop?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
        => await dbContext.Shops
            .Include(s => s.OpeningHours)
            .FirstOrDefaultAsync(s => s.Slug == slug, cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Shop>> GetAllAsync(CancellationToken cancellationToken = default)
        => await dbContext.Shops
            .Include(s => s.OpeningHours)
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<Shop>> GetActiveAsync(CancellationToken cancellationToken = default)
        => await dbContext.Shops
            .Include(s => s.OpeningHours)
            .Where(s => s.IsActive)
            .OrderBy(s => s.Name)
            .ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task AddAsync(Shop shop, CancellationToken cancellationToken = default)
        => await dbContext.Shops.AddAsync(shop, cancellationToken);

    /// <inheritdoc/>
    /// <remarks>
    /// Loads the entity (with opening hours), applies <paramref name="mutate"/>, and saves in one call.
    /// Returns null if not found.
    /// </remarks>
    public async Task<Shop?> UpdateAsync(Guid id, Action<Shop> mutate, CancellationToken cancellationToken = default)
    {
        var shop = await dbContext.Shops
            .Include(s => s.OpeningHours)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (shop is null)
            return null;

        mutate(shop);

        var events = DomainEventDispatcher.CollectAndClear(dbContext);
        await dbContext.SaveChangesAsync(cancellationToken);
        await DomainEventDispatcher.PublishAsync(events, messageBus);

        return shop;
    }

    /// <inheritdoc/>
    public async Task<Shop?> ReplaceOpeningHoursAsync(Guid shopId, Action<Shop> mutate, CancellationToken cancellationToken = default)
    {
        Shop? shop = null;

        await dbContext.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            await dbContext.OpeningHoursTimeBlocks
                .Where(b => b.ShopId == shopId)
                .ExecuteDeleteAsync(cancellationToken);

            // Clear tracker so FirstAsync returns a fresh instance without old block snapshots.
            dbContext.ChangeTracker.Clear();

            shop = await dbContext.Shops
                .FirstOrDefaultAsync(s => s.Id == shopId, cancellationToken);

            if (shop is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return;
            }

            mutate(shop);

            await dbContext.OpeningHoursTimeBlocks.AddRangeAsync(shop.OpeningHours, cancellationToken);

            var events = DomainEventDispatcher.CollectAndClear(dbContext);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            await DomainEventDispatcher.PublishAsync(events, messageBus);
        });

        return shop;
    }

    /// <inheritdoc/>
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var events = DomainEventDispatcher.CollectAndClear(dbContext);
        await dbContext.SaveChangesAsync(cancellationToken);
        await DomainEventDispatcher.PublishAsync(events, messageBus);
    }
}

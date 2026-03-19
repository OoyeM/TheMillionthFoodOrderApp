using Microsoft.EntityFrameworkCore;
using TheMillionthFoodOrderApp.Domain.Shops;
using TheMillionthFoodOrderApp.Infrastructure.Persistence;

namespace TheMillionthFoodOrderApp.Infrastructure.Shops;

/// <summary>
/// Brand-scoped shop repository. Injects <see cref="BrandDbContext"/> directly
/// (registered as scoped via factory delegate in DI).
/// </summary>
public sealed class ShopRepository(BrandDbContext dbContext) : IShopRepository
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
        await dbContext.SaveChangesAsync(cancellationToken);
        return shop;
    }

    /// <inheritdoc/>
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => await dbContext.SaveChangesAsync(cancellationToken);
}

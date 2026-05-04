using Microsoft.EntityFrameworkCore;
using TheMillionthFoodOrderApp.Domain.MenuCategories;
using TheMillionthFoodOrderApp.Infrastructure.Persistence;
using Wolverine;

namespace TheMillionthFoodOrderApp.Infrastructure.MenuCategories;

/// <summary>
/// Brand-scoped menu category repository. Injects <see cref="BrandDbContext"/> directly
/// (registered as scoped via factory delegate in DI).
/// </summary>
public sealed class MenuCategoryRepository(BrandDbContext dbContext, IMessageBus messageBus) : IMenuCategoryRepository
{
    /// <inheritdoc/>
    public async Task<MenuCategory?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.MenuCategories
            .Include(c => c.Translations)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<MenuCategory>> GetAllAsync(CancellationToken cancellationToken = default)
        => await dbContext.MenuCategories
            .Include(c => c.Translations)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.CreatedAt)
            .ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task AddAsync(MenuCategory category, CancellationToken cancellationToken = default)
        => await dbContext.MenuCategories.AddAsync(category, cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyDictionary<Guid, int>> GetProductCountsAsync(CancellationToken cancellationToken = default)
    {
        var counts = await dbContext.Products
            .Where(p => p.MenuCategoryId != null)
            .GroupBy(p => p.MenuCategoryId!.Value)
            .Select(g => new { CategoryId = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return counts.ToDictionary(x => x.CategoryId, x => x.Count);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// Loads the entity with translations, applies <paramref name="mutate"/>, and saves in one call.
    /// Returns null if not found.
    /// </remarks>
    public async Task<MenuCategory?> UpdateAsync(Guid id, Action<MenuCategory> mutate, CancellationToken cancellationToken = default)
    {
        // Load category WITHOUT translations to avoid EF Core change tracking conflicts
        // when the domain method clears and re-adds the child collection.
        var category = await dbContext.MenuCategories
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (category is null)
            return null;

        // Wrap in an explicit transaction so the DELETE + INSERT are atomic.
        // ExecuteDeleteAsync commits immediately and bypasses the change tracker,
        // so without a transaction, a failure in mutate() or SaveChangesAsync()
        // would leave the category with zero translations.
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        await dbContext.MenuCategoryTranslations
            .Where(t => t.MenuCategoryId == id)
            .ExecuteDeleteAsync(cancellationToken);

        mutate(category);

        dbContext.MenuCategoryTranslations.AddRange(category.Translations);

        var events = DomainEventDispatcher.CollectAndClear(dbContext);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await DomainEventDispatcher.PublishAsync(events, messageBus);

        return category;
    }

    /// <inheritdoc/>
    public async Task<MenuCategory?> UpdateScalarAsync(Guid id, Action<MenuCategory> mutate, CancellationToken cancellationToken = default)
    {
        var category = await dbContext.MenuCategories
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        if (category is null)
            return null;

        mutate(category);

        var events = DomainEventDispatcher.CollectAndClear(dbContext);
        await dbContext.SaveChangesAsync(cancellationToken);
        await DomainEventDispatcher.PublishAsync(events, messageBus);

        return category;
    }

    /// <inheritdoc/>
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var events = DomainEventDispatcher.CollectAndClear(dbContext);
        await dbContext.SaveChangesAsync(cancellationToken);
        await DomainEventDispatcher.PublishAsync(events, messageBus);
    }
}

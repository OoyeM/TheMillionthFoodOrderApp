using Microsoft.EntityFrameworkCore;
using TheMillionthFoodOrderApp.Domain.ModifierGroups;
using TheMillionthFoodOrderApp.Infrastructure.Persistence;
using Wolverine;

namespace TheMillionthFoodOrderApp.Infrastructure.ModifierGroups;

/// <summary>
/// Brand-scoped modifier group repository. Injects <see cref="BrandDbContext"/> directly
/// (registered as scoped via factory delegate in DI).
/// </summary>
public sealed class ModifierGroupRepository(BrandDbContext dbContext, IMessageBus messageBus) : IModifierGroupRepository
{
    /// <inheritdoc/>
    public async Task<ModifierGroup?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.ModifierGroups
            .Include(g => g.Translations)
            .Include(g => g.Modifiers)
                .ThenInclude(m => m.Translations)
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ModifierGroup>> GetAllAsync(CancellationToken cancellationToken = default)
        => await dbContext.ModifierGroups
            .Include(g => g.Translations)
            .Include(g => g.Modifiers)
                .ThenInclude(m => m.Translations)
            .OrderBy(g => g.CreatedAt)
            .ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task AddAsync(ModifierGroup modifierGroup, CancellationToken cancellationToken = default)
        => await dbContext.ModifierGroups.AddAsync(modifierGroup, cancellationToken);

    /// <inheritdoc/>
    /// <remarks>
    /// Transactional three-level update:
    /// 1. DELETE ModifierTranslations for all modifiers in this group
    /// 2. DELETE Modifiers for this group
    /// 3. DELETE GroupTranslations for this group
    /// 4. Apply mutate() (which re-adds translations + modifiers via domain method)
    /// 5. INSERT new children and save
    /// </remarks>
    public async Task<ModifierGroup?> UpdateAsync(Guid id, Action<ModifierGroup> mutate, CancellationToken cancellationToken = default)
    {
        var group = await dbContext.ModifierGroups
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

        if (group is null)
            return null;

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        // Step 1: delete modifier translations for all modifiers in this group
        var modifierIds = await dbContext.Modifiers
            .Where(m => EF.Property<Guid>(m, "ModifierGroupId") == id)
            .Select(m => m.Id)
            .ToListAsync(cancellationToken);

        if (modifierIds.Count > 0)
        {
            await dbContext.ModifierTranslations
                .Where(t => modifierIds.Contains(t.ModifierId))
                .ExecuteDeleteAsync(cancellationToken);
        }

        // Step 2: delete modifiers
        await dbContext.Modifiers
            .Where(m => EF.Property<Guid>(m, "ModifierGroupId") == id)
            .ExecuteDeleteAsync(cancellationToken);

        // Step 3: delete group translations
        await dbContext.ModifierGroupTranslations
            .Where(t => t.ModifierGroupId == id)
            .ExecuteDeleteAsync(cancellationToken);

        // Step 4: apply mutation (re-populates _translations and _modifiers collections)
        mutate(group);

        // Step 5: insert new children
        dbContext.ModifierGroupTranslations.AddRange(group.Translations);
        foreach (var modifier in group.Modifiers)
        {
            dbContext.Modifiers.Add(modifier);
            dbContext.ModifierTranslations.AddRange(modifier.Translations);
        }

        var events = DomainEventDispatcher.CollectAndClear(dbContext);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await DomainEventDispatcher.PublishAsync(events, messageBus);

        return group;
    }

    /// <inheritdoc/>
    public async Task<bool> SoftDeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var group = await dbContext.ModifierGroups
            .FirstOrDefaultAsync(g => g.Id == id, cancellationToken);

        if (group is null)
            return false;

        group.SoftDelete();

        var events = DomainEventDispatcher.CollectAndClear(dbContext);
        await dbContext.SaveChangesAsync(cancellationToken);
        await DomainEventDispatcher.PublishAsync(events, messageBus);

        return true;
    }

    /// <inheritdoc/>
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var events = DomainEventDispatcher.CollectAndClear(dbContext);
        await dbContext.SaveChangesAsync(cancellationToken);
        await DomainEventDispatcher.PublishAsync(events, messageBus);
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyList<ProductModifierGroup>> GetProductModifierGroupsAsync(
        Guid productId,
        CancellationToken cancellationToken = default)
        => await dbContext.ProductModifierGroups
            .Where(pmg => pmg.ProductId == productId)
            .OrderBy(pmg => pmg.SortOrder)
            .ToListAsync(cancellationToken);

    /// <inheritdoc/>
    public async Task SetProductModifierGroupsAsync(
        Guid productId,
        IEnumerable<(Guid modifierGroupId, int sortOrder)> assignments,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        // Remove existing assignments for this product
        await dbContext.ProductModifierGroups
            .Where(pmg => pmg.ProductId == productId)
            .ExecuteDeleteAsync(cancellationToken);

        // Add new assignments
        foreach (var (modifierGroupId, sortOrder) in assignments)
        {
            var pmg = ProductModifierGroup.Create(productId, modifierGroupId, sortOrder);
            await dbContext.ProductModifierGroups.AddAsync(pmg, cancellationToken);
        }

        var events = DomainEventDispatcher.CollectAndClear(dbContext);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await DomainEventDispatcher.PublishAsync(events, messageBus);
    }
}

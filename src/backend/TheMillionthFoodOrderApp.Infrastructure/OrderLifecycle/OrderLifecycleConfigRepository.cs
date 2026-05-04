using Microsoft.EntityFrameworkCore;
using TheMillionthFoodOrderApp.Domain.OrderLifecycle;
using TheMillionthFoodOrderApp.Infrastructure.Persistence;
using Wolverine;

namespace TheMillionthFoodOrderApp.Infrastructure.OrderLifecycle;

public sealed class OrderLifecycleConfigRepository(BrandDbContext dbContext, IMessageBus messageBus) : IOrderLifecycleConfigRepository
{
    public async Task<OrderLifecycleConfig?> GetByShopIdAsync(Guid shopId, CancellationToken cancellationToken = default)
        => await dbContext.OrderLifecycleConfigs
            .Include(c => c.Statuses)
            .Include(c => c.Transitions)
            .FirstOrDefaultAsync(c => c.ShopId == shopId, cancellationToken);

    public async Task AddAsync(OrderLifecycleConfig config, CancellationToken cancellationToken = default)
        => await dbContext.OrderLifecycleConfigs.AddAsync(config, cancellationToken);

    public Task RemoveAsync(OrderLifecycleConfig config, CancellationToken cancellationToken = default)
    {
        // Detach children so EF's client-side Restrict check doesn't fire when the config is
        // marked Deleted. SQL Server's DB-level cascade (Config→Transitions, Config→Statuses)
        // handles child deletion in the correct FK order.
        DetachChildren(config);
        dbContext.OrderLifecycleConfigs.Remove(config);
        return Task.CompletedTask;
    }

    public async Task<OrderLifecycleConfig> ReplaceAsync(Guid configId, Action<OrderLifecycleConfig> mutate, CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        // Delete old children in FK order — bypasses EF change tracker entirely.
        await dbContext.OrderStatusTransitions
            .Where(t => t.OrderLifecycleConfigId == configId)
            .ExecuteDeleteAsync(cancellationToken);

        await dbContext.OrderStatuses
            .Where(s => s.OrderLifecycleConfigId == configId)
            .ExecuteDeleteAsync(cancellationToken);

        // Clear all tracked entities so the identity map returns a fresh, unsnapshotted
        // instance below. Without this, FirstAsync would return the stale tracked instance
        // (loaded with includes by GetByShopIdAsync), and DetectChanges would compare the
        // new navigation children against the old snapshot — triggering spurious DELETEs or
        // the Restrict "association severed" error.
        dbContext.ChangeTracker.Clear();

        // Load config WITHOUT children — EF now has no snapshot of old statuses/transitions.
        var config = await dbContext.OrderLifecycleConfigs
            .FirstAsync(c => c.Id == configId, cancellationToken);

        mutate(config);

        // Explicitly register new children rather than relying on navigation snapshot detection,
        // which can miss items added to a backing-field collection after the entity was loaded.
        // Statuses must be Added before Transitions so EF inserts them first (FK ordering).
        await dbContext.OrderStatuses.AddRangeAsync(config.Statuses, cancellationToken);
        dbContext.OrderStatusTransitions.AddRange(config.Transitions);

        var events = DomainEventDispatcher.CollectAndClear(dbContext);
        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        await DomainEventDispatcher.PublishAsync(events, messageBus);

        return config;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var events = DomainEventDispatcher.CollectAndClear(dbContext);
        await dbContext.SaveChangesAsync(cancellationToken);
        await DomainEventDispatcher.PublishAsync(events, messageBus);
    }

    private void DetachChildren(OrderLifecycleConfig config)
    {
        foreach (var t in config.Transitions.ToList())
            dbContext.Entry(t).State = EntityState.Detached;
        foreach (var s in config.Statuses.ToList())
            dbContext.Entry(s).State = EntityState.Detached;
    }
}

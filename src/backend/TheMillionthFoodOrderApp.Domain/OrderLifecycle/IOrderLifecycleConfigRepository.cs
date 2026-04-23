namespace TheMillionthFoodOrderApp.Domain.OrderLifecycle;

public interface IOrderLifecycleConfigRepository
{
    Task<OrderLifecycleConfig?> GetByShopIdAsync(Guid shopId, CancellationToken cancellationToken = default);
    Task AddAsync(OrderLifecycleConfig config, CancellationToken cancellationToken = default);
    Task RemoveAsync(OrderLifecycleConfig config, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces all statuses and transitions on the config identified by <paramref name="configId"/>
    /// within a transaction, bypassing EF Core's change tracker to avoid cascade/restrict conflicts.
    /// Returns the updated config (without children navigation — call GetByShopIdAsync if needed).
    /// </summary>
    Task<OrderLifecycleConfig> ReplaceAsync(Guid configId, Action<OrderLifecycleConfig> mutate, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

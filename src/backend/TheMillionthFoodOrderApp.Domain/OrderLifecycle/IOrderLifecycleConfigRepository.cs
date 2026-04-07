namespace TheMillionthFoodOrderApp.Domain.OrderLifecycle;

public interface IOrderLifecycleConfigRepository
{
    Task<OrderLifecycleConfig?> GetByShopIdAsync(Guid shopId, CancellationToken cancellationToken = default);
    Task AddAsync(OrderLifecycleConfig config, CancellationToken cancellationToken = default);
    Task RemoveAsync(OrderLifecycleConfig config, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

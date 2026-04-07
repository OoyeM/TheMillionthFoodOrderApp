using Microsoft.EntityFrameworkCore;
using TheMillionthFoodOrderApp.Domain.OrderLifecycle;
using TheMillionthFoodOrderApp.Infrastructure.Persistence;

namespace TheMillionthFoodOrderApp.Infrastructure.OrderLifecycle;

public sealed class OrderLifecycleConfigRepository(BrandDbContext dbContext) : IOrderLifecycleConfigRepository
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
        dbContext.OrderLifecycleConfigs.Remove(config);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => await dbContext.SaveChangesAsync(cancellationToken);
}

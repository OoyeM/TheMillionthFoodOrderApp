using Microsoft.EntityFrameworkCore;
using TheMillionthFoodOrderApp.Domain.Orders;
using TheMillionthFoodOrderApp.Infrastructure.Persistence;
using Wolverine;

namespace TheMillionthFoodOrderApp.Infrastructure.Orders;

/// <summary>
/// Brand-scoped order repository. Injects <see cref="BrandDbContext"/> directly
/// (registered as scoped via factory delegate in DI).
/// </summary>
public sealed class OrderRepository(BrandDbContext dbContext, IMessageBus messageBus) : IOrderRepository
{
    /// <summary>
    /// Marker message used when <see cref="SaveChangesAsync"/> detects a duplicate
    /// OrderNumber unique-index violation. The Application layer catches this to
    /// retry with a freshly generated number — keeping EF Core out of the Application layer.
    /// </summary>
    internal const string OrderNumberConflictMessage = "ORDER_NUMBER_CONFLICT";

    /// <inheritdoc/>
    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await dbContext.Orders
            .Include(o => o.Items)
            .ThenInclude(i => i.SelectedModifiers)
            .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    /// <inheritdoc/>
    public async Task AddAsync(Order order, CancellationToken cancellationToken = default)
        => await dbContext.Orders.AddAsync(order, cancellationToken);

    /// <inheritdoc/>
    /// <remarks>
    /// If the INSERT fails because of a duplicate on <c>UX_Orders_ShopId_OrderNumber</c>
    /// (SQL Server error 2601 or 2627), the failed entity is detached from the change tracker
    /// and an <see cref="InvalidOperationException"/> with message
    /// <see cref="OrderNumberConflictMessage"/> is thrown so the Application service can
    /// regenerate the order number and retry. All other exceptions propagate as-is.
    /// </remarks>
    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var events = DomainEventDispatcher.CollectAndClear(dbContext);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (
            ex.InnerException is Microsoft.Data.SqlClient.SqlException { Number: 2601 or 2627 } sqlEx
            && sqlEx.Message.Contains("UX_Orders_ShopId_OrderNumber", StringComparison.OrdinalIgnoreCase))
        {
            // Detach all Added entries so the context is clean for a retry
            foreach (var entry in dbContext.ChangeTracker.Entries().Where(e => e.State == EntityState.Added).ToList())
                entry.State = EntityState.Detached;

            throw new InvalidOperationException(OrderNumberConflictMessage, ex);
        }

        await DomainEventDispatcher.PublishAsync(events, messageBus);
    }

    /// <inheritdoc/>
    public async Task<bool> OrderNumberExistsAsync(
        Guid shopId,
        string orderNumber,
        CancellationToken cancellationToken = default)
        => await dbContext.Orders
            .AnyAsync(o => o.ShopId == shopId && o.OrderNumber == orderNumber, cancellationToken);
}

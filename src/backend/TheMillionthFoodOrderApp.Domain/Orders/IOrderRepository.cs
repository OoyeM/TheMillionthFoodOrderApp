namespace TheMillionthFoodOrderApp.Domain.Orders;

/// <summary>
/// Repository for persisting and retrieving <see cref="Order"/> aggregates.
/// Implementations live in the Infrastructure layer.
/// </summary>
public interface IOrderRepository
{
    /// <summary>Returns the order with all items (including selected modifiers), or null.</summary>
    Task<Order?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Stages the order for insert. Call <see cref="SaveChangesAsync"/> to persist.</summary>
    Task AddAsync(Order order, CancellationToken cancellationToken = default);

    /// <summary>Persists all pending changes and dispatches domain events via Wolverine.</summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the order with all items (including selected modifiers) matching the
    /// given order number for the specified shop, or null if not found.
    /// </summary>
    Task<Order?> GetByOrderNumberAsync(Guid shopId, string orderNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true if an order with this number already exists for the given shop.
    /// Used to guard against race-condition duplicate order numbers.
    /// </summary>
    Task<bool> OrderNumberExistsAsync(Guid shopId, string orderNumber, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the active orders for a shop — those whose status is not a terminal
    /// status in the shop's <c>OrderLifecycleConfig</c> — ordered by <c>CreatedAt</c>
    /// ascending. Items and selected modifiers are included so kitchen-side callers
    /// can render line details without a second round-trip.
    /// </summary>
    Task<IReadOnlyList<Order>> GetActiveByShopAsync(Guid shopId, CancellationToken cancellationToken = default);
}

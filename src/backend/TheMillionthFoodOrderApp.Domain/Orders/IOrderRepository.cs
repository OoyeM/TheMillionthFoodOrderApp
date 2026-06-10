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

    /// <summary>
    /// Returns the number of orders that have been placed into the given slot
    /// (matched by exact <c>TimeSlotStart</c>). Used for best-effort capacity enforcement
    /// at order-create time (US-FP-019, design decision 5).
    /// </summary>
    Task<int> CountByTimeSlotAsync(Guid shopId, DateTimeOffset slotStartUtc, CancellationToken ct);

    /// <summary>
    /// Returns a dictionary of <c>TimeSlotStart → order count</c> for all slots whose UTC
    /// start falls within [<paramref name="fromUtc"/>, <paramref name="toUtc"/>] for the
    /// given shop. Used by the availability endpoint to hydrate capacity in one query.
    /// </summary>
    Task<IReadOnlyDictionary<DateTimeOffset, int>> GetTimeSlotCountsAsync(
        Guid shopId, DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken ct);

    /// <summary>
    /// Returns the count of non-terminal (active) orders for the shop.
    /// Used for the "place in line" notice when time-slot ordering is disabled (AC5).
    /// Applies the same terminal-name filter as <see cref="GetActiveByShopAsync"/>.
    /// </summary>
    Task<int> CountActiveByShopAsync(Guid shopId, CancellationToken ct);
}

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
    /// Returns true if an order with this number already exists for the given shop.
    /// Used to guard against race-condition duplicate order numbers.
    /// </summary>
    Task<bool> OrderNumberExistsAsync(Guid shopId, string orderNumber, CancellationToken cancellationToken = default);
}

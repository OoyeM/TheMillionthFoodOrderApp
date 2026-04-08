namespace TheMillionthFoodOrderApp.Application.Orders;

/// <summary>
/// Abstraction for sending real-time order notifications to connected clients.
/// Implementations live in the Infrastructure layer (SignalR).
/// </summary>
public interface IOrderNotificationService
{
    /// <summary>
    /// Notifies all clients monitoring the specified shop and the specific order
    /// that an order's status has changed.
    /// </summary>
    Task NotifyOrderStatusChangedAsync(
        Guid orderId,
        Guid shopId,
        string brandSlug,
        string previousStatus,
        string newStatus,
        string? customerName,
        CancellationToken cancellationToken = default);
}

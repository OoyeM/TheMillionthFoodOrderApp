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
        DateTimeOffset occurredOn,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Canonical payload shape for order status change notifications sent via SignalR.
/// Shared between the notification service, integration tests, and used as
/// the contract for the TypeScript frontend type.
/// </summary>
public sealed record OrderStatusUpdatePayload(
    Guid OrderId,
    Guid ShopId,
    string BrandSlug,
    string PreviousStatus,
    string NewStatus,
    string? CustomerName,
    DateTimeOffset Timestamp);

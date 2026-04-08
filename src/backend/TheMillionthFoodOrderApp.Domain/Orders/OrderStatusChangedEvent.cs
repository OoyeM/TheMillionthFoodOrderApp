using TheMillionthFoodOrderApp.Domain.Common;

namespace TheMillionthFoodOrderApp.Domain.Orders;

/// <summary>
/// Raised when an order transitions to a new status.
/// Consumed by the SignalR notification handler to push real-time updates
/// to kitchen displays, customer tracking pages, and POS interfaces.
/// </summary>
public sealed record OrderStatusChangedEvent(
    Guid OrderId,
    Guid ShopId,
    string BrandSlug,
    string PreviousStatus,
    string NewStatus,
    string? CustomerName) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}

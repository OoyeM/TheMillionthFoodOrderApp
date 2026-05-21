using TheMillionthFoodOrderApp.Domain.Common;

namespace TheMillionthFoodOrderApp.Domain.Orders;

/// <summary>
/// Raised when a new order is successfully placed.
/// Consumed by SignalR notification infrastructure to push real-time updates
/// to kitchen displays, POS interfaces, and other connected clients.
/// </summary>
public sealed record OrderCreatedEvent(
    Guid OrderId,
    Guid ShopId,
    string BrandSlug,
    string OrderNumber,
    string StatusName,
    string? CustomerName,
    string? TableNumber) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}

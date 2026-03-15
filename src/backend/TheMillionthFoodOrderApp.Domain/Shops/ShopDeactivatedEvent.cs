using TheMillionthFoodOrderApp.Domain.Common;

namespace TheMillionthFoodOrderApp.Domain.Shops;

/// <summary>
/// Raised when a shop is deactivated, hiding it from customers.
/// Downstream handlers can use this event to clear caches, cancel open orders, etc.
/// </summary>
public sealed record ShopDeactivatedEvent(Guid ShopId, string Slug) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}

using TheMillionthFoodOrderApp.Domain.Common;

namespace TheMillionthFoodOrderApp.Domain.Shops;

/// <summary>
/// Raised when a shop is created. A future Wolverine handler will use this event
/// to clone the brand's product catalog into the newly created shop.
/// </summary>
public sealed record ShopCreatedEvent(Guid ShopId, string Name, string Slug) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}

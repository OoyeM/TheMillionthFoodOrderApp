using TheMillionthFoodOrderApp.Domain.Common;

namespace TheMillionthFoodOrderApp.Domain.ModifierGroups;

public sealed record ModifierGroupCreatedEvent(Guid ModifierGroupId) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}

using TheMillionthFoodOrderApp.Domain.Common;

namespace TheMillionthFoodOrderApp.Domain.MenuCategories;

public sealed record MenuCategoryDeletedEvent(Guid MenuCategoryId) : IDomainEvent
{
    public DateTimeOffset OccurredOn { get; } = DateTimeOffset.UtcNow;
}

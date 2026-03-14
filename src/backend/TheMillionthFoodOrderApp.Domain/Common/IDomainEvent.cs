namespace TheMillionthFoodOrderApp.Domain.Common;

public interface IDomainEvent
{
    DateTimeOffset OccurredOnUtc { get; }
}

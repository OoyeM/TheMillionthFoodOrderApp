namespace TheMillionthFoodOrderApp.Domain.Common;

public interface IDomainEvent
{
    DateTimeOffset OccurredOn { get; }
}

using TheMillionthFoodOrderApp.Domain.Common;

namespace TheMillionthFoodOrderApp.Domain.OrderLifecycle;

/// <summary>
/// Defines an allowed transition between two order statuses.
/// Child entity of <see cref="OrderLifecycleConfig"/> — always accessed through the aggregate.
/// </summary>
public sealed class OrderStatusTransition : Entity<Guid>
{
    public Guid OrderLifecycleConfigId { get; private set; }
    public Guid FromStatusId { get; private set; }
    public Guid ToStatusId { get; private set; }

    // Required by EF Core
    private OrderStatusTransition() { }

    public static OrderStatusTransition Create(
        Guid configId,
        Guid fromStatusId,
        Guid toStatusId)
    {
        return new OrderStatusTransition
        {
            Id = Guid.CreateVersion7(),
            OrderLifecycleConfigId = configId,
            FromStatusId = fromStatusId,
            ToStatusId = toStatusId,
        };
    }
}

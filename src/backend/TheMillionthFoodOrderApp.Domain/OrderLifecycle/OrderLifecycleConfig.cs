using TheMillionthFoodOrderApp.Domain.Common;

namespace TheMillionthFoodOrderApp.Domain.OrderLifecycle;

/// <summary>
/// Aggregate root defining the order lifecycle for a single shop.
/// Contains the set of statuses and allowed transitions between them.
/// </summary>
public sealed class OrderLifecycleConfig : AggregateRoot<Guid>, IAuditable
{
    public Guid ShopId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private readonly List<OrderStatus> _statuses = [];
    public IReadOnlyCollection<OrderStatus> Statuses => _statuses.AsReadOnly();

    private readonly List<OrderStatusTransition> _transitions = [];
    public IReadOnlyCollection<OrderStatusTransition> Transitions => _transitions.AsReadOnly();

    // Required by EF Core
    private OrderLifecycleConfig() { }

    /// <summary>
    /// Creates a default order lifecycle config for a shop:
    /// Placed → Confirmed → Preparing → Ready → Picked Up / Delivered.
    /// </summary>
    public static OrderLifecycleConfig CreateDefault(Guid shopId)
    {
        var now = DateTimeOffset.UtcNow;
        var config = new OrderLifecycleConfig
        {
            Id = Guid.CreateVersion7(),
            ShopId = shopId,
            CreatedAt = now,
            UpdatedAt = now,
        };

        var placed = OrderStatus.Create(config.Id, "Placed", "placed", 0, false);
        var confirmed = OrderStatus.Create(config.Id, "Confirmed", "confirmed", 1, false);
        var preparing = OrderStatus.Create(config.Id, "Preparing", "preparing", 2, false);
        var ready = OrderStatus.Create(config.Id, "Ready", "ready", 3, false);
        var pickedUp = OrderStatus.Create(config.Id, "Picked Up", "picked_up", 4, true);
        var delivered = OrderStatus.Create(config.Id, "Delivered", "delivered", 5, true);

        config._statuses.AddRange([placed, confirmed, preparing, ready, pickedUp, delivered]);

        config._transitions.AddRange([
            OrderStatusTransition.Create(config.Id, placed.Id, confirmed.Id),
            OrderStatusTransition.Create(config.Id, confirmed.Id, preparing.Id),
            OrderStatusTransition.Create(config.Id, preparing.Id, ready.Id),
            OrderStatusTransition.Create(config.Id, ready.Id, pickedUp.Id),
            OrderStatusTransition.Create(config.Id, ready.Id, delivered.Id),
        ]);

        return config;
    }

    /// <summary>
    /// Atomically replaces all statuses and transitions.
    /// Validates: min 2 statuses, at least one terminal, sequential sort orders,
    /// and all transitions reference statuses in the provided list.
    /// </summary>
    public void ConfigureLifecycle(
        IEnumerable<OrderStatus> statuses,
        IEnumerable<OrderStatusTransition> transitions)
    {
        var statusList = statuses.ToList();
        var transitionList = transitions.ToList();

        if (statusList.Count < 2)
            throw new ArgumentException("At least two statuses are required.");

        if (!statusList.Any(s => s.IsTerminal))
            throw new ArgumentException("At least one status must be terminal.");

        // Validate sequential sort orders (0..n-1)
        var sortOrders = statusList.Select(s => s.SortOrder).OrderBy(o => o).ToList();
        for (var i = 0; i < sortOrders.Count; i++)
        {
            if (sortOrders[i] != i)
                throw new ArgumentException(
                    $"Sort orders must be sequential starting from 0. Expected {i}, got {sortOrders[i]}.");
        }

        // Validate transitions reference valid statuses
        var statusIds = new HashSet<Guid>(statusList.Select(s => s.Id));
        foreach (var transition in transitionList)
        {
            if (!statusIds.Contains(transition.FromStatusId))
                throw new ArgumentException(
                    $"Transition references unknown FromStatusId: {transition.FromStatusId}.");

            if (!statusIds.Contains(transition.ToStatusId))
                throw new ArgumentException(
                    $"Transition references unknown ToStatusId: {transition.ToStatusId}.");
        }

        _statuses.Clear();
        _statuses.AddRange(statusList);
        _transitions.Clear();
        _transitions.AddRange(transitionList);
        UpdatedAt = DateTimeOffset.UtcNow;
    }
}

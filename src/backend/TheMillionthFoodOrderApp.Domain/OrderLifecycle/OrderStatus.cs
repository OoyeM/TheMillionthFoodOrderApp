using TheMillionthFoodOrderApp.Domain.Common;

namespace TheMillionthFoodOrderApp.Domain.OrderLifecycle;

/// <summary>
/// A status in the order lifecycle (e.g. "Placed", "Preparing", "Ready").
/// Child entity of <see cref="OrderLifecycleConfig"/> — always accessed through the aggregate.
/// </summary>
public sealed class OrderStatus : Entity<Guid>
{
    public Guid OrderLifecycleConfigId { get; private set; }
    public string Name { get; private set; } = string.Empty;

    /// <summary>
    /// Well-known key for predefined statuses: "placed", "confirmed", "preparing",
    /// "ready", "picked_up", "delivered". Null for custom statuses.
    /// </summary>
    public string? SystemKey { get; private set; }

    public int SortOrder { get; private set; }
    public bool IsEnabled { get; private set; } = true;

    /// <summary>Marks end-of-flow statuses (e.g. "Picked Up", "Delivered", "Done").</summary>
    public bool IsTerminal { get; private set; }

    /// <summary>Optional display color in hex format (e.g. "#FF5733").</summary>
    public string? ColorHex { get; private set; }

    // Required by EF Core
    private OrderStatus() { }

    public static OrderStatus Create(
        Guid configId,
        string name,
        string? systemKey,
        int sortOrder,
        bool isTerminal,
        string? colorHex = null)
    {
        return new OrderStatus
        {
            Id = Guid.CreateVersion7(),
            OrderLifecycleConfigId = configId,
            Name = name,
            SystemKey = systemKey,
            SortOrder = sortOrder,
            IsEnabled = true,
            IsTerminal = isTerminal,
            ColorHex = colorHex,
        };
    }
}

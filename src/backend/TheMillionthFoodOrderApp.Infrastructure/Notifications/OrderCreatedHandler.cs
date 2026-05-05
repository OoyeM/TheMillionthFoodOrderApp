using TheMillionthFoodOrderApp.Application.Orders;
using TheMillionthFoodOrderApp.Domain.Orders;

namespace TheMillionthFoodOrderApp.Infrastructure.Notifications;

/// <summary>
/// Wolverine message handler that bridges <see cref="OrderCreatedEvent"/> to SignalR.
/// When a new order is placed, connected kitchen displays and POS clients receive
/// a real-time notification via the shop group.
///
/// Wolverine discovers this handler by convention: HandleAsync(OrderCreatedEvent).
/// </summary>
public sealed class OrderCreatedHandler(IOrderNotificationService notificationService)
{
    public async Task HandleAsync(OrderCreatedEvent @event, CancellationToken cancellationToken)
    {
        // Notify all clients monitoring this shop that a new order has arrived.
        // We reuse the status-changed pipeline: a new order appears as a transition
        // from "(none)" to the opening status so the kitchen display can render it.
        await notificationService.NotifyOrderStatusChangedAsync(
            @event.OrderId,
            @event.ShopId,
            @event.BrandSlug,
            previousStatus: string.Empty,
            newStatus: @event.StatusName,
            @event.CustomerName,
            @event.OccurredOn,
            cancellationToken);
    }
}

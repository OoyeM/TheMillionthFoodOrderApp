using TheMillionthFoodOrderApp.Application.Orders;
using TheMillionthFoodOrderApp.Domain.Orders;

namespace TheMillionthFoodOrderApp.Infrastructure.Notifications;

/// <summary>
/// Wolverine message handler that bridges domain events to SignalR.
/// When <see cref="OrderStatusChangedEvent"/> is published, this handler
/// forwards the notification to connected clients via <see cref="IOrderNotificationService"/>.
///
/// Wolverine discovers this handler by convention: HandleAsync(OrderStatusChangedEvent).
/// </summary>
public sealed class OrderStatusChangedHandler(IOrderNotificationService notificationService)
{
    public async Task HandleAsync(OrderStatusChangedEvent @event, CancellationToken cancellationToken)
    {
        await notificationService.NotifyOrderStatusChangedAsync(
            @event.OrderId,
            @event.ShopId,
            @event.BrandSlug,
            @event.PreviousStatus,
            @event.NewStatus,
            @event.CustomerName,
            cancellationToken);
    }
}

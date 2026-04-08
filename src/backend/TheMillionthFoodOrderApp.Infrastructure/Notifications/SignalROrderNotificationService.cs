using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using TheMillionthFoodOrderApp.Application.Orders;

namespace TheMillionthFoodOrderApp.Infrastructure.Notifications;

/// <summary>
/// Sends real-time order notifications via SignalR to connected clients.
/// Uses IHubContext (not the Hub directly) to send from outside the hub pipeline.
/// </summary>
public sealed class SignalROrderNotificationService(
    IHubContext<OrderHub> hubContext,
    ILogger<SignalROrderNotificationService> logger) : IOrderNotificationService
{
    public async Task NotifyOrderStatusChangedAsync(
        Guid orderId,
        Guid shopId,
        string brandSlug,
        string previousStatus,
        string newStatus,
        string? customerName,
        CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            OrderId = orderId,
            ShopId = shopId,
            BrandSlug = brandSlug,
            PreviousStatus = previousStatus,
            NewStatus = newStatus,
            CustomerName = customerName,
            Timestamp = DateTimeOffset.UtcNow,
        };

        // Send to shop group (kitchen display, POS, floor staff)
        var shopGroup = $"shop:{brandSlug}:{shopId}";
        await hubContext.Clients.Group(shopGroup)
            .SendAsync("OrderStatusChanged", payload, cancellationToken);

        // Send to order-specific group (customer tracking)
        var orderGroup = $"order:{orderId}";
        await hubContext.Clients.Group(orderGroup)
            .SendAsync("OrderStatusChanged", payload, cancellationToken);

        logger.LogInformation(
            "Sent OrderStatusChanged notification for order {OrderId} " +
            "(shop: {ShopGroup}, status: {PreviousStatus} -> {NewStatus})",
            orderId, shopGroup, previousStatus, newStatus);
    }
}

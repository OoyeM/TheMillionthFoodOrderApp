using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace TheMillionthFoodOrderApp.Infrastructure.Notifications;

/// <summary>
/// SignalR hub for real-time order updates.
/// Clients join groups based on what they are monitoring:
///   - shop:{brandSlug}:{shopId} -- kitchen display, POS, floor staff
///   - order:{orderId} -- customer order tracking page
///
/// The hub does not push messages directly -- the server-side
/// <see cref="SignalROrderNotificationService"/> uses IHubContext to send messages to groups.
/// </summary>
[AllowAnonymous]
public sealed class OrderHub : Hub
{
    /// <summary>
    /// Join the group for a specific shop. Called by kitchen display and POS clients.
    /// </summary>
    public async Task JoinShopGroup(string brandSlug, string shopId)
    {
        var groupName = $"shop:{brandSlug}:{shopId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }

    /// <summary>
    /// Leave a shop group.
    /// </summary>
    public async Task LeaveShopGroup(string brandSlug, string shopId)
    {
        var groupName = $"shop:{brandSlug}:{shopId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }

    /// <summary>
    /// Join the group for a specific order. Called by customer tracking pages.
    /// </summary>
    public async Task JoinOrderGroup(string orderId)
    {
        var groupName = $"order:{orderId}";
        await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }

    /// <summary>
    /// Leave an order group.
    /// </summary>
    public async Task LeaveOrderGroup(string orderId)
    {
        var groupName = $"order:{orderId}";
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
    }
}

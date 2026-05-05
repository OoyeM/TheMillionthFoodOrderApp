using TheMillionthFoodOrderApp.Application.OrderLifecycle;

namespace TheMillionthFoodOrderApp.Application.Orders.Dtos;

/// <summary>
/// Combined response DTO returned by the order-tracking endpoints.
/// Bundles the full order detail with the shop's configured lifecycle so the
/// frontend can render the status progression without a second round-trip.
/// </summary>
public record OrderTrackingResponse(
    OrderResponse Order,
    OrderLifecycleResponse Lifecycle);

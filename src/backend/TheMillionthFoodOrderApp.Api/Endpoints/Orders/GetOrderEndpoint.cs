using FastEndpoints;
using TheMillionthFoodOrderApp.Application.OrderLifecycle;
using TheMillionthFoodOrderApp.Application.Orders.Dtos;
using TheMillionthFoodOrderApp.Domain.Orders;

namespace TheMillionthFoodOrderApp.Api.Endpoints.Orders;

public sealed record GetOrderRequest(
    [property: RouteParam] string BrandSlug,
    [property: RouteParam] Guid ShopId,
    [property: RouteParam] Guid OrderId);

/// <summary>
/// Retrieves order tracking details by Order UUID, enriched with the shop's
/// configured lifecycle so the customer can see their order's progression
/// without a second round-trip.
/// </summary>
public sealed class GetOrderEndpoint(IOrderRepository orderRepository, IOrderLifecycleService lifecycleService)
    : Endpoint<GetOrderRequest, OrderTrackingResponse>
{
    public const string Route = "/api/brands/{brandSlug}/shops/{shopId}/orders/{orderId}";

    public override void Configure()
    {
        Get(Route);
        AllowAnonymous();
        PreProcessor<BrandScopedPreProcessor<GetOrderRequest>>();
        Summary(s =>
        {
            s.Summary = "Get order tracking details by ID";
            s.Description =
                "Returns the full order detail and the shop's order lifecycle configuration. " +
                "Returns 404 if the order does not exist or belongs to a different shop.";
            s.Response<OrderTrackingResponse>(200, "Order tracking details retrieved successfully.");
            s.Response(404, "Order not found.");
        });
    }

    public override async Task HandleAsync(GetOrderRequest req, CancellationToken ct)
    {
        var order = await orderRepository.GetByIdAsync(req.OrderId, ct);

        // Return 404 when not found OR when the order belongs to a different shop —
        // never reveal existence via a 403.
        if (order is null || order.ShopId != req.ShopId)
        {
            await HttpContext.Response.SendNotFoundAsync(ct);
            return;
        }

        var lifecycle = await lifecycleService.GetLifecycleAsync(req.ShopId, ct);

        var response = new OrderTrackingResponse(
            OrderTrackingMapper.MapOrder(order),
            lifecycle);

        await HttpContext.Response.SendAsync(response, statusCode: 200, cancellation: ct);
    }
}

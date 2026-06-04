using FastEndpoints;
using TheMillionthFoodOrderApp.Application.OrderLifecycle;
using TheMillionthFoodOrderApp.Application.Orders;
using TheMillionthFoodOrderApp.Domain.Orders;
using TheMillionthFoodOrderApp.Domain.Shops;

namespace TheMillionthFoodOrderApp.Api.Endpoints.Orders;

public sealed record GetOrderByNumberRequest(
    [property: RouteParam] string BrandSlug,
    [property: RouteParam] Guid ShopId,
    [property: RouteParam] string OrderNumber);

/// <summary>
/// Retrieves order tracking details by human-readable order number, enriched with
/// the shop's configured lifecycle. Supports guest lookup where only the order
/// confirmation slip (with the order number) is available.
/// </summary>
public sealed class GetOrderByNumberEndpoint(
    IOrderRepository orderRepository,
    IOrderLifecycleService lifecycleService,
    IShopRepository shopRepository)
    : Endpoint<GetOrderByNumberRequest, OrderTrackingResponse>
{
    public const string Route = "/api/brands/{brandSlug}/shops/{shopId}/orders/number/{orderNumber}";

    public override void Configure()
    {
        Get(Route);
        AllowAnonymous();
        PreProcessor<BrandScopedPreProcessor<GetOrderByNumberRequest>>();
        Summary(s =>
        {
            s.Summary = "Get order tracking details by order number";
            s.Description =
                "Returns the full order detail and the shop's order lifecycle configuration. " +
                "Allows guest lookup using only the human-readable order number printed on the receipt. " +
                "Returns 404 if the order does not exist or belongs to a different shop.";
            s.Response<OrderTrackingResponse>(200, "Order tracking details retrieved successfully.");
            s.Response(404, "Order not found.");
        });
    }

    public override async Task HandleAsync(GetOrderByNumberRequest req, CancellationToken ct)
    {
        var order = await orderRepository.GetByOrderNumberAsync(req.ShopId, req.OrderNumber, ct);

        // Return 404 when not found — shopId is already scoped in the query so a mismatch
        // simply returns null. Never reveal existence via a 403.
        if (order is null)
        {
            await HttpContext.Response.SendNotFoundAsync(ct);
            return;
        }

        var lifecycle = await lifecycleService.GetLifecycleAsync(req.ShopId, ct);

        // Load the shop so the seller legal block (name, VAT number, address) is included
        // for receipt reprints (US-FP-052).
        var shop = await shopRepository.GetByIdAsync(req.ShopId, ct);

        var response = new OrderTrackingResponse(
            OrderTrackingMapper.MapOrder(order, shop),
            lifecycle);

        await HttpContext.Response.SendAsync(response, statusCode: 200, cancellation: ct);
    }
}

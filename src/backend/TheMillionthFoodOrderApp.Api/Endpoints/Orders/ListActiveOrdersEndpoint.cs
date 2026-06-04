using FastEndpoints;
using TheMillionthFoodOrderApp.Application.Orders.Dtos;
using TheMillionthFoodOrderApp.Domain.Orders;

namespace TheMillionthFoodOrderApp.Api.Endpoints.Orders;

public sealed record ListActiveOrdersRequest(
    [property: RouteParam] string BrandSlug,
    [property: RouteParam] Guid ShopId);

public sealed record ListActiveOrdersResponse(IReadOnlyList<OrderResponse> Orders);

/// <summary>
/// Lists the active orders for a shop — those whose status is not a terminal status
/// in the shop's lifecycle configuration. Backs the kitchen display screen (US-FP-027).
/// Sorted by creation time ascending (oldest first).
/// </summary>
public sealed class ListActiveOrdersEndpoint(IOrderRepository orderRepository)
    : Endpoint<ListActiveOrdersRequest, ListActiveOrdersResponse>
{
    public const string Route = "/api/brands/{brandSlug}/shops/{shopId}/orders/active";

    public override void Configure()
    {
        Get(Route);
        // TODO (US-FP-039): require staff role once hub/endpoint authorization is wired.
        AllowAnonymous();
        PreProcessor<BrandScopedPreProcessor<ListActiveOrdersRequest>>();
        Summary(s =>
        {
            s.Summary = "List active orders for a shop (kitchen display)";
            s.Description =
                "Returns the orders whose status is not terminal in the shop's lifecycle, " +
                "sorted by creation time ascending. Used by the kitchen display screen.";
            s.Response<ListActiveOrdersResponse>(200, "Active orders retrieved successfully.");
        });
    }

    public override async Task HandleAsync(ListActiveOrdersRequest req, CancellationToken ct)
    {
        var orders = await orderRepository.GetActiveByShopAsync(req.ShopId, ct);

        // Kitchen display renders order tickets (not customer receipts), so the seller
        // legal block is left null — MapOrder is called with the order only.
        var response = new ListActiveOrdersResponse(
            orders.Select(o => OrderTrackingMapper.MapOrder(o)).ToList().AsReadOnly());

        await HttpContext.Response.SendAsync(response, statusCode: 200, cancellation: ct);
    }
}

using FastEndpoints;
using FluentValidation.Results;
using TheMillionthFoodOrderApp.Application.Orders;
using TheMillionthFoodOrderApp.Application.Orders.Dtos;

namespace TheMillionthFoodOrderApp.Api.Endpoints.Orders;

public sealed record AdvanceOrderStatusRequest(
    [property: RouteParam] string BrandSlug,
    [property: RouteParam] Guid ShopId,
    [property: RouteParam] Guid OrderId,
    Guid ToStatusId);

/// <summary>
/// Advances an order to the next status in the shop's lifecycle (US-FP-023).
/// Backs the "tap to advance" action on the kitchen display. The transition must be
/// configured in the shop's <c>OrderLifecycleConfig</c>; the change is pushed in real
/// time to kitchen displays, the POS, and the customer's tracking page via SignalR.
/// </summary>
public sealed class AdvanceOrderStatusEndpoint(IOrderService orderService)
    : Endpoint<AdvanceOrderStatusRequest, OrderResponse>
{
    public const string Route = "/api/brands/{brandSlug}/shops/{shopId}/orders/{orderId}/status";

    public override void Configure()
    {
        Post(Route);
        // TODO (US-FP-039/069): require a staff role once per-endpoint RBAC is enforced.
        AllowAnonymous();
        PreProcessor<BrandScopedPreProcessor<AdvanceOrderStatusRequest>>();
        Summary(s =>
        {
            s.Summary = "Advance an order to the next lifecycle status (kitchen display)";
            s.Description =
                "Moves the order to the requested status, provided the transition is " +
                "configured in the shop's order lifecycle. Pushes a real-time update to " +
                "connected kitchen displays, POS, and the customer's tracking page.";
            s.Response<OrderResponse>(200, "Order status advanced successfully.");
            s.Response(400, "The requested transition is not allowed for this shop.");
            s.Response(404, "Order, target status, brand, or shop not found.");
        });
    }

    public override async Task HandleAsync(AdvanceOrderStatusRequest req, CancellationToken ct)
    {
        try
        {
            var response = await orderService.AdvanceOrderStatusAsync(
                req.ShopId, req.OrderId, req.ToStatusId, ct);

            await HttpContext.Response.SendAsync(response, statusCode: 200, cancellation: ct);
        }
        catch (KeyNotFoundException ex)
        {
            await HttpContext.Response.SendNotFoundAsync(ct);
            Logger.LogInformation("Advance-status 404: {Message}", ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            var failures = new List<ValidationFailure> { new("toStatusId", ex.Message) };
            await HttpContext.Response.SendErrorsAsync(failures, statusCode: 400, cancellation: ct);
        }
        catch (ArgumentException ex)
        {
            var failures = new List<ValidationFailure> { new("toStatusId", ex.Message) };
            await HttpContext.Response.SendErrorsAsync(failures, statusCode: 400, cancellation: ct);
        }
    }
}

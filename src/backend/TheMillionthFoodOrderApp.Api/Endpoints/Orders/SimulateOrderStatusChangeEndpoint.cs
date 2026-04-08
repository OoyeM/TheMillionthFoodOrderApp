using FastEndpoints;
using TheMillionthFoodOrderApp.Domain.Orders;
using Wolverine;

namespace TheMillionthFoodOrderApp.Api.Endpoints.Orders;

public sealed record SimulateOrderStatusChangeRequest(
    [property: RouteParam] string BrandSlug,
    Guid ShopId,
    Guid? OrderId,
    string PreviousStatus,
    string NewStatus,
    string? CustomerName);

public sealed record SimulateOrderStatusChangeResponse(Guid OrderId, string Message);

/// <summary>
/// Development-only endpoint that publishes a simulated OrderStatusChangedEvent
/// via Wolverine so the SignalR infrastructure can be tested end-to-end
/// before the Order aggregate is implemented (US-FP-016).
/// </summary>
public sealed class SimulateOrderStatusChangeEndpoint(IMessageBus messageBus)
    : Endpoint<SimulateOrderStatusChangeRequest, SimulateOrderStatusChangeResponse>
{
    public override void Configure()
    {
        Post("/api/brands/{brandSlug}/orders/simulate-status-change");
        AllowAnonymous();
        PreProcessor<BrandScopedPreProcessor<SimulateOrderStatusChangeRequest>>();
        Tags("development");
        Summary(s =>
        {
            s.Summary = "Simulate an order status change (dev only)";
            s.Description = "Publishes a simulated OrderStatusChangedEvent to test the SignalR real-time notification pipeline.";
        });
    }

    public override async Task HandleAsync(SimulateOrderStatusChangeRequest req, CancellationToken ct)
    {
        var orderId = req.OrderId ?? Guid.CreateVersion7();

        var @event = new OrderStatusChangedEvent(
            orderId,
            req.ShopId,
            req.BrandSlug,
            req.PreviousStatus,
            req.NewStatus,
            req.CustomerName);

        await messageBus.PublishAsync(@event);

        await HttpContext.Response.SendAsync(
            new SimulateOrderStatusChangeResponse(orderId, "Event published"),
            statusCode: 200,
            cancellation: ct);
    }
}

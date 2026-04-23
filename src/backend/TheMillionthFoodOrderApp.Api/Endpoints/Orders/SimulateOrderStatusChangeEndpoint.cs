using FastEndpoints;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
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
///
/// Note: this endpoint remains in the production route table but returns 404
/// outside Development. FastEndpoints does not support conditional registration,
/// so the runtime guard is the simplest approach.
/// </summary>
public sealed class SimulateOrderStatusChangeEndpoint(IMessageBus messageBus, IWebHostEnvironment env)
    : Endpoint<SimulateOrderStatusChangeRequest, SimulateOrderStatusChangeResponse>
{
    public const string Route = "/api/brands/{brandSlug}/orders/simulate-status-change";

    public override void Configure()
    {
        Post(Route);
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
        if (!env.IsDevelopment() && !env.IsEnvironment("Testing"))
        {
            await HttpContext.Response.SendNotFoundAsync(ct);
            return;
        }

        var orderId = req.OrderId ?? Guid.CreateVersion7();

        var @event = new OrderStatusChangedEvent(
            orderId,
            req.ShopId,
            req.BrandSlug,
            req.PreviousStatus,
            req.NewStatus,
            req.CustomerName);

        await messageBus.InvokeAsync(@event, ct);

        await HttpContext.Response.SendAsync(
            new SimulateOrderStatusChangeResponse(orderId, "Event published"),
            statusCode: 200,
            cancellation: ct);
    }
}

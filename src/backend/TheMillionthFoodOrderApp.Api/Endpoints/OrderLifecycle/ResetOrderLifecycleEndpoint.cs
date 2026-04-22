using FastEndpoints;
using TheMillionthFoodOrderApp.Application.OrderLifecycle;

namespace TheMillionthFoodOrderApp.Api.Endpoints.OrderLifecycle;

public sealed record ResetOrderLifecycleRequest(
    [property: RouteParam] string BrandSlug,
    [property: RouteParam] Guid ShopId);

public sealed class ResetOrderLifecycleEndpoint(IOrderLifecycleService service)
    : Endpoint<ResetOrderLifecycleRequest, OrderLifecycleResponse>
{
    public const string Route = "/api/brands/{brandSlug}/shops/{shopId}/order-lifecycle/reset";

    public override void Configure()
    {
        Post(Route);
        // TODO: Require ShopManager role when auth is implemented (US-FP-039)
        AllowAnonymous();
        PreProcessor<BrandScopedPreProcessor<ResetOrderLifecycleRequest>>();
        Summary(s =>
        {
            s.Summary = "Reset order lifecycle to default";
            s.Description = "Resets the order lifecycle configuration to the default: Placed > Confirmed > Preparing > Ready > Picked Up / Delivered.";
            s.Response<OrderLifecycleResponse>(200, "Order lifecycle reset to default.");
            s.Response(404, "Shop not found.");
        });
    }

    public override async Task HandleAsync(ResetOrderLifecycleRequest req, CancellationToken ct)
    {
        try
        {
            var response = await service.ResetToDefaultAsync(req.ShopId, ct);
            await HttpContext.Response.SendAsync(response, statusCode: 200, cancellation: ct);
        }
        catch (KeyNotFoundException)
        {
            await HttpContext.Response.SendNotFoundAsync(ct);
        }
    }
}

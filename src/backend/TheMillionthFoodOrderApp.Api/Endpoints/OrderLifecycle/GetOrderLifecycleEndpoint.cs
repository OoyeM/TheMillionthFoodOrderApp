using FastEndpoints;
using TheMillionthFoodOrderApp.Application.OrderLifecycle;

namespace TheMillionthFoodOrderApp.Api.Endpoints.OrderLifecycle;

public sealed record GetOrderLifecycleRequest(
    [property: RouteParam] string BrandSlug,
    [property: RouteParam] Guid ShopId);

public sealed class GetOrderLifecycleEndpoint(IOrderLifecycleService service)
    : Endpoint<GetOrderLifecycleRequest, OrderLifecycleResponse>
{
    public override void Configure()
    {
        Get("/api/brands/{brandSlug}/shops/{shopId}/order-lifecycle");
        AllowAnonymous();
        PreProcessor<BrandScopedPreProcessor<GetOrderLifecycleRequest>>();
        Summary(s =>
        {
            s.Summary = "Get order lifecycle configuration for a shop";
            s.Description = "Returns the configured order statuses and transitions. Creates a default lifecycle on first access.";
            s.Response<OrderLifecycleResponse>(200, "Order lifecycle retrieved successfully.");
            s.Response(404, "Shop not found.");
        });
    }

    public override async Task HandleAsync(GetOrderLifecycleRequest req, CancellationToken ct)
    {
        try
        {
            var response = await service.GetLifecycleAsync(req.ShopId, ct);
            await HttpContext.Response.SendAsync(response, statusCode: 200, cancellation: ct);
        }
        catch (KeyNotFoundException)
        {
            await HttpContext.Response.SendNotFoundAsync(ct);
        }
    }
}

using FastEndpoints;
using TheMillionthFoodOrderApp.Application.Shops;

namespace TheMillionthFoodOrderApp.Api.Endpoints.Shops;

public sealed record GetShopStatusRequest(
    [property: RouteParam] string BrandSlug,
    [property: RouteParam] Guid Id);

public sealed class GetShopStatusEndpoint(IOpeningHoursService openingHoursService)
    : Endpoint<GetShopStatusRequest, ShopStatusResponse>
{
    public const string Route = "/api/brands/{brandSlug}/shops/{id}/status";

    public override void Configure()
    {
        Get(Route);
        AllowAnonymous();
        PreProcessor<BrandScopedPreProcessor<GetShopStatusRequest>>();
        Summary(s =>
        {
            s.Summary = "Get real-time open/closed status for a shop";
            s.Description = "Returns whether the shop is currently open, and the next opening time if it is closed.";
            s.Response<ShopStatusResponse>(200, "Status retrieved successfully.");
            s.Response(404, "Shop not found.");
        });
    }

    public override async Task HandleAsync(GetShopStatusRequest req, CancellationToken ct)
    {
        try
        {
            var response = await openingHoursService.GetShopStatusAsync(req.Id, ct);
            await HttpContext.Response.SendAsync(response, statusCode: 200, cancellation: ct);
        }
        catch (KeyNotFoundException)
        {
            await HttpContext.Response.SendNotFoundAsync(ct);
        }
    }
}

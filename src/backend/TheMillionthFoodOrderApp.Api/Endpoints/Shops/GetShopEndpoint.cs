using FastEndpoints;
using TheMillionthFoodOrderApp.Application.Shops;

namespace TheMillionthFoodOrderApp.Api.Endpoints.Shops;

public sealed record GetShopRequest(
    [property: RouteParam] string BrandSlug,
    [property: RouteParam] Guid Id);

public sealed class GetShopEndpoint(IShopService shopService)
    : Endpoint<GetShopRequest, ShopResponse>
{
    public const string Route = "/api/brands/{brandSlug}/shops/{id}";

    public override void Configure()
    {
        Get(Route);
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get a shop by id";
            s.Response<ShopResponse>(200, "Shop found.");
            s.Response(404, "Shop not found.");
        });
    }

    public override async Task HandleAsync(GetShopRequest req, CancellationToken ct)
    {
        try
        {
            var response = await shopService.GetShopAsync(req.Id, ct);
            await HttpContext.Response.SendAsync(response, statusCode: 200, cancellation: ct);
        }
        catch (KeyNotFoundException)
        {
            await HttpContext.Response.SendNotFoundAsync(ct);
        }
    }
}

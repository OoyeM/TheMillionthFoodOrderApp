using FastEndpoints;
using TheMillionthFoodOrderApp.Application.Shops;

namespace TheMillionthFoodOrderApp.Api.Endpoints.Shops;

public sealed record DeactivateShopRequest(
    [property: RouteParam] string BrandSlug,
    [property: RouteParam] Guid Id);

public sealed class DeactivateShopEndpoint(IShopService shopService)
    : Endpoint<DeactivateShopRequest>
{
    public const string Route = "/api/brands/{brandSlug}/shops/{id}/deactivate";

    public override void Configure()
    {
        Post(Route);
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Deactivate a shop";
            s.Description = "Brand Admin deactivates a shop, hiding it from customers. The shop can be re-activated later.";
            s.Response(204, "Shop deactivated successfully.");
            s.Response(404, "Shop not found.");
        });
    }

    public override async Task HandleAsync(DeactivateShopRequest req, CancellationToken ct)
    {
        try
        {
            await shopService.DeactivateShopAsync(req.Id, ct);
            await HttpContext.Response.SendNoContentAsync(ct);
        }
        catch (KeyNotFoundException)
        {
            await HttpContext.Response.SendNotFoundAsync(ct);
        }
    }
}

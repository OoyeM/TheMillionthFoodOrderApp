using FastEndpoints;
using TheMillionthFoodOrderApp.Application.Shops;

namespace TheMillionthFoodOrderApp.Api.Endpoints.Shops;

public sealed record ActivateShopRequest(
    [property: RouteParam] string BrandSlug,
    [property: RouteParam] Guid Id);

public sealed class ActivateShopEndpoint(IShopService shopService)
    : Endpoint<ActivateShopRequest>
{
    public override void Configure()
    {
        Post("/api/brands/{brandSlug}/shops/{id}/activate");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Activate a shop";
            s.Description = "Brand Admin re-activates a previously deactivated shop, making it visible to customers again.";
            s.Response(204, "Shop activated successfully.");
            s.Response(404, "Shop not found.");
        });
    }

    public override async Task HandleAsync(ActivateShopRequest req, CancellationToken ct)
    {
        try
        {
            await shopService.ActivateShopAsync(req.Id, ct);
            await HttpContext.Response.SendNoContentAsync(ct);
        }
        catch (KeyNotFoundException)
        {
            await HttpContext.Response.SendNotFoundAsync(ct);
        }
    }
}

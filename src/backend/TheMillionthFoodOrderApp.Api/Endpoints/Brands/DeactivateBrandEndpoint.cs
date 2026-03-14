using FastEndpoints;
using TheMillionthFoodOrderApp.Application.Brands;

namespace TheMillionthFoodOrderApp.Api.Endpoints.Brands;

public sealed record DeactivateBrandRequest([property: RouteParam] Guid Id);

public sealed class DeactivateBrandEndpoint(IBrandService brandService)
    : Endpoint<DeactivateBrandRequest>
{
    public override void Configure()
    {
        Post("/api/brands/{id}/deactivate");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Deactivate a brand";
            s.Description = "Platform Admin deactivates a brand. All its shops and storefronts are disabled and the brand can no longer accept new orders.";
            s.Response(204, "Brand deactivated successfully.");
            s.Response(404, "Brand not found.");
        });
    }

    public override async Task HandleAsync(DeactivateBrandRequest req, CancellationToken ct)
    {
        try
        {
            await brandService.DeactivateBrandAsync(req.Id, ct);
            await HttpContext.Response.SendNoContentAsync(ct);
        }
        catch (KeyNotFoundException)
        {
            await HttpContext.Response.SendNotFoundAsync(ct);
        }
    }
}

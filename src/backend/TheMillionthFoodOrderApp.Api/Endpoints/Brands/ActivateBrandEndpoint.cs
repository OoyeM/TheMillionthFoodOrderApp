using FastEndpoints;
using TheMillionthFoodOrderApp.Application.Brands;

namespace TheMillionthFoodOrderApp.Api.Endpoints.Brands;

public sealed record ActivateBrandRequest([property: RouteParam] Guid Id);

public sealed class ActivateBrandEndpoint(IBrandService brandService)
    : Endpoint<ActivateBrandRequest>
{
    public override void Configure()
    {
        Post("/api/brands/{id}/activate");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Activate a brand";
            s.Description = "Platform Admin re-activates a previously deactivated brand.";
            s.Response(204, "Brand activated successfully.");
            s.Response(404, "Brand not found.");
        });
    }

    public override async Task HandleAsync(ActivateBrandRequest req, CancellationToken ct)
    {
        try
        {
            await brandService.ActivateBrandAsync(req.Id, ct);
            await HttpContext.Response.SendNoContentAsync(ct);
        }
        catch (KeyNotFoundException)
        {
            await HttpContext.Response.SendNotFoundAsync(ct);
        }
    }
}

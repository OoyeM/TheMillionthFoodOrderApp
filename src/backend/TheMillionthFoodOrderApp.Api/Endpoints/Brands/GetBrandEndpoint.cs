using FastEndpoints;
using TheMillionthFoodOrderApp.Application.Brands;

namespace TheMillionthFoodOrderApp.Api.Endpoints.Brands;

public sealed record GetBrandRequest([property: RouteParam] Guid Id);

public sealed class GetBrandEndpoint(IBrandService brandService)
    : Endpoint<GetBrandRequest, BrandResponse>
{
    public override void Configure()
    {
        Get("/api/brands/{id}");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get a brand by id";
            s.Response<BrandResponse>(200, "Brand found.");
            s.Response(404, "Brand not found.");
        });
    }

    public override async Task HandleAsync(GetBrandRequest req, CancellationToken ct)
    {
        try
        {
            var response = await brandService.GetBrandAsync(req.Id, ct);
            await HttpContext.Response.SendAsync(response, statusCode: 200, cancellation: ct);
        }
        catch (KeyNotFoundException)
        {
            await HttpContext.Response.SendNotFoundAsync(ct);
        }
    }
}

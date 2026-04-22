using FastEndpoints;
using TheMillionthFoodOrderApp.Application.Brands;

namespace TheMillionthFoodOrderApp.Api.Endpoints.Brands;

public sealed class ListBrandsEndpoint(IBrandService brandService)
    : EndpointWithoutRequest<IReadOnlyList<BrandResponse>>
{
    public const string Route = "/api/brands";

    public override void Configure()
    {
        Get(Route);
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "List all brands";
            s.Description = "Returns all registered brands ordered by name.";
            s.Response<IReadOnlyList<BrandResponse>>(200, "List of brands.");
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var response = await brandService.GetBrandsAsync(ct);
        await HttpContext.Response.SendAsync(response, statusCode: 200, cancellation: ct);
    }
}

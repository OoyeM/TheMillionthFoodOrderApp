using FastEndpoints;
using TheMillionthFoodOrderApp.Application.Products;

namespace TheMillionthFoodOrderApp.Api.Endpoints.Products;

public sealed record GetProductRequest(
    [property: RouteParam] string BrandSlug,
    [property: RouteParam] Guid Id);

public sealed class GetProductEndpoint(IProductService productService)
    : Endpoint<GetProductRequest, ProductResponse>
{
    public const string Route = "/api/brands/{brandSlug}/products/{id}";

    public override void Configure()
    {
        Get(Route);
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get a product by id";
            s.Response<ProductResponse>(200, "Product found.");
            s.Response(404, "Product not found.");
        });
    }

    public override async Task HandleAsync(GetProductRequest req, CancellationToken ct)
    {
        try
        {
            var response = await productService.GetProductAsync(req.Id, ct);
            await HttpContext.Response.SendAsync(response, statusCode: 200, cancellation: ct);
        }
        catch (KeyNotFoundException)
        {
            await HttpContext.Response.SendNotFoundAsync(ct);
        }
    }
}

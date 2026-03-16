using FastEndpoints;
using TheMillionthFoodOrderApp.Application.Products;

namespace TheMillionthFoodOrderApp.Api.Endpoints.Products;

public sealed record ListProductsRequest([property: RouteParam] string BrandSlug);

public sealed class ListProductsEndpoint(IProductService productService)
    : Endpoint<ListProductsRequest, IReadOnlyList<ProductListItemResponse>>
{
    public override void Configure()
    {
        Get("/api/brands/{brandSlug}/products");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "List all products for a brand";
            s.Description = "Returns all non-deleted products belonging to the brand, ordered by creation date.";
            s.Response<IReadOnlyList<ProductListItemResponse>>(200, "List of products.");
        });
    }

    public override async Task HandleAsync(ListProductsRequest req, CancellationToken ct)
    {
        var response = await productService.GetProductsAsync(ct);
        await HttpContext.Response.SendAsync(response, statusCode: 200, cancellation: ct);
    }
}

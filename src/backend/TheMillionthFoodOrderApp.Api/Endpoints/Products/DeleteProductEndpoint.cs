using FastEndpoints;
using FluentValidation.Results;
using TheMillionthFoodOrderApp.Application.Products;

namespace TheMillionthFoodOrderApp.Api.Endpoints.Products;

public sealed record DeleteProductRequest(
    [property: RouteParam] string BrandSlug,
    [property: RouteParam] Guid Id);

public sealed class DeleteProductEndpoint(IProductService productService)
    : Endpoint<DeleteProductRequest>
{
    public const string Route = "/api/brands/{brandSlug}/products/{id}";

    public override void Configure()
    {
        Delete(Route);
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Delete a product (soft-delete)";
            s.Description = "Brand Admin soft-deletes a product. Hidden from storefronts but retained for historical order records.";
            s.Response(204, "Product deleted successfully.");
            s.Response(404, "Product not found.");
            s.Response(409, "Product is a component of one or more combo products.");
        });
    }

    public override async Task HandleAsync(DeleteProductRequest req, CancellationToken ct)
    {
        try
        {
            await productService.DeleteProductAsync(req.Id, ct);
            await HttpContext.Response.SendNoContentAsync(ct);
        }
        catch (KeyNotFoundException)
        {
            await HttpContext.Response.SendNotFoundAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            var failures = new List<ValidationFailure> { new("Id", ex.Message) };
            await HttpContext.Response.SendErrorsAsync(failures, statusCode: 409, cancellation: ct);
        }
    }
}

using FastEndpoints;
using FluentValidation;
using TheMillionthFoodOrderApp.Application.MenuCategories;
using TheMillionthFoodOrderApp.Application.Products;

namespace TheMillionthFoodOrderApp.Api.Endpoints.MenuCategories;

public sealed record ListCategoryProductsRequest(
    [property: RouteParam] string BrandSlug,
    [property: RouteParam] Guid Id);

public sealed class ListCategoryProductsRequestValidator : Validator<ListCategoryProductsRequest>
{
    public ListCategoryProductsRequestValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Menu category id is required.");
    }
}

public sealed class ListCategoryProductsEndpoint(IMenuCategoryService menuCategoryService)
    : Endpoint<ListCategoryProductsRequest, IReadOnlyList<ProductListItemResponse>>
{
    public override void Configure()
    {
        Get("/api/brands/{brandSlug}/menu-categories/{id}/products");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "List products in a menu category";
            s.Description = "Returns all products assigned to the specified menu category, ordered by their display position (SortOrderInCategory ascending).";
            s.Response<IReadOnlyList<ProductListItemResponse>>(200, "Products in the category, sorted by display order.");
            s.Response(404, "Menu category not found.");
        });
    }

    public override async Task HandleAsync(ListCategoryProductsRequest req, CancellationToken ct)
    {
        try
        {
            var response = await menuCategoryService.GetCategoryProductsAsync(req.Id, ct);
            await HttpContext.Response.SendAsync(response, statusCode: 200, cancellation: ct);
        }
        catch (KeyNotFoundException)
        {
            await HttpContext.Response.SendNotFoundAsync(ct);
        }
    }
}

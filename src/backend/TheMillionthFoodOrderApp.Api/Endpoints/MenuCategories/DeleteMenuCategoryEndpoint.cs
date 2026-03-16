using FastEndpoints;
using TheMillionthFoodOrderApp.Application.MenuCategories;

namespace TheMillionthFoodOrderApp.Api.Endpoints.MenuCategories;

public sealed record DeleteMenuCategoryRequest(
    [property: RouteParam] string BrandSlug,
    [property: RouteParam] Guid Id);

public sealed class DeleteMenuCategoryEndpoint(IMenuCategoryService menuCategoryService)
    : Endpoint<DeleteMenuCategoryRequest>
{
    public override void Configure()
    {
        Delete("/api/brands/{brandSlug}/menu-categories/{id}");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Delete a menu category (soft-delete)";
            s.Description = "Brand Admin soft-deletes a menu category. Hidden from storefronts but retained for historical data. Products in this category become uncategorised.";
            s.Response(204, "Menu category deleted successfully.");
            s.Response(404, "Menu category not found.");
        });
    }

    public override async Task HandleAsync(DeleteMenuCategoryRequest req, CancellationToken ct)
    {
        try
        {
            await menuCategoryService.DeleteMenuCategoryAsync(req.Id, ct);
            await HttpContext.Response.SendNoContentAsync(ct);
        }
        catch (KeyNotFoundException)
        {
            await HttpContext.Response.SendNotFoundAsync(ct);
        }
    }
}

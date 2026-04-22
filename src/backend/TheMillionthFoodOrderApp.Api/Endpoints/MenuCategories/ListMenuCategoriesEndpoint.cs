using FastEndpoints;
using TheMillionthFoodOrderApp.Application.MenuCategories;

namespace TheMillionthFoodOrderApp.Api.Endpoints.MenuCategories;

public sealed record ListMenuCategoriesRequest([property: RouteParam] string BrandSlug);

public sealed class ListMenuCategoriesEndpoint(IMenuCategoryService menuCategoryService)
    : Endpoint<ListMenuCategoriesRequest, IReadOnlyList<MenuCategoryListItemResponse>>
{
    public const string Route = "/api/brands/{brandSlug}/menu-categories";

    public override void Configure()
    {
        Get(Route);
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "List all menu categories for a brand";
            s.Description = "Returns all non-deleted menu categories belonging to the brand, ordered by sort order then creation date.";
            s.Response<IReadOnlyList<MenuCategoryListItemResponse>>(200, "List of menu categories.");
        });
    }

    public override async Task HandleAsync(ListMenuCategoriesRequest req, CancellationToken ct)
    {
        var response = await menuCategoryService.GetMenuCategoriesAsync(ct);
        await HttpContext.Response.SendAsync(response, statusCode: 200, cancellation: ct);
    }
}

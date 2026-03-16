using FastEndpoints;
using TheMillionthFoodOrderApp.Application.MenuCategories;

namespace TheMillionthFoodOrderApp.Api.Endpoints.MenuCategories;

public sealed record GetMenuCategoryRequest(
    [property: RouteParam] string BrandSlug,
    [property: RouteParam] Guid Id);

public sealed class GetMenuCategoryEndpoint(IMenuCategoryService menuCategoryService)
    : Endpoint<GetMenuCategoryRequest, MenuCategoryResponse>
{
    public override void Configure()
    {
        Get("/api/brands/{brandSlug}/menu-categories/{id}");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get a menu category by id";
            s.Response<MenuCategoryResponse>(200, "Menu category found.");
            s.Response(404, "Menu category not found.");
        });
    }

    public override async Task HandleAsync(GetMenuCategoryRequest req, CancellationToken ct)
    {
        try
        {
            var response = await menuCategoryService.GetMenuCategoryAsync(req.Id, ct);
            await HttpContext.Response.SendAsync(response, statusCode: 200, cancellation: ct);
        }
        catch (KeyNotFoundException)
        {
            await HttpContext.Response.SendNotFoundAsync(ct);
        }
    }
}

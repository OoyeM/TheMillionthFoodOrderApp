using FastEndpoints;
using FluentValidation;
using TheMillionthFoodOrderApp.Application.MenuCategories;

namespace TheMillionthFoodOrderApp.Api.Endpoints.MenuCategories;

public sealed record ReorderMenuCategoryApiRequest(
    [property: RouteParam] string BrandSlug,
    [property: RouteParam] Guid Id,
    int SortOrder);

public sealed class ReorderMenuCategoryRequestValidator : Validator<ReorderMenuCategoryApiRequest>
{
    public ReorderMenuCategoryRequestValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Menu category id is required.");

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Sort order must be zero or greater.");
    }
}

public sealed class ReorderMenuCategoryEndpoint(IMenuCategoryService menuCategoryService)
    : Endpoint<ReorderMenuCategoryApiRequest>
{
    public override void Configure()
    {
        Patch("/api/brands/{brandSlug}/menu-categories/{id}/sort-order");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Update a menu category's display order";
            s.Description = "Brand Admin updates the sort order of a single category. Use this to reposition a category in the menu.";
            s.Response(204, "Sort order updated successfully.");
            s.Response(400, "Validation error.");
            s.Response(404, "Menu category not found.");
        });
    }

    public override async Task HandleAsync(ReorderMenuCategoryApiRequest req, CancellationToken ct)
    {
        try
        {
            var appRequest = new ReorderMenuCategoryRequest(req.SortOrder);
            await menuCategoryService.ReorderMenuCategoryAsync(req.Id, appRequest, ct);
            await HttpContext.Response.SendNoContentAsync(ct);
        }
        catch (KeyNotFoundException)
        {
            await HttpContext.Response.SendNotFoundAsync(ct);
        }
    }
}

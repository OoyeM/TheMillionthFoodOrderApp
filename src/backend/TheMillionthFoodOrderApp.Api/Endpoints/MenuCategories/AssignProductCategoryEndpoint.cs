using FastEndpoints;
using FluentValidation;
using TheMillionthFoodOrderApp.Application.MenuCategories;

namespace TheMillionthFoodOrderApp.Api.Endpoints.MenuCategories;

public sealed record AssignProductCategoryApiRequest(
    [property: RouteParam] string BrandSlug,
    Guid ProductId,
    Guid CategoryId);

public sealed class AssignProductCategoryRequestValidator : Validator<AssignProductCategoryApiRequest>
{
    public AssignProductCategoryRequestValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product id is required.");

        RuleFor(x => x.CategoryId)
            .NotEmpty().WithMessage("Category id is required.");
    }
}

public sealed class AssignProductCategoryEndpoint(IMenuCategoryService menuCategoryService)
    : Endpoint<AssignProductCategoryApiRequest>
{
    public const string Route = "/api/brands/{brandSlug}/menu-categories/assign-product";

    public override void Configure()
    {
        Post(Route);
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Assign a product to a menu category";
            s.Description = "Brand Admin assigns a product to a specific menu category. Replaces any existing category assignment.";
            s.Response(204, "Product assigned to category successfully.");
            s.Response(400, "Validation error.");
            s.Response(404, "Product or menu category not found.");
        });
    }

    public override async Task HandleAsync(AssignProductCategoryApiRequest req, CancellationToken ct)
    {
        try
        {
            var appRequest = new AssignProductCategoryRequest(req.ProductId, req.CategoryId);
            await menuCategoryService.AssignProductCategoryAsync(appRequest, ct);
            await HttpContext.Response.SendNoContentAsync(ct);
        }
        catch (KeyNotFoundException)
        {
            await HttpContext.Response.SendNotFoundAsync(ct);
        }
    }
}

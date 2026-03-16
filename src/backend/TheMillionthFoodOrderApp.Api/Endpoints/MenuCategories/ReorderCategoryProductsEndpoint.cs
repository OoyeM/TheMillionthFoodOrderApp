using FastEndpoints;
using FluentValidation;
using FluentValidation.Results;
using TheMillionthFoodOrderApp.Application.MenuCategories;

namespace TheMillionthFoodOrderApp.Api.Endpoints.MenuCategories;

public sealed record ReorderCategoryProductsApiRequest(
    [property: RouteParam] string BrandSlug,
    [property: RouteParam] Guid Id,
    IReadOnlyList<Guid> ProductIds);

public sealed class ReorderCategoryProductsRequestValidator : Validator<ReorderCategoryProductsApiRequest>
{
    public ReorderCategoryProductsRequestValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Menu category id is required.");

        RuleFor(x => x.ProductIds)
            .NotEmpty().WithMessage("At least one product id is required.");

        RuleFor(x => x.ProductIds)
            .Must(ids => ids.Count == ids.Distinct().Count())
            .WithMessage("Duplicate product ids are not allowed.");
    }
}

public sealed class ReorderCategoryProductsEndpoint(IMenuCategoryService menuCategoryService)
    : Endpoint<ReorderCategoryProductsApiRequest>
{
    public override void Configure()
    {
        Put("/api/brands/{brandSlug}/menu-categories/{id}/products/order");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Reorder products within a menu category";
            s.Description = "Brand Admin submits a full ordered list of product IDs. Products are assigned positions 0..n-1 sequentially. All provided IDs must belong to this category.";
            s.Response(204, "Products reordered successfully.");
            s.Response(400, "Validation error or products do not belong to the category.");
            s.Response(404, "Menu category not found.");
        });
    }

    public override async Task HandleAsync(ReorderCategoryProductsApiRequest req, CancellationToken ct)
    {
        try
        {
            var appRequest = new ReorderProductsInCategoryRequest(req.ProductIds);
            await menuCategoryService.ReorderProductsInCategoryAsync(req.Id, appRequest, ct);
            await HttpContext.Response.SendNoContentAsync(ct);
        }
        catch (KeyNotFoundException)
        {
            await HttpContext.Response.SendNotFoundAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            var failures = new List<ValidationFailure>
            {
                new("productIds", ex.Message)
            };
            await HttpContext.Response.SendErrorsAsync(failures, statusCode: 400, cancellation: ct);
        }
    }
}

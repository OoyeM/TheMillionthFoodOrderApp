using FastEndpoints;
using FluentValidation;
using TheMillionthFoodOrderApp.Application.MenuCategories;

namespace TheMillionthFoodOrderApp.Api.Endpoints.MenuCategories;

public sealed record UpdateMenuCategoryApiRequest(
    [property: RouteParam] string BrandSlug,
    [property: RouteParam] Guid Id,
    string? ImageUrl,
    int SortOrder,
    List<MenuCategoryTranslationInput> Translations);

public sealed class UpdateMenuCategoryRequestValidator : Validator<UpdateMenuCategoryApiRequest>
{
    public UpdateMenuCategoryRequestValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Menu category id is required.");

        RuleFor(x => x.SortOrder)
            .GreaterThanOrEqualTo(0).WithMessage("Sort order must be zero or greater.");

        RuleFor(x => x.Translations)
            .NotEmpty().WithMessage("At least one translation is required.");

        RuleForEach(x => x.Translations).ChildRules(t =>
        {
            t.RuleFor(x => x.LanguageCode)
                .NotEmpty().WithMessage("Language code is required.")
                .Must(lc => lc is "nl" or "fr" or "de")
                .WithMessage("Language code must be 'nl', 'fr', or 'de'.");

            t.RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Translation name is required.")
                .MaximumLength(200);

            t.RuleFor(x => x.Description)
                .MaximumLength(2000)
                .When(x => x.Description is not null);
        });

        RuleFor(x => x.Translations)
            .Must(ts => ts?.Select(t => t.LanguageCode).Distinct().Count() == ts?.Count)
            .When(x => x.Translations is not null)
            .WithMessage("Duplicate language codes are not allowed.");

        RuleFor(x => x.ImageUrl)
            .MaximumLength(2048)
            .When(x => x.ImageUrl is not null);
    }
}

public sealed class UpdateMenuCategoryEndpoint(IMenuCategoryService menuCategoryService)
    : Endpoint<UpdateMenuCategoryApiRequest, MenuCategoryResponse>
{
    public override void Configure()
    {
        Put("/api/brands/{brandSlug}/menu-categories/{id}");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Update a menu category";
            s.Description = "Brand Admin updates menu category details including translations.";
            s.Response<MenuCategoryResponse>(200, "Menu category updated successfully.");
            s.Response(400, "Validation error.");
            s.Response(404, "Menu category not found.");
        });
    }

    public override async Task HandleAsync(UpdateMenuCategoryApiRequest req, CancellationToken ct)
    {
        try
        {
            var appRequest = new UpdateMenuCategoryRequest(
                req.ImageUrl,
                req.SortOrder,
                req.Translations
                    .Select(t => new MenuCategoryTranslationRequest(t.LanguageCode, t.Name, t.Description))
                    .ToList().AsReadOnly());

            var response = await menuCategoryService.UpdateMenuCategoryAsync(req.Id, appRequest, ct);

            await HttpContext.Response.SendAsync(response, statusCode: 200, cancellation: ct);
        }
        catch (KeyNotFoundException)
        {
            await HttpContext.Response.SendNotFoundAsync(ct);
        }
    }
}

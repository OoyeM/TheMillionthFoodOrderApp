using FastEndpoints;
using FluentValidation;
using FluentValidation.Results;
using TheMillionthFoodOrderApp.Application.MenuCategories;

namespace TheMillionthFoodOrderApp.Api.Endpoints.MenuCategories;

public sealed record CreateMenuCategoryApiRequest(
    [property: RouteParam] string BrandSlug,
    string? ImageUrl,
    int SortOrder,
    List<MenuCategoryTranslationInput> Translations);

public sealed record MenuCategoryTranslationInput(string LanguageCode, string Name, string? Description);

public sealed class CreateMenuCategoryRequestValidator : Validator<CreateMenuCategoryApiRequest>
{
    public CreateMenuCategoryRequestValidator()
    {
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

public sealed class CreateMenuCategoryEndpoint(IMenuCategoryService menuCategoryService)
    : Endpoint<CreateMenuCategoryApiRequest, MenuCategoryResponse>
{
    public override void Configure()
    {
        Post("/api/brands/{brandSlug}/menu-categories");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Create a new menu category within a brand";
            s.Description = "Brand Admin creates a menu category with multilingual name/description and display order.";
            s.Response<MenuCategoryResponse>(201, "Menu category created successfully.");
            s.Response(400, "Validation error.");
        });
    }

    public override async Task HandleAsync(CreateMenuCategoryApiRequest req, CancellationToken ct)
    {
        try
        {
            var appRequest = new CreateMenuCategoryRequest(
                req.ImageUrl,
                req.SortOrder,
                req.Translations
                    .Select(t => new MenuCategoryTranslationRequest(t.LanguageCode, t.Name, t.Description))
                    .ToList().AsReadOnly());

            var response = await menuCategoryService.CreateMenuCategoryAsync(appRequest, ct);

            await HttpContext.Response.SendAsync(response, statusCode: 201, cancellation: ct);
        }
        catch (InvalidOperationException ex)
        {
            var failures = new List<ValidationFailure> { new("translations", ex.Message) };
            await HttpContext.Response.SendErrorsAsync(failures, statusCode: 400, cancellation: ct);
        }
    }
}

using FastEndpoints;
using FluentValidation;
using TheMillionthFoodOrderApp.Application.Products;

namespace TheMillionthFoodOrderApp.Api.Endpoints.Products;

public sealed record UpdateProductApiRequest(
    [property: RouteParam] string BrandSlug,
    [property: RouteParam] Guid Id,
    decimal BasePrice,
    string? ImageUrl,
    List<TranslationInput> Translations,
    List<int>? Allergens,
    List<int>? DietaryTags);

public sealed class UpdateProductRequestValidator : Validator<UpdateProductApiRequest>
{
    public UpdateProductRequestValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Product id is required.");

        RuleFor(x => x.BasePrice)
            .GreaterThan(0).WithMessage("Base price must be greater than zero.");

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

        RuleForEach(x => x.Allergens)
            .InclusiveBetween(0, 13)
            .WithMessage("Invalid allergen value. Must be between 0 and 13.")
            .When(x => x.Allergens is not null);

        RuleFor(x => x.Allergens)
            .Must(a => a!.Distinct().Count() == a!.Count)
            .When(x => x.Allergens is { Count: > 0 })
            .WithMessage("Duplicate allergen values are not allowed.");

        RuleForEach(x => x.DietaryTags)
            .InclusiveBetween(0, 3)
            .WithMessage("Invalid dietary tag value. Must be between 0 and 3.")
            .When(x => x.DietaryTags is not null);

        RuleFor(x => x.DietaryTags)
            .Must(d => d!.Distinct().Count() == d!.Count)
            .When(x => x.DietaryTags is { Count: > 0 })
            .WithMessage("Duplicate dietary tag values are not allowed.");
    }
}

public sealed class UpdateProductEndpoint(IProductService productService)
    : Endpoint<UpdateProductApiRequest, ProductResponse>
{
    public override void Configure()
    {
        Put("/api/brands/{brandSlug}/products/{id}");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Update a product";
            s.Description = "Brand Admin updates product details including translations.";
            s.Response<ProductResponse>(200, "Product updated successfully.");
            s.Response(400, "Validation error.");
            s.Response(404, "Product not found.");
        });
    }

    public override async Task HandleAsync(UpdateProductApiRequest req, CancellationToken ct)
    {
        try
        {
            var appRequest = new UpdateProductRequest(
                req.BasePrice,
                req.ImageUrl,
                req.Translations
                    .Select(t => new TranslationRequest(t.LanguageCode, t.Name, t.Description))
                    .ToList().AsReadOnly(),
                req.Allergens?.AsReadOnly(),
                req.DietaryTags?.AsReadOnly());

            var response = await productService.UpdateProductAsync(req.Id, appRequest, ct);

            await HttpContext.Response.SendAsync(response, statusCode: 200, cancellation: ct);
        }
        catch (KeyNotFoundException)
        {
            await HttpContext.Response.SendNotFoundAsync(ct);
        }
    }
}

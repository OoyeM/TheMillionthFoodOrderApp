using FastEndpoints;
using FluentValidation;
using FluentValidation.Results;
using TheMillionthFoodOrderApp.Application.Products;
using TheMillionthFoodOrderApp.Domain.Products;

namespace TheMillionthFoodOrderApp.Api.Endpoints.Products;

public sealed record CreateProductApiRequest(
    [property: RouteParam] string BrandSlug,
    decimal BasePrice,
    string? ImageUrl,
    List<TranslationInput> Translations,
    List<int>? Allergens,
    List<int>? DietaryTags);

public sealed record TranslationInput(string LanguageCode, string Name, string? Description);

public sealed class CreateProductRequestValidator : Validator<CreateProductApiRequest>
{
    public CreateProductRequestValidator()
    {
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
            .Must(v => Enum.IsDefined(typeof(Allergen), v))
            .WithMessage("Invalid allergen value.")
            .When(x => x.Allergens is not null);

        RuleFor(x => x.Allergens)
            .Must(a => a!.Distinct().Count() == a!.Count)
            .When(x => x.Allergens is { Count: > 0 })
            .WithMessage("Duplicate allergen values are not allowed.");

        RuleForEach(x => x.DietaryTags)
            .Must(v => Enum.IsDefined(typeof(DietaryTag), v))
            .WithMessage("Invalid dietary tag value.")
            .When(x => x.DietaryTags is not null);

        RuleFor(x => x.DietaryTags)
            .Must(d => d!.Distinct().Count() == d!.Count)
            .When(x => x.DietaryTags is { Count: > 0 })
            .WithMessage("Duplicate dietary tag values are not allowed.");
    }
}

public sealed class CreateProductEndpoint(IProductService productService)
    : Endpoint<CreateProductApiRequest, ProductResponse>
{
    public const string Route = "/api/brands/{brandSlug}/products";

    public override void Configure()
    {
        Post(Route);
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Create a new product within a brand";
            s.Description = "Brand Admin creates a simple product with multilingual name/description and base price.";
            s.Response<ProductResponse>(201, "Product created successfully.");
            s.Response(400, "Validation error.");
        });
    }

    public override async Task HandleAsync(CreateProductApiRequest req, CancellationToken ct)
    {
        try
        {
            var appRequest = new CreateProductRequest(
                req.BasePrice,
                req.ImageUrl,
                req.Translations
                    .Select(t => new TranslationRequest(t.LanguageCode, t.Name, t.Description))
                    .ToList().AsReadOnly(),
                req.Allergens?.AsReadOnly(),
                req.DietaryTags?.AsReadOnly());

            var response = await productService.CreateProductAsync(appRequest, ct);

            await HttpContext.Response.SendAsync(response, statusCode: 201, cancellation: ct);
        }
        catch (InvalidOperationException ex)
        {
            var failures = new List<ValidationFailure> { new("translations", ex.Message) };
            await HttpContext.Response.SendErrorsAsync(failures, statusCode: 400, cancellation: ct);
        }
    }
}

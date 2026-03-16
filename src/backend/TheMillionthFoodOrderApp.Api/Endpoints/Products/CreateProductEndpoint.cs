using FastEndpoints;
using FluentValidation;
using TheMillionthFoodOrderApp.Application.Products;

namespace TheMillionthFoodOrderApp.Api.Endpoints.Products;

public sealed record CreateProductApiRequest(
    [property: RouteParam] string BrandSlug,
    decimal BasePrice,
    string? ImageUrl,
    List<TranslationInput> Translations);

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
    }
}

public sealed class CreateProductEndpoint(IProductService productService)
    : Endpoint<CreateProductApiRequest, ProductResponse>
{
    public override void Configure()
    {
        Post("/api/brands/{brandSlug}/products");
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
        var appRequest = new CreateProductRequest(
            req.BasePrice,
            req.ImageUrl,
            req.Translations
                .Select(t => new TranslationRequest(t.LanguageCode, t.Name, t.Description))
                .ToList().AsReadOnly());

        var response = await productService.CreateProductAsync(appRequest, ct);

        await HttpContext.Response.SendAsync(response, statusCode: 201, cancellation: ct);
    }
}

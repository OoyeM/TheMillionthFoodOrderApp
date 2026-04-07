using FastEndpoints;
using FluentValidation;
using TheMillionthFoodOrderApp.Api.Endpoints.Products;
using TheMillionthFoodOrderApp.Application.Products;

namespace TheMillionthFoodOrderApp.Api.Endpoints.ComboProducts;

public sealed record CreateComboProductApiRequest(
    [property: RouteParam] string BrandSlug,
    decimal BasePrice,
    string? ImageUrl,
    List<TranslationInput> Translations,
    List<Guid> ComponentProductIds);

public sealed class CreateComboProductRequestValidator : Validator<CreateComboProductApiRequest>
{
    public CreateComboProductRequestValidator()
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

        RuleFor(x => x.ComponentProductIds)
            .NotEmpty().WithMessage("At least two component products are required.")
            .Must(ids => ids?.Count >= 2)
            .WithMessage("A combo product must contain at least two component products.")
            .Must(ids => ids?.Distinct().Count() == ids?.Count)
            .When(x => x.ComponentProductIds is not null)
            .WithMessage("Duplicate component product IDs are not allowed.");
    }
}

public sealed class CreateComboProductEndpoint(IProductService productService)
    : Endpoint<CreateComboProductApiRequest, ProductResponse>
{
    public override void Configure()
    {
        Post("/api/brands/{brandSlug}/combo-products");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Create a new combo product within a brand";
            s.Description = "Brand Admin creates a combo product bundling two or more simple products at a fixed price.";
            s.Response<ProductResponse>(201, "Combo product created successfully.");
            s.Response(400, "Validation error.");
        });
    }

    public override async Task HandleAsync(CreateComboProductApiRequest req, CancellationToken ct)
    {
        try
        {
            var appRequest = new CreateComboProductRequest(
                req.BasePrice,
                req.ImageUrl,
                req.Translations
                    .Select(t => new TranslationRequest(t.LanguageCode, t.Name, t.Description))
                    .ToList().AsReadOnly(),
                req.ComponentProductIds.AsReadOnly());

            var response = await productService.CreateComboProductAsync(appRequest, ct);

            await HttpContext.Response.SendAsync(response, statusCode: 201, cancellation: ct);
        }
        catch (KeyNotFoundException ex)
        {
            AddError(ex.Message);
            await SendErrorsAsync(400, ct);
        }
        catch (InvalidOperationException ex)
        {
            AddError(ex.Message);
            await SendErrorsAsync(400, ct);
        }
    }
}

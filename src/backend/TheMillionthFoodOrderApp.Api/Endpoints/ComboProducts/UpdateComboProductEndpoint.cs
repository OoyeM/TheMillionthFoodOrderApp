using FastEndpoints;
using FluentValidation;
using FluentValidation.Results;
using TheMillionthFoodOrderApp.Api.Endpoints.Products;
using TheMillionthFoodOrderApp.Application.Products;

namespace TheMillionthFoodOrderApp.Api.Endpoints.ComboProducts;

public sealed record UpdateComboProductApiRequest(
    [property: RouteParam] string BrandSlug,
    [property: RouteParam] Guid Id,
    decimal BasePrice,
    string? ImageUrl,
    List<TranslationInput> Translations,
    List<Guid> ComponentProductIds);

public sealed class UpdateComboProductRequestValidator : Validator<UpdateComboProductApiRequest>
{
    public UpdateComboProductRequestValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Combo product id is required.");

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

public sealed class UpdateComboProductEndpoint(IProductService productService)
    : Endpoint<UpdateComboProductApiRequest, ProductResponse>
{
    public override void Configure()
    {
        Put("/api/brands/{brandSlug}/combo-products/{id}");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Update a combo product";
            s.Description = "Brand Admin updates combo product details including translations and component products.";
            s.Response<ProductResponse>(200, "Combo product updated successfully.");
            s.Response(400, "Validation error.");
            s.Response(404, "Combo product not found.");
        });
    }

    public override async Task HandleAsync(UpdateComboProductApiRequest req, CancellationToken ct)
    {
        try
        {
            var appRequest = new UpdateComboProductRequest(
                req.BasePrice,
                req.ImageUrl,
                req.Translations
                    .Select(t => new TranslationRequest(t.LanguageCode, t.Name, t.Description))
                    .ToList().AsReadOnly(),
                req.ComponentProductIds.AsReadOnly());

            var response = await productService.UpdateComboProductAsync(req.Id, appRequest, ct);

            await HttpContext.Response.SendAsync(response, statusCode: 200, cancellation: ct);
        }
        catch (KeyNotFoundException)
        {
            await HttpContext.Response.SendNotFoundAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            var failures = new List<ValidationFailure> { new("ComponentProductIds", ex.Message) };
            await HttpContext.Response.SendErrorsAsync(failures, statusCode: 400, cancellation: ct);
        }
    }
}

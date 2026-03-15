using FastEndpoints;
using FluentValidation;
using FluentValidation.Results;
using TheMillionthFoodOrderApp.Application.Shops;

namespace TheMillionthFoodOrderApp.Api.Endpoints.Shops;

public sealed record CreateShopRequest(
    [property: RouteParam] string BrandSlug,
    string Name,
    string Slug,
    AddressRequest Address,
    string ContactEmail,
    string? ContactPhone);

public sealed record AddressRequest(
    string Street,
    string Number,
    string City,
    string PostalCode,
    string Country = "BE");

public sealed class CreateShopRequestValidator : Validator<CreateShopRequest>
{
    public CreateShopRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200);

        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Slug is required.")
            .MaximumLength(100)
            .Matches(@"^[a-z0-9]+(?:-[a-z0-9]+)*$")
            .WithMessage("Slug must be URL-safe: lowercase letters, digits, and hyphens only (no leading/trailing hyphens).");

        RuleFor(x => x.Address).NotNull().WithMessage("Address is required.");

        RuleFor(x => x.Address.Street)
            .NotEmpty().WithMessage("Address street is required.")
            .MaximumLength(200);

        RuleFor(x => x.Address.Number)
            .NotEmpty().WithMessage("Address number is required.")
            .MaximumLength(20);

        RuleFor(x => x.Address.City)
            .NotEmpty().WithMessage("Address city is required.")
            .MaximumLength(100);

        RuleFor(x => x.Address.PostalCode)
            .NotEmpty().WithMessage("Address postal code is required.")
            .MaximumLength(20);

        RuleFor(x => x.Address.Country)
            .NotEmpty().WithMessage("Address country is required.")
            .Length(2).WithMessage("Country must be an ISO 3166-1 alpha-2 code (e.g. 'BE').");

        RuleFor(x => x.ContactEmail)
            .NotEmpty().WithMessage("Contact email is required.")
            .EmailAddress().WithMessage("Contact email must be a valid email address.")
            .MaximumLength(320);

        RuleFor(x => x.ContactPhone)
            .MaximumLength(30)
            .When(x => x.ContactPhone is not null);
    }
}

public sealed class CreateShopEndpoint(IShopService shopService)
    : Endpoint<CreateShopRequest, ShopResponse>
{
    public override void Configure()
    {
        Post("/api/brands/{brandSlug}/shops");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Create a new shop within a brand";
            s.Description = "Brand Admin creates a shop. The shop slug is unique within the brand.";
            s.Response<ShopResponse>(201, "Shop created successfully.");
            s.Response(400, "Validation error.");
            s.Response(409, "A shop with the same slug already exists in this brand.");
        });
    }

    public override async Task HandleAsync(CreateShopRequest req, CancellationToken ct)
    {
        try
        {
            var appRequest = new Application.Shops.CreateShopRequest(
                req.Name,
                req.Slug,
                new Application.Shops.AddressRequest(
                    req.Address.Street,
                    req.Address.Number,
                    req.Address.City,
                    req.Address.PostalCode,
                    req.Address.Country),
                req.ContactEmail,
                req.ContactPhone);

            var response = await shopService.CreateShopAsync(appRequest, ct);

            await HttpContext.Response.SendAsync(response, statusCode: 201, cancellation: ct);
        }
        catch (InvalidOperationException ex)
        {
            var failures = new List<ValidationFailure>
            {
                new("slug", ex.Message)
            };
            await HttpContext.Response.SendErrorsAsync(failures, statusCode: 409, cancellation: ct);
        }
    }
}

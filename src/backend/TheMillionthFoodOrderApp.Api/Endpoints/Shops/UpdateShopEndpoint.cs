using FastEndpoints;
using FluentValidation;
using TheMillionthFoodOrderApp.Application.Shops;

namespace TheMillionthFoodOrderApp.Api.Endpoints.Shops;

public sealed record UpdateShopRequest(
    [property: RouteParam] string BrandSlug,
    [property: RouteParam] Guid Id,
    string Name,
    AddressRequest Address,
    string ContactEmail,
    string? ContactPhone,
    bool TicketPrinterEnabled);

public sealed class UpdateShopRequestValidator : Validator<UpdateShopRequest>
{
    public UpdateShopRequestValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Shop id is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200);

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

public sealed class UpdateShopEndpoint(IShopService shopService)
    : Endpoint<UpdateShopRequest, ShopResponse>
{
    public const string Route = "/api/brands/{brandSlug}/shops/{id}";

    public override void Configure()
    {
        Put(Route);
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Update shop metadata";
            s.Description = "Brand Admin updates a shop's name, address, and contact information. Slug is immutable.";
            s.Response<ShopResponse>(200, "Shop updated successfully.");
            s.Response(400, "Validation error.");
            s.Response(404, "Shop not found.");
        });
    }

    public override async Task HandleAsync(UpdateShopRequest req, CancellationToken ct)
    {
        try
        {
            var appRequest = new Application.Shops.UpdateShopRequest(
                req.Name,
                new Application.Shops.AddressRequest(
                    req.Address.Street,
                    req.Address.Number,
                    req.Address.City,
                    req.Address.PostalCode,
                    req.Address.Country),
                req.ContactEmail,
                req.ContactPhone,
                req.TicketPrinterEnabled);

            var response = await shopService.UpdateShopAsync(req.Id, appRequest, ct);

            await HttpContext.Response.SendAsync(response, statusCode: 200, cancellation: ct);
        }
        catch (KeyNotFoundException)
        {
            await HttpContext.Response.SendNotFoundAsync(ct);
        }
    }
}

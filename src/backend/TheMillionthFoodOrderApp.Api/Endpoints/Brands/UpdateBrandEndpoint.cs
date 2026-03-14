using FastEndpoints;
using FluentValidation;
using TheMillionthFoodOrderApp.Application.Brands;

namespace TheMillionthFoodOrderApp.Api.Endpoints.Brands;

public sealed record UpdateBrandRequest(
    [property: RouteParam] Guid Id,
    string Name,
    string ContactEmail,
    string? ContactPhone);

public sealed class UpdateBrandRequestValidator : Validator<UpdateBrandRequest>
{
    public UpdateBrandRequestValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Brand id is required.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required.")
            .MaximumLength(200);

        RuleFor(x => x.ContactEmail)
            .NotEmpty().WithMessage("Contact email is required.")
            .EmailAddress().WithMessage("Contact email must be a valid email address.")
            .MaximumLength(320);

        RuleFor(x => x.ContactPhone)
            .MaximumLength(30)
            .When(x => x.ContactPhone is not null);
    }
}

public sealed class UpdateBrandEndpoint(IBrandService brandService)
    : Endpoint<UpdateBrandRequest, BrandResponse>
{
    public override void Configure()
    {
        Put("/api/brands/{id}");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Update brand metadata";
            s.Description = "Platform Admin updates a brand's name and contact information. Slug is immutable.";
            s.Response<BrandResponse>(200, "Brand updated successfully.");
            s.Response(400, "Validation error.");
            s.Response(404, "Brand not found.");
        });
    }

    public override async Task HandleAsync(UpdateBrandRequest req, CancellationToken ct)
    {
        try
        {
            var appRequest = new Application.Brands.UpdateBrandRequest(
                req.Name,
                req.ContactEmail,
                req.ContactPhone);

            var response = await brandService.UpdateBrandAsync(req.Id, appRequest, ct);

            await HttpContext.Response.SendAsync(response, statusCode: 200, cancellation: ct);
        }
        catch (KeyNotFoundException)
        {
            await HttpContext.Response.SendNotFoundAsync(ct);
        }
    }
}

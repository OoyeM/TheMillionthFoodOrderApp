using FastEndpoints;
using FluentValidation;
using TheMillionthFoodOrderApp.Application.Brands;
using TheMillionthFoodOrderApp.Domain.Brands;

namespace TheMillionthFoodOrderApp.Api.Endpoints.Brands;

public sealed record ConfigureStaffAuthRequest(
    [property: RouteParam] string Slug,
    StaffAuthMethod Method);

public sealed class ConfigureStaffAuthRequestValidator : Validator<ConfigureStaffAuthRequest>
{
    public ConfigureStaffAuthRequestValidator()
    {
        RuleFor(x => x.Slug)
            .NotEmpty().WithMessage("Slug is required.");

        RuleFor(x => x.Method)
            .IsInEnum().WithMessage("Method must be a valid StaffAuthMethod value (0 = EmailPassword, 1 = GoogleSso, 2 = MicrosoftSso).");
    }
}

public sealed class ConfigureStaffAuthEndpoint(IBrandService brandService)
    : Endpoint<ConfigureStaffAuthRequest, BrandResponse>
{
    public const string Route = "/api/brands/{slug}/staff-auth";

    public override void Configure()
    {
        Put(Route);
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Configure staff authentication method";
            s.Description = "Brand Admin configures the authentication method for staff members on this brand's management portal.";
            s.Response<BrandResponse>(200, "Staff auth method updated successfully.");
            s.Response(400, "Validation error.");
            s.Response(404, "Brand not found.");
        });
    }

    public override async Task HandleAsync(ConfigureStaffAuthRequest req, CancellationToken ct)
    {
        try
        {
            var response = await brandService.ConfigureStaffAuthAsync(req.Slug, req.Method, ct);
            await HttpContext.Response.SendAsync(response, statusCode: 200, cancellation: ct);
        }
        catch (KeyNotFoundException)
        {
            await HttpContext.Response.SendNotFoundAsync(ct);
        }
    }
}

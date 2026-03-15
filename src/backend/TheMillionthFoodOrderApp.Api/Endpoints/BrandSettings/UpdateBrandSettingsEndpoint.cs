using FastEndpoints;
using FluentValidation;
using TheMillionthFoodOrderApp.Application.BrandSettings;

namespace TheMillionthFoodOrderApp.Api.Endpoints.BrandSettings;

public sealed record UpdateBrandSettingsRequest(
    [property: RouteParam] string BrandSlug,
    string DefaultLanguage,
    string Timezone,
    string Currency);

public sealed class UpdateBrandSettingsRequestValidator : Validator<UpdateBrandSettingsRequest>
{
    public UpdateBrandSettingsRequestValidator()
    {
        RuleFor(x => x.DefaultLanguage)
            .NotEmpty().WithMessage("DefaultLanguage is required.")
            .MaximumLength(20).WithMessage("DefaultLanguage must be a valid BCP-47 language tag (max 20 characters).");

        RuleFor(x => x.Timezone)
            .NotEmpty().WithMessage("Timezone is required.")
            .MaximumLength(100).WithMessage("Timezone must be a valid IANA timezone identifier (max 100 characters).");

        RuleFor(x => x.Currency)
            .NotEmpty().WithMessage("Currency is required.")
            .Length(3).WithMessage("Currency must be a valid ISO 4217 code (exactly 3 characters).");
    }
}

public sealed class UpdateBrandSettingsEndpoint(IBrandSettingsService brandSettingsService)
    : Endpoint<UpdateBrandSettingsRequest, BrandSettingsResponse>
{
    public override void Configure()
    {
        Put("/api/brands/{brandSlug}/settings");
        AllowAnonymous();
        PreProcessor<BrandScopedPreProcessor<UpdateBrandSettingsRequest>>();
        Summary(s =>
        {
            s.Summary = "Update brand settings";
            s.Description = "Creates or updates the configuration settings for the specified brand.";
            s.Response<BrandSettingsResponse>(200, "Brand settings updated successfully.");
            s.Response(400, "Validation error.");
            s.Response(404, "Brand not found.");
        });
    }

    public override async Task HandleAsync(UpdateBrandSettingsRequest req, CancellationToken ct)
    {
        var appRequest = new Application.BrandSettings.UpdateBrandSettingsRequest(
            req.DefaultLanguage,
            req.Timezone,
            req.Currency);

        var response = await brandSettingsService.UpsertAsync(appRequest, ct);

        await HttpContext.Response.SendAsync(response, statusCode: 200, cancellation: ct);
    }
}

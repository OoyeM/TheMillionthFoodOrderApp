using FastEndpoints;
using FluentValidation;
using TheMillionthFoodOrderApp.Application.BrandSettings;
using TheMillionthFoodOrderApp.Domain.BrandSettings;

namespace TheMillionthFoodOrderApp.Api.Endpoints.BrandSettings;

public sealed record UpdateBrandThemingRequest(
    [property: RouteParam] string BrandSlug,
    BrandColorsDto? Colors,
    BrandTypographyDto? Typography,
    string? CustomDomain);

public sealed class UpdateBrandThemingRequestValidator : Validator<UpdateBrandThemingRequest>
{
    public UpdateBrandThemingRequestValidator()
    {
        // Colors validation — only required when the Colors object is provided
        When(x => x.Colors is not null, () =>
        {
            RuleFor(x => x.Colors!.Primary)
                .NotEmpty().WithMessage("Primary color is required.")
                .Matches(@"^#([0-9a-fA-F]{3}|[0-9a-fA-F]{6})$")
                .WithMessage("Primary must be a valid CSS hex color (e.g. #fff or #2563eb).");

            RuleFor(x => x.Colors!.Secondary)
                .NotEmpty().WithMessage("Secondary color is required.")
                .Matches(@"^#([0-9a-fA-F]{3}|[0-9a-fA-F]{6})$")
                .WithMessage("Secondary must be a valid CSS hex color (e.g. #fff or #6b7280).");

            RuleFor(x => x.Colors!.Accent)
                .NotEmpty().WithMessage("Accent color is required.")
                .Matches(@"^#([0-9a-fA-F]{3}|[0-9a-fA-F]{6})$")
                .WithMessage("Accent must be a valid CSS hex color (e.g. #fff or #2563eb).");
        });

        // Typography validation — only required when the Typography object is provided
        When(x => x.Typography is not null, () =>
        {
            RuleFor(x => x.Typography!.HeadingFontFamily)
                .NotEmpty().WithMessage("Heading font family is required.")
                .Must(f => PresetFonts.IsValid(f))
                .WithMessage($"Heading font must be one of: {string.Join(", ", PresetFonts.All)}.");

            RuleFor(x => x.Typography!.BodyFontFamily)
                .NotEmpty().WithMessage("Body font family is required.")
                .Must(f => PresetFonts.IsValid(f))
                .WithMessage($"Body font must be one of: {string.Join(", ", PresetFonts.All)}.");
        });

        // Custom domain is optional but must be a reasonable hostname if provided
        When(x => x.CustomDomain is not null, () =>
        {
            RuleFor(x => x.CustomDomain!)
                .MaximumLength(253).WithMessage("Custom domain must not exceed 253 characters.")
                .Matches(@"^[a-zA-Z0-9]([a-zA-Z0-9\-\.]{0,251}[a-zA-Z0-9])?$")
                .WithMessage("Custom domain must be a valid DNS hostname (e.g. order.frietjes.be).");
        });
    }
}

public sealed class UpdateBrandThemingEndpoint(IBrandSettingsService brandSettingsService)
    : Endpoint<UpdateBrandThemingRequest, BrandSettingsResponse>
{
    public const string Route = "/api/brands/{brandSlug}/settings/theming";

    public override void Configure()
    {
        Put(Route);
        AllowAnonymous();
        PreProcessor<BrandScopedPreProcessor<UpdateBrandThemingRequest>>();
        Summary(s =>
        {
            s.Summary = "Update brand theming";
            s.Description = "Updates the visual theming configuration for the specified brand (colors, typography, custom domain).";
            s.Response<BrandSettingsResponse>(200, "Brand theming updated successfully.");
            s.Response(400, "Validation error.");
            s.Response(404, "Brand not found or settings not yet provisioned.");
        });
    }

    public override async Task HandleAsync(UpdateBrandThemingRequest req, CancellationToken ct)
    {
        var appRequest = new Application.BrandSettings.UpdateBrandThemingRequest(
            req.Colors,
            req.Typography,
            req.CustomDomain);

        var response = await brandSettingsService.UpdateThemingAsync(appRequest, ct);

        if (response is null)
        {
            await HttpContext.Response.SendNotFoundAsync(ct);
            return;
        }

        await HttpContext.Response.SendAsync(response, statusCode: 200, cancellation: ct);
    }
}

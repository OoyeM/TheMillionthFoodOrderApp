using FastEndpoints;
using TheMillionthFoodOrderApp.Application.BrandSettings;

namespace TheMillionthFoodOrderApp.Api.Endpoints.BrandSettings;

public sealed record GetBrandThemeRequest([property: RouteParam] string BrandSlug);

/// <summary>
/// Public endpoint that returns the minimal theme data needed by the storefront.
/// No authentication is required — the storefront fetches this on every load.
/// </summary>
public sealed class GetBrandThemeEndpoint(IBrandSettingsService brandSettingsService)
    : Endpoint<GetBrandThemeRequest, BrandThemeResponse>
{
    public const string Route = "/api/brands/{brandSlug}/theme";

    public override void Configure()
    {
        Get(Route);
        AllowAnonymous();
        PreProcessor<BrandScopedPreProcessor<GetBrandThemeRequest>>();
        Summary(s =>
        {
            s.Summary = "Get brand theme";
            s.Description = "Returns the public theme configuration for the specified brand. " +
                            "Used by the storefront to apply CSS custom properties at runtime.";
            s.Response<BrandThemeResponse>(200, "Brand theme data.");
            s.Response(404, "Brand not found or settings not yet provisioned.");
        });
    }

    public override async Task HandleAsync(GetBrandThemeRequest req, CancellationToken ct)
    {
        var response = await brandSettingsService.GetThemeAsync(ct);

        if (response is null)
        {
            await HttpContext.Response.SendNotFoundAsync(ct);
            return;
        }

        await HttpContext.Response.SendAsync(response, statusCode: 200, cancellation: ct);
    }
}

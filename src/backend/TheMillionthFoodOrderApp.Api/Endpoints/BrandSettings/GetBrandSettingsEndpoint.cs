using FastEndpoints;
using TheMillionthFoodOrderApp.Application.BrandSettings;

namespace TheMillionthFoodOrderApp.Api.Endpoints.BrandSettings;

public sealed record GetBrandSettingsRequest([property: RouteParam] string BrandSlug);

public sealed class GetBrandSettingsEndpoint(IBrandSettingsService brandSettingsService)
    : Endpoint<GetBrandSettingsRequest, BrandSettingsResponse>
{
    public override void Configure()
    {
        Get("/api/brands/{brandSlug}/settings");
        AllowAnonymous();
        PreProcessor<BrandScopedPreProcessor<GetBrandSettingsRequest>>();
        Summary(s =>
        {
            s.Summary = "Get brand settings";
            s.Description = "Returns the configuration settings for the specified brand.";
            s.Response<BrandSettingsResponse>(200, "Brand settings found.");
            s.Response(404, "Brand not found or settings not yet provisioned.");
        });
    }

    public override async Task HandleAsync(GetBrandSettingsRequest req, CancellationToken ct)
    {
        var response = await brandSettingsService.GetAsync(ct);

        if (response is null)
        {
            await HttpContext.Response.SendNotFoundAsync(ct);
            return;
        }

        await HttpContext.Response.SendAsync(response, statusCode: 200, cancellation: ct);
    }
}

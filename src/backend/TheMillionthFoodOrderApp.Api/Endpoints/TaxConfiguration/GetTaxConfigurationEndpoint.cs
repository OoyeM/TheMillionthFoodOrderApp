using FastEndpoints;
using TheMillionthFoodOrderApp.Application.TaxConfiguration;

namespace TheMillionthFoodOrderApp.Api.Endpoints.TaxConfiguration;

public sealed record GetTaxConfigurationRequest([property: RouteParam] string BrandSlug);

public sealed class GetTaxConfigurationEndpoint(ITaxConfigurationService service)
    : Endpoint<GetTaxConfigurationRequest, TaxConfigurationResponse>
{
    public override void Configure()
    {
        Get("/api/brands/{brandSlug}/tax-configuration");
        // TODO: Require BrandAdmin role when auth is implemented (US-FP-046)
        AllowAnonymous();
        PreProcessor<BrandScopedPreProcessor<GetTaxConfigurationRequest>>();
        Summary(s =>
        {
            s.Summary = "Get tax configuration for a brand";
            s.Description = "Returns the VAT rate configuration for the specified brand.";
            s.Response<TaxConfigurationResponse>(200, "Tax configuration retrieved successfully.");
            s.Response(404, "No tax configuration found for this brand.");
        });
    }

    public override async Task HandleAsync(GetTaxConfigurationRequest req, CancellationToken ct)
    {
        var response = await service.GetAsync(ct);

        if (response is null)
        {
            await HttpContext.Response.SendNotFoundAsync(ct);
            return;
        }

        await HttpContext.Response.SendAsync(response, statusCode: 200, cancellation: ct);
    }
}

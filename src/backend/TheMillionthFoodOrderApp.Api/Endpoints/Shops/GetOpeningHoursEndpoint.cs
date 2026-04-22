using FastEndpoints;
using TheMillionthFoodOrderApp.Application.Shops;

namespace TheMillionthFoodOrderApp.Api.Endpoints.Shops;

public sealed record GetOpeningHoursRequest(
    [property: RouteParam] string BrandSlug,
    [property: RouteParam] Guid Id);

public sealed class GetOpeningHoursEndpoint(IOpeningHoursService openingHoursService)
    : Endpoint<GetOpeningHoursRequest, OpeningHoursResponse>
{
    public const string Route = "/api/brands/{brandSlug}/shops/{id}/opening-hours";

    public override void Configure()
    {
        Get(Route);
        AllowAnonymous();
        PreProcessor<BrandScopedPreProcessor<GetOpeningHoursRequest>>();
        Summary(s =>
        {
            s.Summary = "Get opening hours for a shop";
            s.Description = "Returns the complete weekly opening hours schedule for the given shop.";
            s.Response<OpeningHoursResponse>(200, "Opening hours retrieved successfully.");
            s.Response(404, "Shop not found.");
        });
    }

    public override async Task HandleAsync(GetOpeningHoursRequest req, CancellationToken ct)
    {
        try
        {
            var response = await openingHoursService.GetOpeningHoursAsync(req.Id, ct);
            await HttpContext.Response.SendAsync(response, statusCode: 200, cancellation: ct);
        }
        catch (KeyNotFoundException)
        {
            await HttpContext.Response.SendNotFoundAsync(ct);
        }
    }
}

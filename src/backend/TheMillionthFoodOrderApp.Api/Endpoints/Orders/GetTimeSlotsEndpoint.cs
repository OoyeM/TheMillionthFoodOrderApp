using FastEndpoints;
using TheMillionthFoodOrderApp.Application.Orders;

namespace TheMillionthFoodOrderApp.Api.Endpoints.Orders;

public sealed record GetTimeSlotsRequest(
    [property: RouteParam] string BrandSlug,
    [property: RouteParam] Guid ShopId);

/// <summary>
/// Returns the time-slot availability for a shop (US-FP-019).
/// Always anonymous — the storefront calls this before or during checkout.
/// </summary>
public sealed class GetTimeSlotsEndpoint(ITimeSlotService timeSlotService)
    : Endpoint<GetTimeSlotsRequest, TimeSlotAvailabilityResponse>
{
    public const string Route = "/api/brands/{brandSlug}/shops/{shopId}/time-slots";

    public override void Configure()
    {
        Get(Route);
        AllowAnonymous();
        PreProcessor<BrandScopedPreProcessor<GetTimeSlotsRequest>>();
        Summary(s =>
        {
            s.Summary = "Get time-slot availability for a shop";
            s.Description =
                "Returns whether time-slot ordering is enabled for the shop, and the list of " +
                "available slots for the remainder of today with their capacity status. " +
                "When disabled, returns the active order count for place-in-line display (AC5). " +
                "Safe to poll at 60-second intervals — the response is lightweight.";
            s.Response<TimeSlotAvailabilityResponse>(200, "Availability retrieved successfully.");
            s.Response(404, "Shop or brand not found.");
        });
    }

    public override async Task HandleAsync(GetTimeSlotsRequest req, CancellationToken ct)
    {
        try
        {
            var response = await timeSlotService.GetAvailabilityAsync(req.ShopId, ct);
            await HttpContext.Response.SendAsync(response, statusCode: 200, cancellation: ct);
        }
        catch (KeyNotFoundException)
        {
            await HttpContext.Response.SendNotFoundAsync(ct);
        }
    }
}

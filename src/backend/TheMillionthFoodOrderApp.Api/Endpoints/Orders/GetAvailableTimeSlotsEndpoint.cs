using FastEndpoints;
using FluentValidation.Results;
using TheMillionthFoodOrderApp.Application.Orders;

namespace TheMillionthFoodOrderApp.Api.Endpoints.Orders;

public sealed record GetAvailableTimeSlotsRequest(
    [property: RouteParam] string BrandSlug,
    [property: RouteParam] Guid ShopId);

/// <summary>
/// Returns the available time slots for a shop's checkout page (US-FP-019).
/// When time-slot ordering is disabled, the response contains <c>isEnabled: false</c>
/// and an empty <c>slots</c> array.
/// </summary>
public sealed class GetAvailableTimeSlotsEndpoint(ITimeSlotAvailabilityService availabilityService)
    : Endpoint<GetAvailableTimeSlotsRequest, AvailableTimeSlotsResponse>
{
    public const string Route = "/api/brands/{brandSlug}/shops/{shopId}/time-slots";

    public override void Configure()
    {
        Get(Route);
        AllowAnonymous();
        PreProcessor<BrandScopedPreProcessor<GetAvailableTimeSlotsRequest>>();
        Summary(s =>
        {
            s.Summary = "Get available time slots for a shop";
            s.Description =
                "Returns time slots for the remainder of today (shop-local time) based on the " +
                "shop's configured interval and capacity. Full slots are returned with " +
                "isAvailable: false and remainingCapacity: 0. " +
                "When time-slot ordering is disabled, returns { isEnabled: false, slots: [] }. " +
                "Clients should poll this endpoint every ~30 seconds so they reflect real-time capacity.";
            s.Response<AvailableTimeSlotsResponse>(200, "Available time slots (may be an empty list near closing).");
            s.Response(404, "Shop or brand not found.");
        });
    }

    public override async Task HandleAsync(GetAvailableTimeSlotsRequest req, CancellationToken ct)
    {
        try
        {
            var response = await availabilityService.GetAvailableSlotsAsync(req.ShopId, ct);
            await HttpContext.Response.SendAsync(response, statusCode: 200, cancellation: ct);
        }
        catch (KeyNotFoundException ex)
        {
            var failures = new List<ValidationFailure> { new("shopId", ex.Message) };
            await HttpContext.Response.SendErrorsAsync(failures, statusCode: 404, cancellation: ct);
        }
    }
}

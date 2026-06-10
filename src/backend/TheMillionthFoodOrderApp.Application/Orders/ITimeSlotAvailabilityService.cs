namespace TheMillionthFoodOrderApp.Application.Orders;

/// <summary>
/// Computes the available time slots for a shop's checkout page (US-FP-019).
/// </summary>
public interface ITimeSlotAvailabilityService
{
    /// <summary>
    /// Returns the available time slots for the given shop for the rest of today.
    /// </summary>
    /// <param name="shopId">The shop whose slots are computed.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// An <see cref="AvailableTimeSlotsResponse"/> whose <c>IsEnabled</c> flag reflects
    /// whether time-slot ordering is active. When disabled, <c>Slots</c> is empty.
    /// </returns>
    /// <exception cref="KeyNotFoundException">Thrown when the shop is not found.</exception>
    Task<AvailableTimeSlotsResponse> GetAvailableSlotsAsync(Guid shopId, CancellationToken cancellationToken = default);
}

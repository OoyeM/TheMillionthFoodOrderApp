using TheMillionthFoodOrderApp.Domain.Orders;
using TheMillionthFoodOrderApp.Domain.Shops;

namespace TheMillionthFoodOrderApp.Application.Orders;

/// <summary>
/// Computes the available time slots for a shop by combining the domain slot
/// generator with live order-count data from the repository. US-FP-019.
/// </summary>
public sealed class TimeSlotAvailabilityService(
    IShopRepository shopRepository,
    IOrderRepository orderRepository) : ITimeSlotAvailabilityService
{
    /// <inheritdoc/>
    public async Task<AvailableTimeSlotsResponse> GetAvailableSlotsAsync(
        Guid shopId,
        CancellationToken cancellationToken = default)
    {
        // 1. Load the shop; 404 if missing.
        var shop = await shopRepository.GetByIdAsync(shopId, cancellationToken)
            ?? throw new KeyNotFoundException($"Shop with id '{shopId}' was not found.");

        // 2. When time-slot ordering is disabled, return a disabled response immediately.
        if (!shop.TimeSlotOrdering.IsEnabled)
        {
            return new AvailableTimeSlotsResponse(
                IsEnabled: false,
                IntervalMinutes: null,
                MaxOrdersPerInterval: null,
                Slots: []);
        }

        // 3. Generate slots for the rest of today (shop-local time).
        var now = DateTimeOffset.UtcNow;
        var slots = TimeSlotGenerator.GenerateSlotsForToday(shop, now);

        if (slots.Count == 0)
        {
            return new AvailableTimeSlotsResponse(
                IsEnabled: true,
                IntervalMinutes: (int?)shop.TimeSlotOrdering.Interval,
                MaxOrdersPerInterval: shop.TimeSlotOrdering.MaxOrdersPerInterval,
                Slots: []);
        }

        // 4. One DB call to count existing orders per slot across the full range.
        var fromInclusive = slots[0].Start;
        var toExclusive = slots[^1].End;

        var orderCounts = await orderRepository.GetTimeSlotOrderCountsAsync(
            shopId, fromInclusive, toExclusive, cancellationToken);

        var maxPerSlot = shop.TimeSlotOrdering.MaxOrdersPerInterval!.Value;
        var intervalMinutes = (int)shop.TimeSlotOrdering.Interval!.Value;

        // 5. Map each slot to its DTO with remaining capacity.
        var slotDtos = slots
            .Select(s =>
            {
                var count = orderCounts.TryGetValue(s.Start, out var c) ? c : 0;
                var remaining = maxPerSlot - count;
                return new TimeSlotDto(s.Start, s.End, remaining > 0, Math.Max(0, remaining));
            })
            .ToList()
            .AsReadOnly();

        return new AvailableTimeSlotsResponse(
            IsEnabled: true,
            IntervalMinutes: intervalMinutes,
            MaxOrdersPerInterval: maxPerSlot,
            Slots: slotDtos);
    }
}

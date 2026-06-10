using TheMillionthFoodOrderApp.Domain.Orders;
using TheMillionthFoodOrderApp.Domain.Shops;

namespace TheMillionthFoodOrderApp.Application.Orders;

/// <summary>
/// Represents a single time slot in the availability response.
/// </summary>
/// <param name="SlotStart">UTC start of the slot.</param>
/// <param name="Label">Shop-local label formatted as <c>"HH:mm"</c>.</param>
/// <param name="IsAvailable"><see langword="true"/> when the slot has remaining capacity.</param>
public sealed record TimeSlotDto(DateTimeOffset SlotStart, string Label, bool IsAvailable);

/// <summary>
/// Response DTO for the GET time-slots availability endpoint (US-FP-019).
/// </summary>
/// <param name="IsEnabled">
/// Reflects time-slot ordering configuration only — never the open/closed state (design decision 3).
/// </param>
/// <param name="IntervalMinutes">Slot length in minutes; null when disabled.</param>
/// <param name="Slots">
/// Available slots for today. Empty when disabled, when the shop is currently closed,
/// or when no opening hours are configured (see design decision 3).
/// </param>
/// <param name="ActiveOrderCount">
/// Active (non-terminal) order count for place-in-line display (AC5).
/// Only populated when <see cref="IsEnabled"/> is <see langword="false"/>; null otherwise.
/// </param>
public sealed record TimeSlotAvailabilityResponse(
    bool IsEnabled,
    int? IntervalMinutes,
    IReadOnlyList<TimeSlotDto> Slots,
    int? ActiveOrderCount);

/// <summary>
/// Application service for time-slot availability and gating (US-FP-019).
/// </summary>
public interface ITimeSlotService
{
    /// <summary>
    /// Returns the time-slot availability for the given shop.
    /// </summary>
    /// <exception cref="KeyNotFoundException">Thrown when the shop does not exist.</exception>
    Task<TimeSlotAvailabilityResponse> GetAvailabilityAsync(Guid shopId, CancellationToken ct);
}

/// <summary>
/// Implementation of <see cref="ITimeSlotService"/>.
/// </summary>
public sealed class TimeSlotService(
    IShopRepository shopRepository,
    IOrderRepository orderRepository) : ITimeSlotService
{
    /// <inheritdoc/>
    public async Task<TimeSlotAvailabilityResponse> GetAvailabilityAsync(Guid shopId, CancellationToken ct)
    {
        var shop = await shopRepository.GetByIdAsync(shopId, ct);
        if (shop is null)
            throw new KeyNotFoundException($"Shop with id '{shopId}' was not found.");

        var settings = shop.TimeSlotOrdering;

        // Slots disabled — return place-in-line count for AC5 notice. A null interval/max on an
        // "enabled" row is treated as disabled defensively (possible on rows written before the
        // owned-bool default-value fix in ShopConfiguration).
        if (!settings.IsEnabled || settings.Interval is null || settings.MaxOrdersPerInterval is null)
        {
            var activeCount = await orderRepository.CountActiveByShopAsync(shopId, ct);
            return new TimeSlotAvailabilityResponse(
                IsEnabled: false,
                IntervalMinutes: null,
                Slots: [],
                ActiveOrderCount: activeCount);
        }

        // Slots enabled but shop is currently closed — return empty slot list (design decision 3).
        var nowUtc = DateTimeOffset.UtcNow;
        if (!shop.IsOpenAt(nowUtc))
        {
            return new TimeSlotAvailabilityResponse(
                IsEnabled: true,
                IntervalMinutes: (int)settings.Interval!.Value,
                Slots: [],
                ActiveOrderCount: null);
        }

        // Generate candidates for today.
        var candidates = TimeSlotCalculator.GenerateSlots(
            shop.OpeningHours,
            shop.TimeZoneId,
            settings.Interval!.Value,
            nowUtc);

        if (candidates.Count == 0)
        {
            return new TimeSlotAvailabilityResponse(
                IsEnabled: true,
                IntervalMinutes: (int)settings.Interval.Value,
                Slots: [],
                ActiveOrderCount: null);
        }

        // Fetch slot counts for the entire range in one query.
        var fromUtc = candidates[0].SlotStartUtc;
        var toUtc = candidates[^1].SlotStartUtc;
        var slotCounts = await orderRepository.GetTimeSlotCountsAsync(shopId, fromUtc, toUtc, ct);

        var max = settings.MaxOrdersPerInterval!.Value;
        var slots = candidates
            .Select(c =>
            {
                slotCounts.TryGetValue(c.SlotStartUtc, out var count);
                return new TimeSlotDto(c.SlotStartUtc, c.LocalLabel, IsAvailable: count < max);
            })
            .ToList()
            .AsReadOnly();

        return new TimeSlotAvailabilityResponse(
            IsEnabled: true,
            IntervalMinutes: (int)settings.Interval.Value,
            Slots: slots,
            ActiveOrderCount: null);
    }
}

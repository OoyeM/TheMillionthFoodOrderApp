namespace TheMillionthFoodOrderApp.Application.Orders;

/// <summary>
/// Thrown when an order requests a time slot that can no longer be fulfilled —
/// either the slot is at capacity or it is no longer offered (aged past the
/// lead-time window, misaligned, or outside opening hours). US-FP-019.
///
/// Mapped to HTTP 409 by <c>CreateOrderEndpoint</c> so the storefront can
/// refresh its slot list and ask the customer to pick again.
/// </summary>
public sealed class TimeSlotUnavailableException(string message) : Exception(message);

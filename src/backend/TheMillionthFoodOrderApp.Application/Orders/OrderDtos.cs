using TheMillionthFoodOrderApp.Application.OrderLifecycle;

namespace TheMillionthFoodOrderApp.Application.Orders;

/// <summary>
/// Represents a single product line in a create-order request.
/// </summary>
public sealed record OrderItemInput(
    Guid ProductId,
    int Quantity,
    IReadOnlyList<Guid> SelectedModifierIds);

/// <summary>
/// Application-layer DTO for placing a new order.
/// </summary>
public sealed record CreateOrderRequest(
    Guid ShopId,
    string BrandSlug,
    string OrderType,
    string PaymentMethod,
    string? CustomerFirstName,
    string? CustomerLastName,
    IReadOnlyList<OrderItemInput> Items,
    string? CustomerEmail = null,
    string? CustomerPhone = null,
    /// <summary>BCP-47 checkout language (e.g. "nl-BE") for the digital receipt; falls back to the brand default (US-FP-051).</summary>
    string? LanguageCode = null,
    /// <summary>Table number for eat-in orders (US-FP-024/066); null for takeaway/delivery or when not captured.</summary>
    int? TableNumber = null,
    /// <summary>
    /// UTC start of the chosen time slot (US-FP-019). Null means ASAP (no slot constraint).
    /// </summary>
    DateTimeOffset? TimeSlotStart = null);

/// <summary>
/// Application-layer DTO for placing a new in-store order via counter staff.
/// PaymentMethod is forced to CashAtPickup by the service regardless of what is passed.
/// Note: CreatedByStaffId is NOT part of this DTO — it is captured server-side from the
/// authenticated user's claims and passed as a separate explicit parameter to
/// <see cref="IOrderService.CreateInStoreOrderAsync"/> to prevent any possibility of
/// client-supplied values being trusted.
/// </summary>
public sealed record CreateInStoreOrderRequest(
    Guid ShopId,
    string BrandSlug,
    string OrderType,
    string PaymentMethod,
    string? CustomerFirstName,
    string? CustomerLastName,
    int? TableNumber,
    IReadOnlyList<OrderItemInput> Items);

/// <summary>Response DTO for a selected modifier on an order item.</summary>
public sealed record SelectedModifierResponse(
    Guid ModifierId,
    string ModifierName,
    decimal PriceAdjustment);

/// <summary>Response DTO for a single order line item.</summary>
public sealed record OrderItemResponse(
    Guid ProductId,
    string ProductName,
    int Quantity,
    decimal UnitGrossPrice,
    decimal UnitNetPrice,
    decimal UnitVatAmount,
    decimal LineTotal,
    IReadOnlyList<SelectedModifierResponse> SelectedModifiers);

/// <summary>
/// Full response DTO returned when an order is created or retrieved.
/// <para>
/// <c>TableNumber</c> and <c>CreatedByStaffId</c> are populated only for in-store orders
/// placed via the counter-staff endpoint. They are <see langword="null"/> for customer-facing
/// online orders (public <c>POST /orders</c>). API consumers must not assume these fields
/// are always populated.
/// </para>
/// </summary>
public sealed record OrderResponse(
    Guid Id,
    string OrderNumber,
    Guid ShopId,
    string BrandSlug,
    string OrderType,
    string PaymentMethod,
    string StatusName,
    string? CustomerName,
    IReadOnlyList<OrderItemResponse> Items,
    decimal VatRatePercent,
    decimal SubtotalGross,
    decimal TotalVatAmount,
    decimal TotalNet,
    decimal TotalGross,
    DateTimeOffset CreatedAt,
    /// <summary>Table number for eat-in in-store orders; null for online/pickup/delivery orders.</summary>
    int? TableNumber = null,
    /// <summary>Counter staff ID extracted server-side from JWT claims; null for customer-facing orders.</summary>
    Guid? CreatedByStaffId = null,
    /// <summary>Optional customer email address for digital receipts (US-FP-017).</summary>
    string? CustomerEmail = null,
    /// <summary>Optional customer phone number (US-FP-017).</summary>
    string? CustomerPhone = null,
    /// <summary>
    /// Seller legal block for receipts (US-FP-052): the shop's name, VAT number, and a
    /// single-line address. Populated on the order-create and order-tracking responses
    /// (where the shop is loaded); null on the status-advance response which the kitchen
    /// display consumes and which does not render a receipt.
    /// </summary>
    string? ShopName = null,
    string? ShopVatNumber = null,
    string? ShopAddressLine = null,
    /// <summary>Customer first name (US-FP-051). The combined <see cref="CustomerName"/> remains populated for display.</summary>
    string? CustomerFirstName = null,
    /// <summary>Customer last name (US-FP-051).</summary>
    string? CustomerLastName = null,
    /// <summary>BCP-47 checkout language captured for the digital receipt (US-FP-051).</summary>
    string? LanguageCode = null,
    /// <summary>
    /// Denormalised shop-local time-slot label (e.g. "18:30") stored at order creation (US-FP-019).
    /// Null when the customer chose ASAP or the shop does not use time-slot ordering.
    /// </summary>
    string? TimeSlot = null);

/// <summary>
/// Combined response DTO returned by the order-tracking endpoints.
/// Bundles the full order detail with the shop's configured lifecycle so the
/// frontend can render the status progression without a second round-trip.
/// </summary>
public record OrderTrackingResponse(
    OrderResponse Order,
    OrderLifecycleResponse Lifecycle);

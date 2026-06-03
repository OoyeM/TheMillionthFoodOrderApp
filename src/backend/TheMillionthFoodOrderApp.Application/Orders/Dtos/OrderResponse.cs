namespace TheMillionthFoodOrderApp.Application.Orders.Dtos;

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
    string? CustomerPhone = null);

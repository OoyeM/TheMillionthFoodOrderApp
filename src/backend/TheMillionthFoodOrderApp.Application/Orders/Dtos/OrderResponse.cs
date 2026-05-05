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

/// <summary>Full response DTO returned when an order is created or retrieved.</summary>
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
    DateTimeOffset CreatedAt);

namespace TheMillionthFoodOrderApp.Application.Orders.Dtos;

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
    string? CustomerName,
    int? TableNumber,
    IReadOnlyList<OrderItemInput> Items);

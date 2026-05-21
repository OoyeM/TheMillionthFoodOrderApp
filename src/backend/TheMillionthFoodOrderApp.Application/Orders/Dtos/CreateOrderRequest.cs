namespace TheMillionthFoodOrderApp.Application.Orders.Dtos;

/// <summary>
/// Application-layer DTO for placing a new order.
/// </summary>
public sealed record CreateOrderRequest(
    Guid ShopId,
    string BrandSlug,
    string OrderType,
    string PaymentMethod,
    string? CustomerName,
    IReadOnlyList<OrderItemInput> Items,
    string? TableNumber = null);

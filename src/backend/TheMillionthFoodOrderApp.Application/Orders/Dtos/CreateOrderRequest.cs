namespace TheMillionthFoodOrderApp.Application.Orders.Dtos;

/// <summary>
/// Application-layer DTO for placing a new order.
/// </summary>
public sealed record CreateOrderRequest(
    Guid ShopId,
    string BrandSlug,
    string OrderType,
    string? CustomerName,
    IReadOnlyList<OrderItemInput> Items);

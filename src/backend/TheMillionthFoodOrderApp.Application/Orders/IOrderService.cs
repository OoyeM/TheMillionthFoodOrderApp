using TheMillionthFoodOrderApp.Application.Orders.Dtos;

namespace TheMillionthFoodOrderApp.Application.Orders;

/// <summary>
/// Application service for placing and managing orders.
/// </summary>
public interface IOrderService
{
    /// <summary>
    /// Places a new order for the specified shop, resolving prices from the database
    /// and applying the correct Belgian VAT rate based on order type.
    /// </summary>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when a referenced product ID does not exist.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the order type is invalid or items list is empty.
    /// </exception>
    Task<OrderResponse> CreateOrderAsync(CreateOrderRequest request, CancellationToken cancellationToken = default);
}

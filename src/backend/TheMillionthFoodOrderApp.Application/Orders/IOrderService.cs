
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

    /// <summary>
    /// Places a new in-store order on behalf of authenticated counter staff.
    /// Differs from <see cref="CreateOrderAsync"/> in the following ways:
    /// <list type="bullet">
    ///   <item><description>PaymentMethod is forced to <c>CashAtPickup</c> regardless of the request value.</description></item>
    ///   <item><description><c>TableNumber</c> is required for <c>EatIn</c> orders and must be greater than zero.</description></item>
    ///   <item><description><c>CreatedByStaffId</c> is captured server-side from the authenticated user claim — not from the client.</description></item>
    /// </list>
    /// </summary>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when a referenced product ID or the shop does not exist.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Thrown when the order type is invalid, items list is empty,
    /// or TableNumber is null/zero for an EatIn order.
    /// </exception>
    /// <param name="createdByStaffId">
    /// The staff member's identity extracted server-side from the authenticated user's claims.
    /// Passed separately from <paramref name="request"/> so this value is never read from the
    /// client-submitted DTO — it is always injected by the endpoint from the JWT/session.
    /// </param>
    Task<OrderResponse> CreateInStoreOrderAsync(
        CreateInStoreOrderRequest request,
        Guid? createdByStaffId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Advances an order to a new status in the shop's configured lifecycle (US-FP-023).
    /// Validates that a transition from the order's current status to
    /// <paramref name="toStatusId"/> is allowed before applying it, then persists the
    /// change — which dispatches <c>OrderStatusChangedEvent</c> for real-time notification.
    /// </summary>
    /// <param name="shopId">The shop the order belongs to (from the route).</param>
    /// <param name="orderId">The order to advance.</param>
    /// <param name="toStatusId">The target lifecycle status's identifier.</param>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when the order does not exist for the shop, or the target status id is
    /// not part of the shop's lifecycle.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the shop has no lifecycle config, the order's current status is not
    /// in the lifecycle, or the requested transition is not configured.
    /// </exception>
    Task<OrderResponse> AdvanceOrderStatusAsync(
        Guid shopId,
        Guid orderId,
        Guid toStatusId,
        CancellationToken cancellationToken = default);
}

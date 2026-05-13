using TheMillionthFoodOrderApp.Application.Orders.Dtos;
using TheMillionthFoodOrderApp.Domain.Orders;

namespace TheMillionthFoodOrderApp.Api.Endpoints.Orders;

/// <summary>
/// Maps <see cref="Order"/> aggregates to <see cref="OrderResponse"/> DTOs for the
/// order-tracking endpoints. Duplicating the private helper from OrderService here
/// keeps the endpoint layer self-contained and avoids coupling the API layer to
/// the Application service's internal mapping concern.
/// </summary>
internal static class OrderTrackingMapper
{
    internal static OrderResponse MapOrder(Order order) =>
        new(
            order.Id,
            order.OrderNumber,
            order.ShopId,
            order.BrandSlug,
            order.OrderType.ToString(),
            order.PaymentMethod.ToString(),
            order.StatusName,
            order.CustomerName,
            order.Items
                .Select(i => new OrderItemResponse(
                    i.ProductId,
                    i.ProductName,
                    i.Quantity,
                    i.UnitGrossPrice,
                    i.UnitNetPrice,
                    i.UnitVatAmount,
                    i.LineTotal,
                    i.SelectedModifiers
                        .Select(m => new SelectedModifierResponse(m.ModifierId, m.ModifierName, m.PriceAdjustment))
                        .ToList()
                        .AsReadOnly()))
                .ToList()
                .AsReadOnly(),
            order.VatRatePercent,
            order.SubtotalGross,
            order.TotalVatAmount,
            order.TotalNet,
            order.TotalGross,
            order.CreatedAt);
}

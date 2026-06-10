using TheMillionthFoodOrderApp.Application.Orders;
using TheMillionthFoodOrderApp.Domain.Orders;
using TheMillionthFoodOrderApp.Domain.Shops;

namespace TheMillionthFoodOrderApp.Api.Endpoints.Orders;

/// <summary>
/// Maps <see cref="Order"/> aggregates to <see cref="OrderResponse"/> DTOs for the
/// order-tracking endpoints. Duplicating the private helper from OrderService here
/// keeps the endpoint layer self-contained and avoids coupling the API layer to
/// the Application service's internal mapping concern.
/// </summary>
internal static class OrderTrackingMapper
{
    /// <summary>
    /// Maps an order for tracking responses. When <paramref name="shop"/> is supplied, the
    /// seller legal block (name, VAT number, address) is included so counter staff can
    /// reprint a complete receipt (US-FP-052).
    /// </summary>
    internal static OrderResponse MapOrder(Order order, Shop? shop = null) =>
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
            order.CreatedAt,
            order.TableNumber,
            order.CreatedByStaffId,
            order.CustomerEmail,
            order.CustomerPhone,
            shop?.Name,
            shop?.VatNumber,
            shop?.Address.ToSingleLine(),
            order.CustomerFirstName,
            order.CustomerLastName,
            order.LanguageCode,
            order.TimeSlotStart,
            order.TimeSlotEnd);
}

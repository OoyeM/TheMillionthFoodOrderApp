using TheMillionthFoodOrderApp.Domain.Orders;

namespace TheMillionthFoodOrderApp.Tests.Unit.Orders;

/// <summary>
/// Unit tests for the <see cref="Order"/> aggregate (US-FP-019 additions).
/// Covers time-slot params on <see cref="Order.Create"/> and default behaviour.
/// </summary>
public sealed class OrderTests
{
    // ── Fixtures ─────────────────────────────────────────────────────────────

    /// <summary>Minimal valid items list — one frietje at €3.50 gross.</summary>
    private static List<OrderItem> MinimalItems(Guid orderId)
        => [OrderItem.Create(orderId, Guid.CreateVersion7(), "Frietje", 1, 3.50m, 3.30m, 0.20m, [])];

    // ── Time slot params ─────────────────────────────────────────────────────

    [Test]
    public async Task Create_WithTimeSlotParams_PersistsBothProperties()
    {
        var orderId = Guid.CreateVersion7();
        var slotStart = DateTimeOffset.UtcNow.AddHours(1);
        const string slotLabel = "17:15";

        var order = Order.Create(
            orderId: orderId,
            shopId: Guid.CreateVersion7(),
            brandSlug: "frietjes",
            orderNumber: "ABCD1234",
            orderType: OrderType.Pickup,
            paymentMethod: PaymentMethod.CashAtPickup,
            statusName: "Placed",
            customerFirstName: "Jan",
            customerLastName: "Janssen",
            vatRatePercent: 6m,
            items: MinimalItems(orderId),
            timeSlotStart: slotStart,
            timeSlot: slotLabel);

        await Assert.That(order.TimeSlotStart).IsEqualTo(slotStart);
        await Assert.That(order.TimeSlot).IsEqualTo(slotLabel);
    }

    [Test]
    public async Task Create_WithoutTimeSlotParams_DefaultsToNull()
    {
        var orderId = Guid.CreateVersion7();

        var order = Order.Create(
            orderId: orderId,
            shopId: Guid.CreateVersion7(),
            brandSlug: "frietjes",
            orderNumber: "ABCD5678",
            orderType: OrderType.Pickup,
            paymentMethod: PaymentMethod.CashAtPickup,
            statusName: "Placed",
            customerFirstName: "Marie",
            customerLastName: "Claes",
            vatRatePercent: 6m,
            items: MinimalItems(orderId));

        await Assert.That(order.TimeSlotStart).IsNull();
        await Assert.That(order.TimeSlot).IsNull();
    }
}

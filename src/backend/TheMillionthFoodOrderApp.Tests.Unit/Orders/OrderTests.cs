using TheMillionthFoodOrderApp.Domain.Orders;

namespace TheMillionthFoodOrderApp.Tests.Unit.Orders;

/// <summary>
/// Unit tests for <see cref="Order.Create"/> time-slot invariants (US-FP-019).
/// </summary>
public sealed class OrderTests
{
    // ── Helpers ───────────────────────────────────────────────────────────────

    private static OrderItem MakeItem()
    {
        var orderId = Guid.CreateVersion7();
        return OrderItem.Create(orderId, Guid.CreateVersion7(), "Test Product", 1, 3.50m, 3.30m, 0.20m, []);
    }

    private static Order MakeOrder(
        DateTimeOffset? timeSlotStart = null,
        DateTimeOffset? timeSlotEnd = null)
    {
        return Order.Create(
            orderId: Guid.CreateVersion7(),
            shopId: Guid.CreateVersion7(),
            brandSlug: "frietjes",
            orderNumber: "TEST-001",
            orderType: OrderType.Pickup,
            paymentMethod: PaymentMethod.CashAtPickup,
            statusName: "Placed",
            customerFirstName: "Jan",
            customerLastName: "Janssen",
            vatRatePercent: 6m,
            items: [MakeItem()],
            timeSlotStart: timeSlotStart,
            timeSlotEnd: timeSlotEnd);
    }

    // ── Both null (ASAP) ──────────────────────────────────────────────────────

    [Test]
    public async Task Create_WithNullTimeSlots_Succeeds_AndSlotsAreNull()
    {
        var order = MakeOrder(null, null);

        await Assert.That(order.TimeSlotStart).IsNull();
        await Assert.That(order.TimeSlotEnd).IsNull();
    }

    // ── Both set — valid ──────────────────────────────────────────────────────

    [Test]
    public async Task Create_WithValidTimeSlots_Succeeds_AndPersists()
    {
        var start = new DateTimeOffset(2026, 6, 10, 15, 0, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2026, 6, 10, 15, 10, 0, TimeSpan.Zero);

        var order = MakeOrder(start, end);

        await Assert.That(order.TimeSlotStart).IsEqualTo(start);
        await Assert.That(order.TimeSlotEnd).IsEqualTo(end);
    }

    // ── Start only (end missing) ───────────────────────────────────────────────

    [Test]
    public async Task Create_WithStartOnlyAndNullEnd_ThrowsArgumentException()
    {
        var start = new DateTimeOffset(2026, 6, 10, 15, 0, 0, TimeSpan.Zero);

        await Assert.ThrowsAsync<ArgumentException>(() =>
        {
            _ = MakeOrder(start, null);
            return Task.CompletedTask;
        });
    }

    // ── End only (start missing) ───────────────────────────────────────────────

    [Test]
    public async Task Create_WithEndOnlyAndNullStart_ThrowsArgumentException()
    {
        var end = new DateTimeOffset(2026, 6, 10, 15, 10, 0, TimeSpan.Zero);

        await Assert.ThrowsAsync<ArgumentException>(() =>
        {
            _ = MakeOrder(null, end);
            return Task.CompletedTask;
        });
    }

    // ── End before start ──────────────────────────────────────────────────────

    [Test]
    public async Task Create_WithEndBeforeStart_ThrowsArgumentException()
    {
        var start = new DateTimeOffset(2026, 6, 10, 15, 10, 0, TimeSpan.Zero);
        var end = new DateTimeOffset(2026, 6, 10, 15, 0, 0, TimeSpan.Zero); // end < start

        await Assert.ThrowsAsync<ArgumentException>(() =>
        {
            _ = MakeOrder(start, end);
            return Task.CompletedTask;
        });
    }

    // ── End equal to start ────────────────────────────────────────────────────

    [Test]
    public async Task Create_WithEndEqualToStart_ThrowsArgumentException()
    {
        var instant = new DateTimeOffset(2026, 6, 10, 15, 0, 0, TimeSpan.Zero);

        await Assert.ThrowsAsync<ArgumentException>(() =>
        {
            _ = MakeOrder(instant, instant); // end == start
            return Task.CompletedTask;
        });
    }
}

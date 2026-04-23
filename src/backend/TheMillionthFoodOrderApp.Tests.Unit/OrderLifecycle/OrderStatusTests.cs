using TheMillionthFoodOrderApp.Domain.OrderLifecycle;

namespace TheMillionthFoodOrderApp.Tests.Unit.OrderLifecycle;

public sealed class OrderStatusTests
{
    private static readonly Guid ConfigId = Guid.NewGuid();

    // ── Create — happy path ───────────────────────────────────────────────────

    [Test]
    public async Task Create_WithAllFields_SetsPropertiesCorrectly()
    {
        var status = OrderStatus.Create(ConfigId, "Preparing", "preparing", 2, false, "#FF5733");

        await Assert.That(status).IsNotNull();
        await Assert.That(status.Id).IsNotEqualTo(Guid.Empty);
        await Assert.That(status.OrderLifecycleConfigId).IsEqualTo(ConfigId);
        await Assert.That(status.Name).IsEqualTo("Preparing");
        await Assert.That(status.SystemKey).IsEqualTo("preparing");
        await Assert.That(status.SortOrder).IsEqualTo(2);
        await Assert.That(status.IsEnabled).IsTrue();
        await Assert.That(status.IsTerminal).IsFalse();
        await Assert.That(status.ColorHex).IsEqualTo("#FF5733");
    }

    [Test]
    public async Task Create_WithNullSystemKey_SetsSystemKeyToNull()
    {
        var status = OrderStatus.Create(ConfigId, "Custom Status", null, 0, false);

        await Assert.That(status.SystemKey).IsNull();
    }

    [Test]
    public async Task Create_WithNullColorHex_SetsColorHexToNull()
    {
        var status = OrderStatus.Create(ConfigId, "Placed", "placed", 0, false);

        await Assert.That(status.ColorHex).IsNull();
    }

    [Test]
    public async Task Create_WithIsTerminalTrue_SetsIsTerminal()
    {
        var status = OrderStatus.Create(ConfigId, "Delivered", "delivered", 5, true);

        await Assert.That(status.IsTerminal).IsTrue();
    }

    [Test]
    public async Task Create_AlwaysDefaultsIsEnabledToTrue()
    {
        var status = OrderStatus.Create(ConfigId, "Some Status", null, 0, false);

        await Assert.That(status.IsEnabled).IsTrue();
    }

    // ── Create — UUIDv7 ───────────────────────────────────────────────────────

    [Test]
    public async Task Create_GeneratesUuidV7()
    {
        var status = OrderStatus.Create(ConfigId, "Placed", "placed", 0, false);

        // UUIDv7 has version nibble = 7 (bits 48-51)
        var version = (status.Id.ToByteArray()[7] >> 4) & 0x0F;
        await Assert.That(version).IsEqualTo(7);
    }

    // ── Create — optional color hex stored as-is (no transformation) ──────────

    [Test]
    public async Task Create_WithColorHex_StoresValueAsProvided()
    {
        var status = OrderStatus.Create(ConfigId, "Ready", "ready", 3, false, "#00FF00");

        await Assert.That(status.ColorHex).IsEqualTo("#00FF00");
    }
}

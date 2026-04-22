using Shouldly;
using TheMillionthFoodOrderApp.Domain.OrderLifecycle;

namespace TheMillionthFoodOrderApp.Tests.Unit.OrderLifecycle;

public sealed class OrderStatusTests
{
    private static readonly Guid ConfigId = Guid.NewGuid();

    // ── Create — happy path ───────────────────────────────────────────────────

    [Fact]
    public void Create_WithAllFields_SetsPropertiesCorrectly()
    {
        var status = OrderStatus.Create(ConfigId, "Preparing", "preparing", 2, false, "#FF5733");

        status.ShouldNotBeNull();
        status.Id.ShouldNotBe(Guid.Empty);
        status.OrderLifecycleConfigId.ShouldBe(ConfigId);
        status.Name.ShouldBe("Preparing");
        status.SystemKey.ShouldBe("preparing");
        status.SortOrder.ShouldBe(2);
        status.IsEnabled.ShouldBeTrue();
        status.IsTerminal.ShouldBeFalse();
        status.ColorHex.ShouldBe("#FF5733");
    }

    [Fact]
    public void Create_WithNullSystemKey_SetsSystemKeyToNull()
    {
        var status = OrderStatus.Create(ConfigId, "Custom Status", null, 0, false);

        status.SystemKey.ShouldBeNull();
    }

    [Fact]
    public void Create_WithNullColorHex_SetsColorHexToNull()
    {
        var status = OrderStatus.Create(ConfigId, "Placed", "placed", 0, false);

        status.ColorHex.ShouldBeNull();
    }

    [Fact]
    public void Create_WithIsTerminalTrue_SetsIsTerminal()
    {
        var status = OrderStatus.Create(ConfigId, "Delivered", "delivered", 5, true);

        status.IsTerminal.ShouldBeTrue();
    }

    [Fact]
    public void Create_AlwaysDefaultsIsEnabledToTrue()
    {
        var status = OrderStatus.Create(ConfigId, "Some Status", null, 0, false);

        status.IsEnabled.ShouldBeTrue();
    }

    // ── Create — UUIDv7 ───────────────────────────────────────────────────────

    [Fact]
    public void Create_GeneratesUuidV7()
    {
        var status = OrderStatus.Create(ConfigId, "Placed", "placed", 0, false);

        // UUIDv7 has version nibble = 7 (bits 48-51)
        var version = (status.Id.ToByteArray()[7] >> 4) & 0x0F;
        version.ShouldBe(7);
    }

    // ── Create — optional color hex stored as-is (no transformation) ──────────

    [Fact]
    public void Create_WithColorHex_StoresValueAsProvided()
    {
        var status = OrderStatus.Create(ConfigId, "Ready", "ready", 3, false, "#00FF00");

        status.ColorHex.ShouldBe("#00FF00");
    }
}

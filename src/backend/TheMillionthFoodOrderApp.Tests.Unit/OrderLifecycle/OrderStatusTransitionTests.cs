using Shouldly;
using TheMillionthFoodOrderApp.Domain.OrderLifecycle;

namespace TheMillionthFoodOrderApp.Tests.Unit.OrderLifecycle;

public sealed class OrderStatusTransitionTests
{
    private static readonly Guid ConfigId = Guid.NewGuid();

    // ── Create — happy path ───────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidArgs_SetsPropertiesCorrectly()
    {
        var fromId = Guid.NewGuid();
        var toId = Guid.NewGuid();

        var transition = OrderStatusTransition.Create(ConfigId, fromId, toId);

        transition.ShouldNotBeNull();
        transition.Id.ShouldNotBe(Guid.Empty);
        transition.OrderLifecycleConfigId.ShouldBe(ConfigId);
        transition.FromStatusId.ShouldBe(fromId);
        transition.ToStatusId.ShouldBe(toId);
    }

    [Fact]
    public void Create_GeneratesUuidV7()
    {
        var transition = OrderStatusTransition.Create(ConfigId, Guid.NewGuid(), Guid.NewGuid());

        // UUIDv7 has version nibble = 7 (bits 48-51)
        var version = (transition.Id.ToByteArray()[7] >> 4) & 0x0F;
        version.ShouldBe(7);
    }

    // ── Create — no self-transition guard in domain ───────────────────────────
    // The domain's Create factory does not enforce FromStatusId != ToStatusId;
    // that constraint lives in ConfigureLifecycle's transition-reference validation
    // (which ensures both sides are in the provided status list). A self-referencing
    // transition can be created — it would only be rejected if validated by the config.

    [Fact]
    public void Create_WithSameFromAndToId_DoesNotThrow()
    {
        var sameId = Guid.NewGuid();

        var transition = OrderStatusTransition.Create(ConfigId, sameId, sameId);

        transition.FromStatusId.ShouldBe(sameId);
        transition.ToStatusId.ShouldBe(sameId);
    }
}

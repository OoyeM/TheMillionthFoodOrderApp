using TheMillionthFoodOrderApp.Domain.OrderLifecycle;

namespace TheMillionthFoodOrderApp.Tests.Unit.OrderLifecycle;

public sealed class OrderStatusTransitionTests
{
    private static readonly Guid ConfigId = Guid.NewGuid();

    // ── Create — happy path ───────────────────────────────────────────────────

    [Test]
    public async Task Create_WithValidArgs_SetsPropertiesCorrectly()
    {
        var fromId = Guid.NewGuid();
        var toId = Guid.NewGuid();

        var transition = OrderStatusTransition.Create(ConfigId, fromId, toId);

        await Assert.That(transition).IsNotNull();
        await Assert.That(transition.Id).IsNotEqualTo(Guid.Empty);
        await Assert.That(transition.OrderLifecycleConfigId).IsEqualTo(ConfigId);
        await Assert.That(transition.FromStatusId).IsEqualTo(fromId);
        await Assert.That(transition.ToStatusId).IsEqualTo(toId);
    }

    [Test]
    public async Task Create_GeneratesUuidV7()
    {
        var transition = OrderStatusTransition.Create(ConfigId, Guid.NewGuid(), Guid.NewGuid());

        // UUIDv7 has version nibble = 7 (bits 48-51)
        var version = (transition.Id.ToByteArray()[7] >> 4) & 0x0F;
        await Assert.That(version).IsEqualTo(7);
    }

    // ── Create — no self-transition guard in domain ───────────────────────────
    // The domain's Create factory does not enforce FromStatusId != ToStatusId;
    // that constraint lives in ConfigureLifecycle's transition-reference validation
    // (which ensures both sides are in the provided status list). A self-referencing
    // transition can be created — it would only be rejected if validated by the config.

    [Test]
    public async Task Create_WithSameFromAndToId_DoesNotThrow()
    {
        var sameId = Guid.NewGuid();

        var transition = OrderStatusTransition.Create(ConfigId, sameId, sameId);

        await Assert.That(transition.FromStatusId).IsEqualTo(sameId);
        await Assert.That(transition.ToStatusId).IsEqualTo(sameId);
    }
}

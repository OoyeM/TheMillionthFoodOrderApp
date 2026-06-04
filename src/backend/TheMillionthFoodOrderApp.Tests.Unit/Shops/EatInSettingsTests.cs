using TheMillionthFoodOrderApp.Domain.Shops;

namespace TheMillionthFoodOrderApp.Tests.Unit.Shops;

/// <summary>Unit tests for the <see cref="EatInSettings"/> value object (US-FP-066).</summary>
public sealed class EatInSettingsTests
{
    [Test]
    public async Task CreateDefault_IsEnabledAndRequiresTableNumber()
    {
        var settings = EatInSettings.CreateDefault();

        await Assert.That(settings.IsEnabled).IsTrue();
        await Assert.That(settings.RequiresTableNumber).IsTrue();
    }

    [Test]
    public async Task Constructor_SetsBothFlags()
    {
        var settings = new EatInSettings(isEnabled: false, requiresTableNumber: false);

        await Assert.That(settings.IsEnabled).IsFalse();
        await Assert.That(settings.RequiresTableNumber).IsFalse();
    }

    [Test]
    public async Task SameValues_AreEqual()
    {
        var a = new EatInSettings(true, false);
        var b = new EatInSettings(true, false);

        await Assert.That(a).IsEqualTo(b);
    }

    [Test]
    public async Task DifferentValues_AreNotEqual()
    {
        var a = new EatInSettings(true, true);
        var b = new EatInSettings(true, false);

        await Assert.That(a).IsNotEqualTo(b);
    }
}

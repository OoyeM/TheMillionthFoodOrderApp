using TheMillionthFoodOrderApp.Domain.Shops;

namespace TheMillionthFoodOrderApp.Tests.Unit.Shops;

/// <summary>Unit tests for the <see cref="TimeSlotOrderingSettings"/> value object (US-FP-020).</summary>
public sealed class TimeSlotOrderingSettingsTests
{
    [Test]
    public async Task Disabled_HasNoIntervalOrMax()
    {
        var settings = TimeSlotOrderingSettings.Disabled();

        await Assert.That(settings.IsEnabled).IsFalse();
        await Assert.That(settings.Interval).IsNull();
        await Assert.That(settings.MaxOrdersPerInterval).IsNull();
    }

    [Test]
    public async Task Enabled_WithValidValues_SetsProperties()
    {
        var settings = TimeSlotOrderingSettings.Enabled(TimeSlotInterval.FifteenMinutes, 3);

        await Assert.That(settings.IsEnabled).IsTrue();
        await Assert.That(settings.Interval).IsEqualTo(TimeSlotInterval.FifteenMinutes);
        await Assert.That(settings.MaxOrdersPerInterval).IsEqualTo(3);
    }

    [Test]
    public async Task Enabled_WithZeroMax_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
        {
            _ = TimeSlotOrderingSettings.Enabled(TimeSlotInterval.TenMinutes, 0);
            return Task.CompletedTask;
        });
    }

    [Test]
    public async Task Enabled_WithNegativeMax_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
        {
            _ = TimeSlotOrderingSettings.Enabled(TimeSlotInterval.TenMinutes, -5);
            return Task.CompletedTask;
        });
    }

    [Test]
    public async Task Enabled_WithUndefinedInterval_ThrowsArgumentException()
    {
        await Assert.ThrowsAsync<ArgumentException>(() =>
        {
            _ = TimeSlotOrderingSettings.Enabled((TimeSlotInterval)7, 3);
            return Task.CompletedTask;
        });
    }

    [Test]
    public async Task Enabled_SameValues_AreEqual()
    {
        var a = TimeSlotOrderingSettings.Enabled(TimeSlotInterval.FiveMinutes, 2);
        var b = TimeSlotOrderingSettings.Enabled(TimeSlotInterval.FiveMinutes, 2);

        await Assert.That(a).IsEqualTo(b);
    }

    [Test]
    public async Task Enabled_DifferentValues_AreNotEqual()
    {
        var a = TimeSlotOrderingSettings.Enabled(TimeSlotInterval.FiveMinutes, 2);
        var b = TimeSlotOrderingSettings.Enabled(TimeSlotInterval.TenMinutes, 2);

        await Assert.That(a).IsNotEqualTo(b);
    }
}

using TheMillionthFoodOrderApp.Domain.Common;
using DomainTaxConfiguration = TheMillionthFoodOrderApp.Domain.TaxConfiguration.TaxConfiguration;

namespace TheMillionthFoodOrderApp.Tests.Unit.TaxConfiguration;

public sealed class TaxConfigurationTests
{
    // ── CreateBelgianDefault ─────────────────────────────────────────────────

    [Test]
    public async Task CreateBelgianDefault_HasTwoRates()
    {
        var config = DomainTaxConfiguration.CreateBelgianDefault();

        await Assert.That(config.VatRates.Count).IsEqualTo(2);
    }

    [Test]
    public async Task CreateBelgianDefault_HasCorrectTakeawayRate()
    {
        var config = DomainTaxConfiguration.CreateBelgianDefault();

        var takeaway = config.VatRates.Single(r => r.ConsumptionMode == ConsumptionMode.Takeaway);
        await Assert.That(takeaway.RatePercentage).IsEqualTo(6m);
    }

    [Test]
    public async Task CreateBelgianDefault_HasCorrectEatInRate()
    {
        var config = DomainTaxConfiguration.CreateBelgianDefault();

        var eatIn = config.VatRates.Single(r => r.ConsumptionMode == ConsumptionMode.EatIn);
        await Assert.That(eatIn.RatePercentage).IsEqualTo(21m);
    }

    // ── UpdateRates ──────────────────────────────────────────────────────────

    [Test]
    public async Task UpdateRates_ValidRates_ReplacesExisting()
    {
        var config = DomainTaxConfiguration.CreateBelgianDefault();
        var updatedAt = config.UpdatedAt;

        config.UpdateRates(
        [
            (ConsumptionMode.Takeaway, 7m),
            (ConsumptionMode.EatIn, 22m),
        ]);

        await Assert.That(config.VatRates.Count).IsEqualTo(2);
        await Assert.That(config.VatRates.Single(r => r.ConsumptionMode == ConsumptionMode.Takeaway).RatePercentage).IsEqualTo(7m);
        await Assert.That(config.VatRates.Single(r => r.ConsumptionMode == ConsumptionMode.EatIn).RatePercentage).IsEqualTo(22m);
        await Assert.That(config.UpdatedAt).IsGreaterThanOrEqualTo(updatedAt);
    }

    [Test]
    public async Task UpdateRates_MissingConsumptionMode_ThrowsArgumentException()
    {
        var config = DomainTaxConfiguration.CreateBelgianDefault();

        // Providing only Takeaway — EatIn is missing
        await Assert.ThrowsAsync<ArgumentException>(() =>
        {
            config.UpdateRates([(ConsumptionMode.Takeaway, 7m)]);
            return Task.CompletedTask;
        });
    }

    [Test]
    public async Task UpdateRates_DuplicateConsumptionMode_ThrowsArgumentException()
    {
        var config = DomainTaxConfiguration.CreateBelgianDefault();

        // Two Takeaway rates and no EatIn rate
        await Assert.ThrowsAsync<ArgumentException>(() =>
        {
            config.UpdateRates(
            [
                (ConsumptionMode.Takeaway, 6m),
                (ConsumptionMode.Takeaway, 8m),
            ]);
            return Task.CompletedTask;
        });
    }

    // ── GetRateForMode ────────────────────────────────────────────────────────

    [Test]
    public async Task GetRateForMode_Takeaway_Returns6()
    {
        var config = DomainTaxConfiguration.CreateBelgianDefault();

        var rate = config.GetRateForMode(ConsumptionMode.Takeaway);

        await Assert.That(rate).IsEqualTo(6m);
    }

    [Test]
    public async Task GetRateForMode_UnknownMode_ThrowsKeyNotFoundException()
    {
        var config = DomainTaxConfiguration.CreateBelgianDefault();

        // Cast an integer value that is not a defined ConsumptionMode member
        var unknownMode = (ConsumptionMode)999;

        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            Task.FromResult(config.GetRateForMode(unknownMode)));
    }
}

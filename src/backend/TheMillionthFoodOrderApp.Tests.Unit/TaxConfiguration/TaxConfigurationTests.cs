using Shouldly;
using TheMillionthFoodOrderApp.Domain.Common;
using DomainTaxConfiguration = TheMillionthFoodOrderApp.Domain.TaxConfiguration.TaxConfiguration;

namespace TheMillionthFoodOrderApp.Tests.Unit.TaxConfiguration;

public sealed class TaxConfigurationTests
{
    // ── CreateBelgianDefault ─────────────────────────────────────────────────

    [Fact]
    public void CreateBelgianDefault_HasTwoRates()
    {
        var config = DomainTaxConfiguration.CreateBelgianDefault();

        config.VatRates.Count.ShouldBe(2);
    }

    [Fact]
    public void CreateBelgianDefault_HasCorrectTakeawayRate()
    {
        var config = DomainTaxConfiguration.CreateBelgianDefault();

        var takeaway = config.VatRates.Single(r => r.ConsumptionMode == ConsumptionMode.Takeaway);
        takeaway.RatePercentage.ShouldBe(6m);
    }

    [Fact]
    public void CreateBelgianDefault_HasCorrectEatInRate()
    {
        var config = DomainTaxConfiguration.CreateBelgianDefault();

        var eatIn = config.VatRates.Single(r => r.ConsumptionMode == ConsumptionMode.EatIn);
        eatIn.RatePercentage.ShouldBe(21m);
    }

    // ── UpdateRates ──────────────────────────────────────────────────────────

    [Fact]
    public void UpdateRates_ValidRates_ReplacesExisting()
    {
        var config = DomainTaxConfiguration.CreateBelgianDefault();
        var updatedAt = config.UpdatedAt;

        config.UpdateRates(
        [
            (ConsumptionMode.Takeaway, 7m),
            (ConsumptionMode.EatIn, 22m),
        ]);

        config.VatRates.Count.ShouldBe(2);
        config.VatRates.Single(r => r.ConsumptionMode == ConsumptionMode.Takeaway).RatePercentage.ShouldBe(7m);
        config.VatRates.Single(r => r.ConsumptionMode == ConsumptionMode.EatIn).RatePercentage.ShouldBe(22m);
        config.UpdatedAt.ShouldBeGreaterThanOrEqualTo(updatedAt);
    }

    [Fact]
    public void UpdateRates_MissingConsumptionMode_ThrowsArgumentException()
    {
        var config = DomainTaxConfiguration.CreateBelgianDefault();

        // Providing only Takeaway — EatIn is missing
        Should.Throw<ArgumentException>(() =>
            config.UpdateRates([(ConsumptionMode.Takeaway, 7m)]));
    }

    [Fact]
    public void UpdateRates_DuplicateConsumptionMode_ThrowsArgumentException()
    {
        var config = DomainTaxConfiguration.CreateBelgianDefault();

        // Two Takeaway rates and no EatIn rate
        Should.Throw<ArgumentException>(() =>
            config.UpdateRates(
            [
                (ConsumptionMode.Takeaway, 6m),
                (ConsumptionMode.Takeaway, 8m),
            ]));
    }

    // ── GetRateForMode ────────────────────────────────────────────────────────

    [Fact]
    public void GetRateForMode_Takeaway_Returns6()
    {
        var config = DomainTaxConfiguration.CreateBelgianDefault();

        var rate = config.GetRateForMode(ConsumptionMode.Takeaway);

        rate.ShouldBe(6m);
    }

    [Fact]
    public void GetRateForMode_UnknownMode_ThrowsKeyNotFoundException()
    {
        var config = DomainTaxConfiguration.CreateBelgianDefault();

        // Cast an integer value that is not a defined ConsumptionMode member
        var unknownMode = (ConsumptionMode)999;

        Should.Throw<KeyNotFoundException>(() =>
            config.GetRateForMode(unknownMode));
    }
}

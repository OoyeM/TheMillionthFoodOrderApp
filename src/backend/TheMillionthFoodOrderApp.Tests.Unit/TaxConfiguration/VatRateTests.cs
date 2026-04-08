using Shouldly;
using TheMillionthFoodOrderApp.Domain.Common;
using TheMillionthFoodOrderApp.Domain.TaxConfiguration;

namespace TheMillionthFoodOrderApp.Tests.Unit.TaxConfiguration;

public sealed class VatRateTests
{
    private static readonly Guid SomeTaxConfigId = Guid.CreateVersion7();

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidData_SetsProperties()
    {
        var vatRate = VatRate.Create(SomeTaxConfigId, ConsumptionMode.Takeaway, 6m);

        vatRate.ShouldNotBeNull();
        vatRate.Id.ShouldNotBe(Guid.Empty);
        vatRate.TaxConfigurationId.ShouldBe(SomeTaxConfigId);
        vatRate.ConsumptionMode.ShouldBe(ConsumptionMode.Takeaway);
        vatRate.RatePercentage.ShouldBe(6m);
    }

    [Fact]
    public void Create_WithNegativeRate_ThrowsArgumentOutOfRangeException()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            VatRate.Create(SomeTaxConfigId, ConsumptionMode.Takeaway, -1m));
    }

    [Fact]
    public void Create_WithRateOver100_ThrowsArgumentOutOfRangeException()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            VatRate.Create(SomeTaxConfigId, ConsumptionMode.Takeaway, 100.01m));
    }

    [Fact]
    public void Create_WithZeroRate_Succeeds()
    {
        var vatRate = VatRate.Create(SomeTaxConfigId, ConsumptionMode.EatIn, 0m);

        vatRate.RatePercentage.ShouldBe(0m);
    }

    [Fact]
    public void Create_WithRate100_Succeeds()
    {
        var vatRate = VatRate.Create(SomeTaxConfigId, ConsumptionMode.EatIn, 100m);

        vatRate.RatePercentage.ShouldBe(100m);
    }
}

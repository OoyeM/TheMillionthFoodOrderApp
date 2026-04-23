using TheMillionthFoodOrderApp.Domain.Common;
using TheMillionthFoodOrderApp.Domain.TaxConfiguration;

namespace TheMillionthFoodOrderApp.Tests.Unit.TaxConfiguration;

public sealed class VatRateTests
{
    private static readonly Guid SomeTaxConfigId = Guid.CreateVersion7();

    // ── Create ────────────────────────────────────────────────────────────────

    [Test]
    public async Task Create_WithValidData_SetsProperties()
    {
        var vatRate = VatRate.Create(SomeTaxConfigId, ConsumptionMode.Takeaway, 6m);

        await Assert.That(vatRate).IsNotNull();
        await Assert.That(vatRate.Id).IsNotEqualTo(Guid.Empty);
        await Assert.That(vatRate.TaxConfigurationId).IsEqualTo(SomeTaxConfigId);
        await Assert.That(vatRate.ConsumptionMode).IsEqualTo(ConsumptionMode.Takeaway);
        await Assert.That(vatRate.RatePercentage).IsEqualTo(6m);
    }

    [Test]
    public async Task Create_WithNegativeRate_ThrowsArgumentOutOfRangeException()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            Task.FromResult(VatRate.Create(SomeTaxConfigId, ConsumptionMode.Takeaway, -1m)));
    }

    [Test]
    public async Task Create_WithRateOver100_ThrowsArgumentOutOfRangeException()
    {
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            Task.FromResult(VatRate.Create(SomeTaxConfigId, ConsumptionMode.Takeaway, 100.01m)));
    }

    [Test]
    public async Task Create_WithZeroRate_Succeeds()
    {
        var vatRate = VatRate.Create(SomeTaxConfigId, ConsumptionMode.EatIn, 0m);

        await Assert.That(vatRate.RatePercentage).IsEqualTo(0m);
    }

    [Test]
    public async Task Create_WithRate100_Succeeds()
    {
        var vatRate = VatRate.Create(SomeTaxConfigId, ConsumptionMode.EatIn, 100m);

        await Assert.That(vatRate.RatePercentage).IsEqualTo(100m);
    }
}

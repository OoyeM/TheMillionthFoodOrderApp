using TheMillionthFoodOrderApp.Domain.TaxConfiguration;

namespace TheMillionthFoodOrderApp.Tests.Unit.TaxConfiguration;

public sealed class TaxCalculatorTests
{
    // ── 6% Takeaway ───────────────────────────────────────────────────────────

    [Test]
    public async Task CalculateFromGross_Takeaway6Percent_ReturnsCorrectBreakdown()
    {
        var result = TaxCalculator.CalculateFromGross(3.50m, 6m);

        await Assert.That(result.GrossAmount).IsEqualTo(3.50m);
        await Assert.That(result.NetAmount).IsEqualTo(3.30m);
        await Assert.That(result.VatAmount).IsEqualTo(0.20m);
        await Assert.That(result.VatRatePercentage).IsEqualTo(6m);
    }

    // ── 21% EatIn ─────────────────────────────────────────────────────────────

    [Test]
    public async Task CalculateFromGross_EatIn21Percent_ReturnsCorrectBreakdown()
    {
        var result = TaxCalculator.CalculateFromGross(3.50m, 21m);

        await Assert.That(result.GrossAmount).IsEqualTo(3.50m);
        await Assert.That(result.NetAmount).IsEqualTo(2.89m);
        await Assert.That(result.VatAmount).IsEqualTo(0.61m);
        await Assert.That(result.VatRatePercentage).IsEqualTo(21m);
    }

    // ── Zero amount ───────────────────────────────────────────────────────────

    [Test]
    public async Task CalculateFromGross_ZeroAmount_ReturnsAllZeros()
    {
        var result = TaxCalculator.CalculateFromGross(0m, 6m);

        await Assert.That(result.GrossAmount).IsEqualTo(0m);
        await Assert.That(result.NetAmount).IsEqualTo(0m);
        await Assert.That(result.VatAmount).IsEqualTo(0m);
        await Assert.That(result.VatRatePercentage).IsEqualTo(6m);
    }

    // ── Zero rate ─────────────────────────────────────────────────────────────

    [Test]
    public async Task CalculateFromGross_ZeroRate_NetEqualsGross()
    {
        var result = TaxCalculator.CalculateFromGross(5.00m, 0m);

        await Assert.That(result.GrossAmount).IsEqualTo(5.00m);
        await Assert.That(result.NetAmount).IsEqualTo(5.00m);
        await Assert.That(result.VatAmount).IsEqualTo(0m);
        await Assert.That(result.VatRatePercentage).IsEqualTo(0m);
    }

    // ── Rounding edge case ────────────────────────────────────────────────────

    [Test]
    public async Task CalculateFromGross_RoundingEdgeCase_SumsCorrectly()
    {
        // 1.99 / 1.06 = 1.87735... → net rounds to 1.88, vat = 1.99 - 1.88 = 0.11
        var result = TaxCalculator.CalculateFromGross(1.99m, 6m);

        // Net + VAT must always reconstitute the gross exactly
        await Assert.That(result.NetAmount + result.VatAmount).IsEqualTo(result.GrossAmount);
        await Assert.That(result.GrossAmount).IsEqualTo(1.99m);
    }
}

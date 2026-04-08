using Shouldly;
using TheMillionthFoodOrderApp.Domain.TaxConfiguration;

namespace TheMillionthFoodOrderApp.Tests.Unit.TaxConfiguration;

public sealed class TaxCalculatorTests
{
    // ── 6% Takeaway ───────────────────────────────────────────────────────────

    [Fact]
    public void CalculateFromGross_Takeaway6Percent_ReturnsCorrectBreakdown()
    {
        var result = TaxCalculator.CalculateFromGross(3.50m, 6m);

        result.GrossAmount.ShouldBe(3.50m);
        result.NetAmount.ShouldBe(3.30m);
        result.VatAmount.ShouldBe(0.20m);
        result.VatRatePercentage.ShouldBe(6m);
    }

    // ── 21% EatIn ─────────────────────────────────────────────────────────────

    [Fact]
    public void CalculateFromGross_EatIn21Percent_ReturnsCorrectBreakdown()
    {
        var result = TaxCalculator.CalculateFromGross(3.50m, 21m);

        result.GrossAmount.ShouldBe(3.50m);
        result.NetAmount.ShouldBe(2.89m);
        result.VatAmount.ShouldBe(0.61m);
        result.VatRatePercentage.ShouldBe(21m);
    }

    // ── Zero amount ───────────────────────────────────────────────────────────

    [Fact]
    public void CalculateFromGross_ZeroAmount_ReturnsAllZeros()
    {
        var result = TaxCalculator.CalculateFromGross(0m, 6m);

        result.GrossAmount.ShouldBe(0m);
        result.NetAmount.ShouldBe(0m);
        result.VatAmount.ShouldBe(0m);
        result.VatRatePercentage.ShouldBe(6m);
    }

    // ── Zero rate ─────────────────────────────────────────────────────────────

    [Fact]
    public void CalculateFromGross_ZeroRate_NetEqualsGross()
    {
        var result = TaxCalculator.CalculateFromGross(5.00m, 0m);

        result.GrossAmount.ShouldBe(5.00m);
        result.NetAmount.ShouldBe(5.00m);
        result.VatAmount.ShouldBe(0m);
        result.VatRatePercentage.ShouldBe(0m);
    }

    // ── Rounding edge case ────────────────────────────────────────────────────

    [Fact]
    public void CalculateFromGross_RoundingEdgeCase_SumsCorrectly()
    {
        // 1.99 / 1.06 = 1.87735... → net rounds to 1.88, vat = 1.99 - 1.88 = 0.11
        var result = TaxCalculator.CalculateFromGross(1.99m, 6m);

        // Net + VAT must always reconstitute the gross exactly
        (result.NetAmount + result.VatAmount).ShouldBe(result.GrossAmount);
        result.GrossAmount.ShouldBe(1.99m);
    }
}

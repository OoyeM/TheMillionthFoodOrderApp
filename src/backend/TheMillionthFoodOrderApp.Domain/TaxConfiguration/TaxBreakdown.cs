using TheMillionthFoodOrderApp.Domain.Common;

namespace TheMillionthFoodOrderApp.Domain.TaxConfiguration;

public sealed class TaxBreakdown : ValueObject
{
    public decimal NetAmount { get; private set; }
    public decimal VatAmount { get; private set; }
    public decimal GrossAmount { get; private set; }
    public decimal VatRatePercentage { get; private set; }

    public TaxBreakdown(decimal netAmount, decimal vatAmount, decimal grossAmount, decimal vatRatePercentage)
    {
        if (netAmount < 0) throw new ArgumentException("Net amount cannot be negative.", nameof(netAmount));
        if (vatAmount < 0) throw new ArgumentException("VAT amount cannot be negative.", nameof(vatAmount));
        if (grossAmount < 0) throw new ArgumentException("Gross amount cannot be negative.", nameof(grossAmount));
        if (vatRatePercentage < 0 || vatRatePercentage > 100)
            throw new ArgumentOutOfRangeException(nameof(vatRatePercentage), "VAT rate percentage must be between 0 and 100.");

        NetAmount = netAmount;
        VatAmount = vatAmount;
        GrossAmount = grossAmount;
        VatRatePercentage = vatRatePercentage;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return NetAmount;
        yield return VatAmount;
        yield return GrossAmount;
        yield return VatRatePercentage;
    }
}

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

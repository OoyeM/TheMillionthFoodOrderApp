namespace TheMillionthFoodOrderApp.Domain.TaxConfiguration;

public static class TaxCalculator
{
    public static TaxBreakdown CalculateFromGross(decimal grossAmount, decimal vatRatePercentage)
    {
        if (grossAmount < 0)
            throw new ArgumentOutOfRangeException(nameof(grossAmount), "Gross amount cannot be negative.");
        if (vatRatePercentage < 0 || vatRatePercentage > 100)
            throw new ArgumentOutOfRangeException(nameof(vatRatePercentage), "Rate must be between 0 and 100.");

        var net = Math.Round(grossAmount / (1m + vatRatePercentage / 100m), 2, MidpointRounding.AwayFromZero);
        var vat = grossAmount - net;
        return new TaxBreakdown(net, vat, grossAmount, vatRatePercentage);
    }
}

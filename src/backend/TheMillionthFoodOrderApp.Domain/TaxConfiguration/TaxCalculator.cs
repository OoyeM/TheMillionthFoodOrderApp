namespace TheMillionthFoodOrderApp.Domain.TaxConfiguration;

public static class TaxCalculator
{
    public static TaxBreakdown CalculateFromGross(decimal grossAmount, decimal vatRatePercentage)
    {
        var net = Math.Round(grossAmount / (1m + vatRatePercentage / 100m), 2, MidpointRounding.AwayFromZero);
        var vat = grossAmount - net;
        return new TaxBreakdown(net, vat, grossAmount, vatRatePercentage);
    }
}

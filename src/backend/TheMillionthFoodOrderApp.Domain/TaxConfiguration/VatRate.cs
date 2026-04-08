using TheMillionthFoodOrderApp.Domain.Common;

namespace TheMillionthFoodOrderApp.Domain.TaxConfiguration;

public sealed class VatRate : Entity<Guid>
{
    public Guid TaxConfigurationId { get; private set; }
    public ConsumptionMode ConsumptionMode { get; private set; }
    public decimal RatePercentage { get; private set; }

    // Required by EF Core
    private VatRate() { }

    public static VatRate Create(Guid taxConfigurationId, ConsumptionMode mode, decimal ratePercentage)
    {
        if (ratePercentage < 0 || ratePercentage > 100)
            throw new ArgumentOutOfRangeException(nameof(ratePercentage), "Rate percentage must be between 0 and 100.");

        return new VatRate
        {
            Id = Guid.CreateVersion7(),
            TaxConfigurationId = taxConfigurationId,
            ConsumptionMode = mode,
            RatePercentage = ratePercentage,
        };
    }
}

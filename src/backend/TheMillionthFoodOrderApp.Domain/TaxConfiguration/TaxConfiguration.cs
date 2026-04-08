using TheMillionthFoodOrderApp.Domain.Common;

namespace TheMillionthFoodOrderApp.Domain.TaxConfiguration;

public sealed class TaxConfiguration : AggregateRoot<Guid>, IAuditable
{
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    private List<VatRate> _vatRates = [];
    public IReadOnlyCollection<VatRate> VatRates => _vatRates.AsReadOnly();

    // Required by EF Core
    private TaxConfiguration() { }

    public static TaxConfiguration CreateBelgianDefault()
    {
        var now = DateTimeOffset.UtcNow;
        var config = new TaxConfiguration
        {
            Id = Guid.CreateVersion7(),
            CreatedAt = now,
            UpdatedAt = now,
        };

        config._vatRates.Add(VatRate.Create(config.Id, ConsumptionMode.Takeaway, 6m));
        config._vatRates.Add(VatRate.Create(config.Id, ConsumptionMode.EatIn, 21m));

        return config;
    }

    public void UpdateRates(IEnumerable<(ConsumptionMode Mode, decimal RatePercentage)> rates)
    {
        var rateList = rates.ToList();

        var enumValues = Enum.GetValues<ConsumptionMode>();
        foreach (var mode in enumValues)
        {
            var count = rateList.Count(r => r.Mode == mode);
            if (count != 1)
                throw new ArgumentException($"Exactly one rate must be provided for ConsumptionMode '{mode}'.");
        }

        _vatRates.Clear();
        _vatRates.AddRange(rateList.Select(r => VatRate.Create(Id, r.Mode, r.RatePercentage)));
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public decimal GetRateForMode(ConsumptionMode mode)
    {
        var vatRate = _vatRates.FirstOrDefault(r => r.ConsumptionMode == mode);
        if (vatRate is null)
            throw new KeyNotFoundException($"No VAT rate configured for ConsumptionMode '{mode}'.");

        return vatRate.RatePercentage;
    }
}

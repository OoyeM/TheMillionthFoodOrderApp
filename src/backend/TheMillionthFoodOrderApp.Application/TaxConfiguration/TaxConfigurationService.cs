using TheMillionthFoodOrderApp.Domain.Common;
using TheMillionthFoodOrderApp.Domain.TaxConfiguration;

namespace TheMillionthFoodOrderApp.Application.TaxConfiguration;

public sealed class TaxConfigurationService(ITaxConfigurationRepository repository) : ITaxConfigurationService
{
    public async Task<TaxConfigurationResponse?> GetAsync(CancellationToken cancellationToken = default)
    {
        var config = await repository.GetAsync(cancellationToken);
        return config is null ? null : MapToResponse(config);
    }

    public async Task<TaxConfigurationResponse> UpsertAsync(
        UpdateTaxConfigurationRequest request,
        CancellationToken cancellationToken = default)
    {
        var existing = await repository.GetAsync(cancellationToken);

        var rates = request.VatRates
            .Select(r => (
                ConsumptionMode: ParseConsumptionMode(r.ConsumptionMode),
                RatePercentage: r.RatePercentage))
            .ToList();

        if (existing is null)
        {
            var config = Domain.TaxConfiguration.TaxConfiguration.Create();
            config.UpdateRates(rates);
            try
            {
                await repository.AddAsync(config, cancellationToken);
                await repository.SaveChangesAsync(cancellationToken);
                return MapToResponse(config);
            }
            catch (InvalidOperationException)
            {
                // Race condition: a concurrent request created the config between our
                // GetAsync check and AddAsync. Fall through to update the existing record.
                existing = await repository.GetAsync(cancellationToken);
            }
        }

        var updated = await repository.ReplaceRatesAsync(existing!.Id, c => c.UpdateRates(rates), cancellationToken);
        return MapToResponse(updated);
    }

    public async Task<TaxBreakdownDto> CalculateAsync(
        CalculateTaxRequest request,
        CancellationToken cancellationToken = default)
    {
        var config = await repository.GetAsync(cancellationToken);
        if (config is null)
            throw new KeyNotFoundException("No tax configuration has been set up.");

        var mode = ParseConsumptionMode(request.ConsumptionMode);
        var rate = config.GetRateForMode(mode);
        var breakdown = TaxCalculator.CalculateFromGross(request.GrossAmount, rate);

        return new TaxBreakdownDto(
            breakdown.NetAmount,
            breakdown.VatAmount,
            breakdown.GrossAmount,
            breakdown.VatRatePercentage);
    }

    private static TaxConfigurationResponse MapToResponse(Domain.TaxConfiguration.TaxConfiguration config) =>
        new(
            config.Id,
            config.VatRates
                .Select(r => new VatRateDto(r.ConsumptionMode.ToString(), r.RatePercentage))
                .ToList(),
            config.CreatedAt,
            config.UpdatedAt);

    private static ConsumptionMode ParseConsumptionMode(string value)
    {
        if (!Enum.TryParse<ConsumptionMode>(value, out var mode) || !Enum.IsDefined(mode))
            throw new ArgumentException(
                $"Invalid consumption mode: '{value}'. Valid values: {string.Join(", ", Enum.GetNames<ConsumptionMode>())}.");
        return mode;
    }
}

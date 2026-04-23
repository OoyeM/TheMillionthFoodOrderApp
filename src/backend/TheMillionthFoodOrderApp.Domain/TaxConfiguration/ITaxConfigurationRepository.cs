namespace TheMillionthFoodOrderApp.Domain.TaxConfiguration;

public interface ITaxConfigurationRepository
{
    Task<TaxConfiguration?> GetAsync(CancellationToken cancellationToken = default);
    Task AddAsync(TaxConfiguration configuration, CancellationToken cancellationToken = default);
    Task RemoveAsync(TaxConfiguration configuration, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces all VatRates on the given config within a transaction.
    /// Bypasses EF change tracker to avoid snapshot / cascade conflicts.
    /// Returns the updated config.
    /// </summary>
    Task<TaxConfiguration> ReplaceRatesAsync(Guid configId, Action<TaxConfiguration> mutate, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

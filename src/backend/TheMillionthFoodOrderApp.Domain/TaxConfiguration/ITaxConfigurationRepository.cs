namespace TheMillionthFoodOrderApp.Domain.TaxConfiguration;

public interface ITaxConfigurationRepository
{
    Task<TaxConfiguration?> GetAsync(CancellationToken cancellationToken = default);
    Task AddAsync(TaxConfiguration configuration, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

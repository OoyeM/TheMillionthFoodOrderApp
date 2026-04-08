using Microsoft.EntityFrameworkCore;
using TheMillionthFoodOrderApp.Domain.TaxConfiguration;
using TheMillionthFoodOrderApp.Infrastructure.Persistence;

namespace TheMillionthFoodOrderApp.Infrastructure.TaxConfiguration;

public sealed class TaxConfigurationRepository(BrandDbContext dbContext) : ITaxConfigurationRepository
{
    public async Task<Domain.TaxConfiguration.TaxConfiguration?> GetAsync(CancellationToken cancellationToken = default)
        => await dbContext.TaxConfigurations
            .Include(c => c.VatRates)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task AddAsync(Domain.TaxConfiguration.TaxConfiguration configuration, CancellationToken cancellationToken = default)
        => await dbContext.TaxConfigurations.AddAsync(configuration, cancellationToken);

    public Task RemoveAsync(Domain.TaxConfiguration.TaxConfiguration configuration, CancellationToken cancellationToken = default)
    {
        dbContext.TaxConfigurations.Remove(configuration);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => await dbContext.SaveChangesAsync(cancellationToken);
}

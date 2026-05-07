using Microsoft.EntityFrameworkCore;
using TheMillionthFoodOrderApp.Domain.TaxConfiguration;
using TheMillionthFoodOrderApp.Infrastructure.Persistence;
using Wolverine;

namespace TheMillionthFoodOrderApp.Infrastructure.TaxConfiguration;

public sealed class TaxConfigurationRepository(BrandDbContext dbContext, IMessageBus messageBus) : ITaxConfigurationRepository
{
    public async Task<Domain.TaxConfiguration.TaxConfiguration?> GetAsync(CancellationToken cancellationToken = default)
        => await dbContext.TaxConfigurations
            .Include(c => c.VatRates)
            .OrderByDescending(c => c.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task AddAsync(Domain.TaxConfiguration.TaxConfiguration configuration, CancellationToken cancellationToken = default)
    {
        var exists = await dbContext.TaxConfigurations.AnyAsync(cancellationToken);
        if (exists)
            throw new InvalidOperationException("A TaxConfiguration already exists for this brand. Use UpdateRates instead.");

        await dbContext.TaxConfigurations.AddAsync(configuration, cancellationToken);
    }

    public Task RemoveAsync(Domain.TaxConfiguration.TaxConfiguration configuration, CancellationToken cancellationToken = default)
    {
        dbContext.TaxConfigurations.Remove(configuration);
        return Task.CompletedTask;
    }

    public async Task<Domain.TaxConfiguration.TaxConfiguration> ReplaceRatesAsync(
        Guid configId,
        Action<Domain.TaxConfiguration.TaxConfiguration> mutate,
        CancellationToken cancellationToken = default)
    {
        Domain.TaxConfiguration.TaxConfiguration config = null!;

        await dbContext.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

            await dbContext.VatRates
                .Where(v => v.TaxConfigurationId == configId)
                .ExecuteDeleteAsync(cancellationToken);

            // Clear tracker so FirstAsync returns a fresh instance without old VatRate snapshots.
            dbContext.ChangeTracker.Clear();

            config = await dbContext.TaxConfigurations
                .FirstAsync(c => c.Id == configId, cancellationToken);

            mutate(config);

            await dbContext.VatRates.AddRangeAsync(config.VatRates, cancellationToken);

            var events = DomainEventDispatcher.CollectAndClear(dbContext);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            await DomainEventDispatcher.PublishAsync(events, messageBus);
        });

        return config;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var events = DomainEventDispatcher.CollectAndClear(dbContext);
        await dbContext.SaveChangesAsync(cancellationToken);
        await DomainEventDispatcher.PublishAsync(events, messageBus);
    }
}

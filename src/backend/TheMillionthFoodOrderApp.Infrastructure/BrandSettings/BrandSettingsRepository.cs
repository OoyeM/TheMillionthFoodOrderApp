using Microsoft.EntityFrameworkCore;
using TheMillionthFoodOrderApp.Domain.BrandSettings;
using TheMillionthFoodOrderApp.Infrastructure.Persistence;

namespace TheMillionthFoodOrderApp.Infrastructure.BrandSettings;

public sealed class BrandSettingsRepository(BrandDbContext dbContext) : IBrandSettingsRepository
{
    public async Task<Domain.BrandSettings.BrandSettings?> GetAsync(
        CancellationToken cancellationToken = default)
        => await dbContext.BrandSettings
            .FirstOrDefaultAsync(cancellationToken);

    public async Task AddAsync(
        Domain.BrandSettings.BrandSettings settings,
        CancellationToken cancellationToken = default)
        => await dbContext.BrandSettings.AddAsync(settings, cancellationToken);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => await dbContext.SaveChangesAsync(cancellationToken);
}

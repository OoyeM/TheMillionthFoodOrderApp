using Microsoft.Extensions.Logging;

namespace TheMillionthFoodOrderApp.Infrastructure.Persistence.Seeding;

/// <summary>
/// Seeds a brand-specific database with development data.
/// Currently a stub — populate as brand entities (shops, products, menus) are introduced.
/// </summary>
#pragma warning disable CS9113 // dbContext will be used when brand entities are added
public sealed class BrandDbSeeder(
    BrandDbContext dbContext,
    ILogger<BrandDbSeeder> logger)
#pragma warning restore CS9113
{
    public Task SeedAsync(CancellationToken cancellationToken = default)
    {
        // Suppress unused variable until brand entities are added
        _ = dbContext;

        // TODO: seed shops, categories, products, etc. once those entities exist
        logger.LogDebug("BrandDbSeeder: no seed data defined yet.");
        return Task.CompletedTask;
    }
}

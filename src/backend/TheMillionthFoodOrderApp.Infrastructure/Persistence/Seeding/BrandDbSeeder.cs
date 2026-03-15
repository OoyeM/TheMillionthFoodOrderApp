using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TheMillionthFoodOrderApp.Application.Multitenancy;
using TheMillionthFoodOrderApp.Domain.BrandSettings;

namespace TheMillionthFoodOrderApp.Infrastructure.Persistence.Seeding;

/// <summary>
/// Seeds a brand-specific database with development data.
/// Must be called with a brand context already set (i.e. after
/// <see cref="IBrandContextAccessor.BrandSlug"/> is populated).
/// </summary>
public sealed class BrandDbSeeder(
    BrandDbContextFactory dbContextFactory,
    ILogger<BrandDbSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await using var context = dbContextFactory.CreateDbContext();

        await SeedBrandSettingsAsync(context, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedBrandSettingsAsync(
        BrandDbContext context,
        CancellationToken cancellationToken)
    {
        var exists = await context.BrandSettings.AnyAsync(cancellationToken);

        if (exists)
        {
            logger.LogDebug("Seed: BrandSettings already exists — skipping.");
            return;
        }

        var settings = TheMillionthFoodOrderApp.Domain.BrandSettings.BrandSettings.CreateDefault();
        await context.BrandSettings.AddAsync(settings, cancellationToken);

        logger.LogInformation(
            "Seed: Created default BrandSettings (language: {Language}, timezone: {Timezone}, currency: {Currency}).",
            settings.DefaultLanguage,
            settings.Timezone,
            settings.Currency);
    }
}

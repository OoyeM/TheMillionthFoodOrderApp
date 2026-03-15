using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TheMillionthFoodOrderApp.Domain.Brands;

namespace TheMillionthFoodOrderApp.Infrastructure.Persistence.Seeding;

/// <summary>
/// Seeds the platform database with essential development data.
/// Only runs in the Development environment — wired in Api's Program.cs.
///
/// Seeds:
/// - "Frietjes?" brand — the first customer, a Belgian fries chain.
/// </summary>
public sealed class PlatformDbSeeder(
    PlatformDbContext dbContext,
    ILogger<PlatformDbSeeder> logger)
{
    private const string FrietjesSlug = "frietjes";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedFrietjesBrandAsync(cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedFrietjesBrandAsync(CancellationToken cancellationToken)
    {
        var exists = await dbContext.Brands
            .AnyAsync(b => b.Slug == FrietjesSlug, cancellationToken);

        if (exists)
        {
            logger.LogDebug("Seed: 'Frietjes?' brand already exists — skipping.");
            return;
        }

        var brand = Brand.Create(
            name: "Frietjes?",
            slug: FrietjesSlug,
            contactEmail: "hello@frietjes.be",
            contactPhone: "+32 2 000 00 00");

        await dbContext.Brands.AddAsync(brand, cancellationToken);

        logger.LogInformation("Seed: Created 'Frietjes?' brand (slug: {Slug}).", FrietjesSlug);
    }
}

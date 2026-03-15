using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TheMillionthFoodOrderApp.Application.Multitenancy;
using TheMillionthFoodOrderApp.Domain.BrandSettings;
using TheMillionthFoodOrderApp.Domain.Shops;

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
        await SeedShopsAsync(context, cancellationToken);
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

    private async Task SeedShopsAsync(
        BrandDbContext context,
        CancellationToken cancellationToken)
    {
        var seedSlugs = new[] { "bruxelles-centre", "antwerpen-centraal", "gent-korenmarkt" };

        foreach (var slug in seedSlugs)
        {
            var exists = await context.Shops.AnyAsync(s => s.Slug == slug, cancellationToken);
            if (exists)
            {
                logger.LogDebug("Seed: Shop '{Slug}' already exists — skipping.", slug);
                continue;
            }

            var shop = slug switch
            {
                "bruxelles-centre" => Shop.Create(
                    name: "Bruxelles Centre",
                    slug: "bruxelles-centre",
                    address: new Address("Rue du Marché aux Herbes", "83", "Bruxelles", "1000"),
                    contactEmail: "bruxelles@frietjes.be",
                    contactPhone: "+32 2 000 00 01"),

                "antwerpen-centraal" => Shop.Create(
                    name: "Antwerpen Centraal",
                    slug: "antwerpen-centraal",
                    address: new Address("Koningin Astridplein", "27", "Antwerpen", "2018"),
                    contactEmail: "antwerpen@frietjes.be",
                    contactPhone: "+32 3 000 00 02"),

                "gent-korenmarkt" => Shop.Create(
                    name: "Gent Korenmarkt",
                    slug: "gent-korenmarkt",
                    address: new Address("Korenmarkt", "1", "Gent", "9000"),
                    contactEmail: "gent@frietjes.be",
                    contactPhone: "+32 9 000 00 03"),

                _ => throw new InvalidOperationException($"Unknown seed slug: {slug}")
            };

            await context.Shops.AddAsync(shop, cancellationToken);
            logger.LogInformation("Seed: Created shop '{Name}' (slug: {Slug}).", shop.Name, shop.Slug);
        }
    }
}

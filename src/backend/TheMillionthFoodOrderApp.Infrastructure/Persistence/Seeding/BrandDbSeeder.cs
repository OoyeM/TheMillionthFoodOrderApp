using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TheMillionthFoodOrderApp.Domain.Shops;

namespace TheMillionthFoodOrderApp.Infrastructure.Persistence.Seeding;

/// <summary>
/// Seeds a brand-specific database with development data.
/// Called once per brand database after migrations are applied.
/// </summary>
public sealed class BrandDbSeeder(
    BrandDbContextFactory dbContextFactory,
    ILogger<BrandDbSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedFrietjesShopsAsync(cancellationToken);
    }

    private async Task SeedFrietjesShopsAsync(CancellationToken cancellationToken)
    {
        await using var dbContext = dbContextFactory.CreateDbContext();

        var seedSlugs = new[] { "bruxelles-centre", "antwerpen-centraal", "gent-korenmarkt" };

        foreach (var slug in seedSlugs)
        {
            var exists = await dbContext.Shops.AnyAsync(s => s.Slug == slug, cancellationToken);
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

            await dbContext.Shops.AddAsync(shop, cancellationToken);
            logger.LogInformation("Seed: Created shop '{Name}' (slug: {Slug}).", shop.Name, shop.Slug);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}

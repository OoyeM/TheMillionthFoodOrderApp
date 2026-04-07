using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TheMillionthFoodOrderApp.Application.Multitenancy;
using TheMillionthFoodOrderApp.Domain.BrandSettings;
using TheMillionthFoodOrderApp.Domain.MenuCategories;
using TheMillionthFoodOrderApp.Domain.ModifierGroups;
using TheMillionthFoodOrderApp.Domain.Products;
using TheMillionthFoodOrderApp.Domain.Shops;
using BrandSettingsDomain = TheMillionthFoodOrderApp.Domain.BrandSettings.BrandSettings;

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
        await SeedMenuCategoriesAsync(context, cancellationToken);
        await SeedProductsAsync(context, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await SeedModifierGroupsAsync(context, cancellationToken);
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

        // Seed Frietjes? brand settings with Belgian defaults and sample theming.
        // The dark yellow/golden palette reflects a typical Belgian fries brand identity.
        var settings = BrandSettingsDomain.Create(
            defaultLanguage: "nl-BE",
            timezone: "Europe/Brussels",
            currency: "EUR");

        settings.UpdateTheming(
            colors: new BrandColors(
                primary: "#1a1a1a",    // Near-black — header / primary actions
                secondary: "#f59e0b",  // Amber — brand accent inspired by golden fries
                accent: "#d97706"),    // Darker amber — hover / CTA accent
            typography: new BrandTypography(
                headingFontFamily: "Poppins",
                bodyFontFamily: "Inter"),
            customDomain: null);       // DNS routing is US-FP-067

        await context.BrandSettings.AddAsync(settings, cancellationToken);

        logger.LogInformation(
            "Seed: Created BrandSettings for Frietjes? (language: {Language}, timezone: {Timezone}, currency: {Currency}).",
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

    private async Task SeedMenuCategoriesAsync(
        BrandDbContext context,
        CancellationToken cancellationToken)
    {
        var exists = await context.MenuCategories.AnyAsync(cancellationToken);
        if (exists)
        {
            logger.LogDebug("Seed: Menu categories already exist — skipping.");
            return;
        }

        var categories = new[]
        {
            MenuCategory.Create(
                imageUrl: null,
                sortOrder: 0,
                translations: new[] { ("nl", "Frietjes", (string?)"Verse frieten in verschillende maten"), ("fr", "Frites", (string?)"Frites fraîches en différentes tailles"), ("de", "Pommes", (string?)"Frische Pommes in verschiedenen Größen") }),

            MenuCategory.Create(
                imageUrl: null,
                sortOrder: 1,
                translations: new[] { ("nl", "Sauzen", (string?)"Huisgemaakte en klassieke sauzen"), ("fr", "Sauces", (string?)"Sauces maison et classiques"), ("de", "Soßen", (string?)"Hausgemachte und klassische Soßen") }),

            MenuCategory.Create(
                imageUrl: null,
                sortOrder: 2,
                translations: new[] { ("nl", "Snacks", (string?)"Belgische snacks en lekkernijen"), ("fr", "Snacks", (string?)"Snacks et délices belges"), ("de", "Snacks", (string?)"Belgische Snacks und Leckereien") }),

            MenuCategory.Create(
                imageUrl: null,
                sortOrder: 3,
                translations: new[] { ("nl", "Burgers", (string?)"Iconische Belgische burgers"), ("fr", "Burgers", (string?)"Burgers belges iconiques"), ("de", "Burger", (string?)"Ikonische belgische Burger") }),
        };

        foreach (var category in categories)
        {
            await context.MenuCategories.AddAsync(category, cancellationToken);
            var name = category.Translations.First().Name;
            logger.LogInformation("Seed: Created menu category '{Name}'.", name);
        }
    }

    private async Task SeedProductsAsync(
        BrandDbContext context,
        CancellationToken cancellationToken)
    {
        var exists = await context.Products.AnyAsync(cancellationToken);
        if (exists)
        {
            logger.LogDebug("Seed: Products already exist — skipping.");
            return;
        }

        var products = new[]
        {
            Product.Create(
                new Money(3.50m, "EUR"), null,
                new[] { ("nl", "Kleine Friet", (string?)"Knapperig gebakken frieten, klein portie"), ("fr", "Petites Frites", (string?)"Frites croustillantes, petite portion"), ("de", "Kleine Pommes", (string?)"Knusprige Pommes, kleine Portion") }),
            Product.Create(
                new Money(5.00m, "EUR"), null,
                new[] { ("nl", "Grote Friet", (string?)"Knapperig gebakken frieten, groot portie"), ("fr", "Grandes Frites", (string?)"Frites croustillantes, grande portion"), ("de", "Große Pommes", (string?)"Knusprige Pommes, große Portion") }),
            Product.Create(
                new Money(1.50m, "EUR"), null,
                new[] { ("nl", "Stoofvleessaus", (string?)"Klassieke Vlaamse stoofvleessaus"), ("fr", "Sauce Carbonade", (string?)"Sauce carbonade flamande classique"), ("de", "Schmorfleischsoße", (string?)"Klassische flämische Schmorfleischsoße") }),
            Product.Create(
                new Money(2.50m, "EUR"), null,
                new[] { ("nl", "Frikandel", (string?)"Gekruide gehaktstaaf"), ("fr", "Fricandelle", (string?)"Rouleau de viande épicé"), ("de", "Frikandel", (string?)"Gewürzte Fleischrolle") }),
            Product.Create(
                new Money(4.50m, "EUR"), null,
                new[] { ("nl", "Bicky Burger", (string?)"Iconische Belgische burger met bickysaus"), ("fr", "Bicky Burger", (string?)"Burger belge iconique avec sauce bicky"), ("de", "Bicky Burger", (string?)"Ikonischer belgischer Burger mit Bickysoße") }),
        };

        foreach (var product in products)
        {
            await context.Products.AddAsync(product, cancellationToken);
            var name = product.Translations.First().Name;
            logger.LogInformation("Seed: Created product '{Name}'.", name);
        }
    }

    private async Task SeedModifierGroupsAsync(
        BrandDbContext context,
        CancellationToken cancellationToken)
    {
        var exists = await context.ModifierGroups.AnyAsync(cancellationToken);
        if (exists)
        {
            logger.LogDebug("Seed: Modifier groups already exist — skipping.");
            return;
        }

        // Group 1: Size — Klein (+0), Medium (+1), Groot (+2)
        var sizeGroup = ModifierGroup.Create(
            translations: new[]
            {
                ("nl", "Maat"),
                ("fr", "Taille"),
                ("de", "Groesse"),
            },
            modifiers: new[]
            {
                (0m, 0, (IEnumerable<(string, string)>)new[] { ("nl", "Klein"), ("fr", "Petit"), ("de", "Klein") }),
                (1m, 1, (IEnumerable<(string, string)>)new[] { ("nl", "Medium"), ("fr", "Moyen"), ("de", "Mittel") }),
                (2m, 2, (IEnumerable<(string, string)>)new[] { ("nl", "Groot"), ("fr", "Grand"), ("de", "Gross") }),
            });

        await context.ModifierGroups.AddAsync(sizeGroup, cancellationToken);
        logger.LogInformation("Seed: Created modifier group 'Maat/Taille/Groesse' with {Count} modifiers.", sizeGroup.Modifiers.Count);

        // Group 2: Sauces — Mayonaise (+0), Stoofvleessaus (+0.50), Speciaal (+0.50)
        var saucesGroup = ModifierGroup.Create(
            translations: new[]
            {
                ("nl", "Sauzen"),
                ("fr", "Sauces"),
                ("de", "Sossen"),
            },
            modifiers: new[]
            {
                (0m, 0, (IEnumerable<(string, string)>)new[] { ("nl", "Mayonaise"), ("fr", "Mayonnaise"), ("de", "Mayonnaise") }),
                (0.50m, 1, (IEnumerable<(string, string)>)new[] { ("nl", "Stoofvleessaus"), ("fr", "Sauce Carbonade"), ("de", "Fleischsauce") }),
                (0.50m, 2, (IEnumerable<(string, string)>)new[] { ("nl", "Speciaal"), ("fr", "Spéciale"), ("de", "Spezial") }),
            });

        await context.ModifierGroups.AddAsync(saucesGroup, cancellationToken);
        logger.LogInformation("Seed: Created modifier group 'Sauzen/Sauces/Sossen' with {Count} modifiers.", saucesGroup.Modifiers.Count);

        await context.SaveChangesAsync(cancellationToken);

        // Link the Size group to the first seeded product (Kleine Friet)
        var firstProduct = await context.Products
            .OrderBy(p => p.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (firstProduct is not null)
        {
            var assignment = ProductModifierGroup.Create(firstProduct.Id, sizeGroup.Id, sortOrder: 0);
            await context.ProductModifierGroups.AddAsync(assignment, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Seed: Linked modifier group 'Maat/Taille/Groesse' to product '{ProductId}'.",
                firstProduct.Id);
        }
    }
}

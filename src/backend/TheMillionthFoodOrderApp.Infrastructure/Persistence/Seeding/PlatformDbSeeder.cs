using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TheMillionthFoodOrderApp.Domain.Brands;
using TheMillionthFoodOrderApp.Domain.Identity;

namespace TheMillionthFoodOrderApp.Infrastructure.Persistence.Seeding;

/// <summary>
/// Seeds the platform database with essential development data.
/// Only runs in the Development environment — wired in Api's Program.cs.
///
/// Seeds:
/// - "Frietjes?" brand — the first customer, a Belgian fries chain.
/// - Test users matching Keycloak dev realm personas.
/// </summary>
public sealed class PlatformDbSeeder(
    PlatformDbContext dbContext,
    ILogger<PlatformDbSeeder> logger)
{
    private const string FrietjesSlug = "frietjes";

    // Deterministic IDs matching Keycloak dev-realm.json user IDs.
    // These ensure the Keycloak 'sub' claim maps to the correct PlatformUser.
    private const string KeycloakPlatformAdminSub = "00000000-0000-0000-0000-000000000001";
    private const string KeycloakBrandAdminSub    = "00000000-0000-0000-0000-000000000002";
    private const string KeycloakCounterStaffSub  = "00000000-0000-0000-0000-000000000003";
    private const string KeycloakCustomerSub      = "00000000-0000-0000-0000-000000000004";

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        await SeedFrietjesBrandAsync(cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        await SeedTestUsersAsync(cancellationToken);
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

    private async Task SeedTestUsersAsync(CancellationToken cancellationToken)
    {
        // Platform Admin
        var platformAdmin = await SeedUserAsync(
            KeycloakPlatformAdminSub,
            "platform-admin@mock.local",
            "Platform Admin",
            isPlatformAdmin: true,
            cancellationToken);

        // Brand Admin for Frietjes
        var brandAdmin = await SeedUserAsync(
            KeycloakBrandAdminSub,
            "brand-admin@frietjes.mock.local",
            "Brand Admin (Frietjes)",
            isPlatformAdmin: false,
            cancellationToken);

        if (brandAdmin is not null)
        {
            var frietjes = await dbContext.Brands
                .FirstOrDefaultAsync(b => b.Slug == FrietjesSlug, cancellationToken);

            if (frietjes is not null)
            {
                var hasRole = await dbContext.BrandUserRoles
                    .AnyAsync(r => r.PlatformUserId == brandAdmin.Id
                                   && r.BrandId == frietjes.Id
                                   && r.Role == StaffRole.BrandAdmin, cancellationToken);

                if (!hasRole)
                {
                    var role = BrandUserRole.Create(brandAdmin.Id, frietjes.Id, null, StaffRole.BrandAdmin);
                    await dbContext.BrandUserRoles.AddAsync(role, cancellationToken);
                    logger.LogInformation("Seed: Assigned BrandAdmin role to {Email} for Frietjes.", brandAdmin.Email);
                }
            }
        }

        // Counter Staff for Frietjes — needs a shop, skip role assignment for now
        await SeedUserAsync(
            KeycloakCounterStaffSub,
            "counter-staff@frietjes.mock.local",
            "Counter Staff (Frietjes)",
            isPlatformAdmin: false,
            cancellationToken);

        // Customer
        await SeedUserAsync(
            KeycloakCustomerSub,
            "customer@mock.local",
            "Customer",
            isPlatformAdmin: false,
            cancellationToken);
    }

    /// <summary>
    /// Creates a PlatformUser if one with the given external identity ID does not already exist.
    /// Returns the user (existing or newly created), or null if a creation was skipped.
    /// </summary>
    private async Task<PlatformUser?> SeedUserAsync(
        string externalIdentityId,
        string email,
        string displayName,
        bool isPlatformAdmin,
        CancellationToken cancellationToken)
    {
        var existing = await dbContext.PlatformUsers
            .FirstOrDefaultAsync(u => u.ExternalIdentityId == externalIdentityId, cancellationToken);

        if (existing is not null)
        {
            logger.LogDebug("Seed: User {Email} already exists — skipping.", email);
            return existing;
        }

        var user = PlatformUser.Create(externalIdentityId, email, displayName, isPlatformAdmin);
        await dbContext.PlatformUsers.AddAsync(user, cancellationToken);
        logger.LogInformation("Seed: Created user {Email} (platformAdmin={IsPlatformAdmin}).", email, isPlatformAdmin);

        return user;
    }
}

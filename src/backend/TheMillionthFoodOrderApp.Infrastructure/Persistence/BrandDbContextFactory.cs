using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using TheMillionthFoodOrderApp.Application.Multitenancy;
using TheMillionthFoodOrderApp.Infrastructure.Persistence.Interceptors;

namespace TheMillionthFoodOrderApp.Infrastructure.Persistence;

/// <summary>
/// Creates a <see cref="BrandDbContext"/> scoped to the brand identified by
/// <see cref="IBrandContextAccessor.BrandSlug"/>.
/// The brand database connection string is derived from the platform connection string
/// by swapping the <c>Initial Catalog</c> (database name) to <c>brand_{slug}</c>.
/// Using <see cref="SqlConnectionStringBuilder"/> ensures correct parsing regardless of
/// connection string format — never use regex or string replacement here.
/// </summary>
public sealed class BrandDbContextFactory(
    IBrandContextAccessor brandContextAccessor,
    IConfiguration configuration,
    AuditSaveChangesInterceptor auditInterceptor)
{
    /// <summary>
    /// Creates a brand-scoped <see cref="BrandDbContext"/> for the current request's brand.
    /// The <see cref="AuditSaveChangesInterceptor"/> is added so audit fields are auto-populated.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no brand slug is set in the current context or the platform connection
    /// string is not configured.
    /// </exception>
    /// <summary>
    /// Creates a brand-scoped <see cref="BrandDbContext"/> for the current request's brand.
    /// When no brand slug is set (e.g. during startup when FastEndpoints builds the route map),
    /// returns a placeholder context that will throw on actual database access.
    /// </summary>
    public BrandDbContext CreateDbContext()
    {
        var slug = brandContextAccessor.BrandSlug;

        if (slug is null)
        {
            // Startup resolution — FastEndpoints instantiates endpoints to discover routes.
            // Return a context that satisfies DI but will fail loudly if actually queried.
            var placeholder = new DbContextOptionsBuilder<BrandDbContext>()
                .UseSqlServer("Server=placeholder;Database=placeholder")
                .Options;
            return new BrandDbContext(placeholder);
        }

        var platformConnectionString = configuration.GetConnectionString("platform")
            ?? throw new InvalidOperationException(
                "Platform connection string 'platform' is not configured. " +
                "Verify Aspire has injected the connection string.");

        var brandConnectionString = BrandConnectionStringHelper.DeriveBrandConnectionString(
            platformConnectionString, slug);

        var options = new DbContextOptionsBuilder<BrandDbContext>()
            .UseSqlServer(brandConnectionString, sqlOptions => sqlOptions.EnableRetryOnFailure())
            .AddInterceptors(auditInterceptor)
            .Options;

        return new BrandDbContext(options);
    }
}

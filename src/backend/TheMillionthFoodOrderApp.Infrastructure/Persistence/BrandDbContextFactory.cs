using Microsoft.Data.SqlClient;
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
    public BrandDbContext CreateDbContext()
    {
        var slug = brandContextAccessor.BrandSlug
            ?? throw new InvalidOperationException(
                "No brand context is active for this request. " +
                "Ensure the BrandContextMiddleware is registered and the request includes a brand identifier.");

        var platformConnectionString = configuration.GetConnectionString("platform")
            ?? throw new InvalidOperationException(
                "Platform connection string 'platform' is not configured. " +
                "Verify Aspire has injected the connection string.");

        var brandConnectionString = DeriveBrandConnectionString(platformConnectionString, slug);

        var options = new DbContextOptionsBuilder<BrandDbContext>()
            .UseSqlServer(brandConnectionString)
            .AddInterceptors(auditInterceptor)
            .Options;

        return new BrandDbContext(options);
    }

    /// <summary>
    /// Derives a brand-specific connection string by replacing the database name
    /// in the platform connection string with <c>brand_{slug}</c>.
    /// </summary>
    private static string DeriveBrandConnectionString(string platformConnectionString, string brandSlug)
    {
        var builder = new SqlConnectionStringBuilder(platformConnectionString)
        {
            InitialCatalog = $"brand_{brandSlug}"
        };

        return builder.ConnectionString;
    }
}

using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace TheMillionthFoodOrderApp.Infrastructure.Persistence;

/// <summary>
/// Health check that verifies connectivity to every active brand database.
/// Queries the platform database for active brands, then attempts to open a connection
/// to each brand database.
///
/// Returns:
/// - <see cref="HealthCheckResult.Healthy"/> if all brand databases are reachable.
/// - <see cref="HealthCheckResult.Degraded"/> if some (but not all) are reachable.
/// - <see cref="HealthCheckResult.Unhealthy"/> if all brand databases are unreachable,
///   or the platform database itself cannot be queried.
///
/// Connectivity-only check — migration verification is deliberately excluded because
/// checking <c>__EFMigrationsHistory</c> for every brand on every health probe is too slow.
/// </summary>
public sealed class BrandDatabaseHealthCheck(
    PlatformDbContext platformDbContext,
    IConfiguration configuration,
    ILogger<BrandDatabaseHealthCheck> logger) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        string platformConnectionString;
        List<string> activeSlugs;

        try
        {
            platformConnectionString = configuration.GetConnectionString("platform")
                ?? throw new InvalidOperationException(
                    "Platform connection string 'platform' is not configured.");

            activeSlugs = await platformDbContext.Brands
                .Where(b => b.IsActive)
                .Select(b => b.Slug)
                .AsNoTracking()
                .ToListAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Brand database health check failed: cannot query platform database.");
            return HealthCheckResult.Unhealthy(
                "Cannot query platform database to retrieve active brands.", ex);
        }

        if (activeSlugs.Count == 0)
        {
            return HealthCheckResult.Healthy("No active brands found — nothing to check.");
        }

        var failedBrands = new List<string>();

        foreach (var slug in activeSlugs)
        {
            try
            {
                var brandConnectionString = BrandConnectionStringHelper.DeriveBrandConnectionString(
                    platformConnectionString, slug);

                await using var connection = new SqlConnection(brandConnectionString);
                await connection.OpenAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "Brand database health check: cannot connect to 'brand_{BrandSlug}' — {Message}",
                    slug, ex.Message);
                failedBrands.Add(slug);
            }
        }

        if (failedBrands.Count == 0)
        {
            return HealthCheckResult.Healthy(
                $"All {activeSlugs.Count} brand database(s) are reachable.");
        }

        var failedList = string.Join(", ", failedBrands);

        if (failedBrands.Count == activeSlugs.Count)
        {
            return HealthCheckResult.Unhealthy(
                $"All {activeSlugs.Count} brand database(s) are unreachable: {failedList}");
        }

        return HealthCheckResult.Degraded(
            $"{failedBrands.Count} of {activeSlugs.Count} brand database(s) are unreachable: {failedList}");
    }
}

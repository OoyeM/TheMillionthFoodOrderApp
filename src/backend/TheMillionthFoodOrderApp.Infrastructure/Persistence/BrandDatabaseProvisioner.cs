using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using TheMillionthFoodOrderApp.Domain.Brands;

namespace TheMillionthFoodOrderApp.Infrastructure.Persistence;

/// <summary>
/// Wolverine message handler that provisions a brand database when a new brand is created.
/// Triggered by <see cref="BrandCreatedEvent"/> — runs asynchronously after the brand is
/// persisted in the platform database, so the request is not blocked by database creation.
///
/// Responsibilities:
/// 1. Derive the brand connection string via <see cref="BrandConnectionStringHelper"/>.
/// 2. Create the brand database if it does not already exist.
/// 3. Apply all pending EF Core migrations for <see cref="BrandDbContext"/>.
/// 4. Verify the database and migrations were applied successfully.
/// </summary>
public sealed class BrandDatabaseProvisioner(
    IConfiguration configuration,
    ILogger<BrandDatabaseProvisioner> logger)
{
    // SQL Server transient error numbers — these are safe to retry.
    // Reference: https://learn.microsoft.com/en-us/azure/azure-sql/database/troubleshoot-common-errors-issues
    private static readonly HashSet<int> TransientErrorNumbers =
    [
        40143, // The connection could not be initialized
        40197, // The service has encountered an error processing your request
        40501, // The service is currently busy
        40613, // Database is currently unavailable
        49918, // Cannot process request — not enough resources
        49919, // Cannot process create or update request
        49920, // Service is busy
        4060,  // Cannot open database requested by the login
    ];

    /// <summary>
    /// Handles <see cref="BrandCreatedEvent"/> — Wolverine discovers this method by convention.
    /// </summary>
    public async Task HandleAsync(BrandCreatedEvent @event, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Provisioning brand database for brand '{BrandSlug}' (id: {BrandId})",
            @event.Slug,
            @event.BrandId);

        var platformConnectionString = configuration.GetConnectionString("platform")
            ?? throw new InvalidOperationException(
                "Platform connection string 'platform' is not configured.");

        var brandConnectionString = BrandConnectionStringHelper.DeriveBrandConnectionString(
            platformConnectionString, @event.Slug);

        await EnsureDatabaseExistsAsync(brandConnectionString, @event.Slug, cancellationToken);

        // SQL Server CREATE DATABASE is internally async — a brief pause ensures the database
        // is fully online before we attempt to connect and run migrations.
        await Task.Delay(500, cancellationToken);

        await ApplyMigrationsAsync(brandConnectionString, @event.Slug, cancellationToken);
        await VerifyDatabaseProvisionedAsync(platformConnectionString, brandConnectionString, @event.Slug, cancellationToken);

        logger.LogInformation(
            "Brand database 'brand_{BrandSlug}' provisioned and verified successfully.",
            @event.Slug);
    }

    private async Task EnsureDatabaseExistsAsync(
        string brandConnectionString,
        string brandSlug,
        CancellationToken cancellationToken)
    {
        var masterConnectionString = BrandConnectionStringHelper.DeriveMasterConnectionString(
            brandConnectionString);

        try
        {
            await using var connection = new SqlConnection(masterConnectionString);
            await connection.OpenAsync(cancellationToken);

            var databaseName = $"brand_{brandSlug}";

            // Parameterization is not supported for DDL identifiers — sanitize slug instead
            var safeName = SanitizeDatabaseName(databaseName);

            var sql = $"""
                IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'{safeName}')
                BEGIN
                    CREATE DATABASE [{safeName}]
                END
                """;

            await using var command = new SqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (SqlException ex)
        {
            if (TransientErrorNumbers.Contains(ex.Number))
            {
                logger.LogWarning(ex,
                    "Transient SQL error {ErrorNumber} while creating brand database 'brand_{BrandSlug}'. " +
                    "Will retry — {Message}",
                    ex.Number, brandSlug, ex.Message);
            }
            else
            {
                logger.LogError(ex,
                    "Permanent SQL error {ErrorNumber} while creating brand database 'brand_{BrandSlug}'. " +
                    "Manual intervention may be required — {Message}",
                    ex.Number, brandSlug, ex.Message);
            }

            throw;
        }
    }

    private async Task ApplyMigrationsAsync(
        string brandConnectionString,
        string brandSlug,
        CancellationToken cancellationToken)
    {
        var options = new DbContextOptionsBuilder<BrandDbContext>()
            .UseSqlServer(brandConnectionString)
            .Options;

        try
        {
            await using var context = new BrandDbContext(options);

            var pendingMigrations = await context.Database.GetPendingMigrationsAsync(cancellationToken);
            var pendingList = pendingMigrations.ToList();

            if (pendingList.Count == 0)
            {
                logger.LogInformation(
                    "No pending migrations for brand_{BrandSlug} — already up to date.",
                    brandSlug);
                return;
            }

            logger.LogInformation(
                "Applying {Count} migration(s) to brand_{BrandSlug}: {Migrations}",
                pendingList.Count,
                brandSlug,
                string.Join(", ", pendingList));

            await context.Database.MigrateAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to apply EF Core migrations to brand database 'brand_{BrandSlug}'. " +
                "The database may be in an inconsistent state — {Message}",
                brandSlug, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Verifies that the brand database exists and all known migrations have been applied.
    /// Connects to <c>master</c> to confirm the database is registered in <c>sys.databases</c>,
    /// then queries <c>__EFMigrationsHistory</c> and compares against the full migration list.
    /// </summary>
    public async Task VerifyDatabaseProvisionedAsync(
        string platformConnectionString,
        string brandConnectionString,
        string brandSlug,
        CancellationToken cancellationToken)
    {
        var masterConnectionString = BrandConnectionStringHelper.DeriveMasterConnectionString(
            platformConnectionString);

        var databaseName = SanitizeDatabaseName($"brand_{brandSlug}");

        // Step 1: Verify the database exists in sys.databases
        await using (var masterConnection = new SqlConnection(masterConnectionString))
        {
            await masterConnection.OpenAsync(cancellationToken);

            var checkSql = $"SELECT COUNT(1) FROM sys.databases WHERE name = N'{databaseName}'";
            await using var command = new SqlCommand(checkSql, masterConnection);
            var count = (int)(await command.ExecuteScalarAsync(cancellationToken) ?? 0);

            if (count == 0)
            {
                var message = $"Post-provisioning verification failed: database 'brand_{brandSlug}' was not found in sys.databases.";
                logger.LogError(message);
                throw new InvalidOperationException(message);
            }
        }

        // Step 2: Verify all known migrations have been applied
        var options = new DbContextOptionsBuilder<BrandDbContext>()
            .UseSqlServer(brandConnectionString)
            .Options;

        await using var context = new BrandDbContext(options);

        var allMigrations = context.Database.GetMigrations().ToList();
        var appliedMigrations = (await context.Database.GetAppliedMigrationsAsync(cancellationToken)).ToList();

        if (appliedMigrations.Count < allMigrations.Count)
        {
            var missing = allMigrations.Except(appliedMigrations).ToList();
            var message = $"Post-provisioning verification failed: brand database 'brand_{brandSlug}' is missing " +
                          $"{missing.Count} migration(s): {string.Join(", ", missing)}";
            logger.LogError(message);
            throw new InvalidOperationException(message);
        }

        logger.LogInformation(
            "Verification passed: brand database 'brand_{BrandSlug}' has {Applied}/{Total} migrations applied.",
            brandSlug,
            appliedMigrations.Count,
            allMigrations.Count);
    }

    /// <summary>
    /// Sanitizes a database name to prevent SQL injection in DDL statements.
    /// Only allows alphanumerics and underscores — the slug is already validated
    /// upstream but we enforce it here as a defence-in-depth measure.
    /// </summary>
    private static string SanitizeDatabaseName(string name)
    {
        var sanitized = new string(name.Where(c => char.IsLetterOrDigit(c) || c == '_').ToArray());

        if (sanitized.Length == 0)
            throw new ArgumentException($"Database name '{name}' is invalid after sanitization.");

        return sanitized;
    }
}

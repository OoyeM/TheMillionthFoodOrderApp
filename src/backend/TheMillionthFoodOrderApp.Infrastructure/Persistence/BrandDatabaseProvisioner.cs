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
/// 1. Derive the brand connection string from the platform connection string.
/// 2. Create the brand database if it does not already exist.
/// 3. Apply all pending EF Core migrations for <see cref="BrandDbContext"/>.
/// </summary>
public sealed class BrandDatabaseProvisioner(
    IConfiguration configuration,
    ILogger<BrandDatabaseProvisioner> logger)
{
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

        var brandConnectionString = DeriveBrandConnectionString(platformConnectionString, @event.Slug);

        await EnsureDatabaseExistsAsync(brandConnectionString, @event.Slug, cancellationToken);
        await ApplyMigrationsAsync(brandConnectionString, @event.Slug, cancellationToken);

        logger.LogInformation(
            "Brand database 'brand_{BrandSlug}' provisioned successfully.",
            @event.Slug);
    }

    private static string DeriveBrandConnectionString(string platformConnectionString, string brandSlug)
    {
        var builder = new SqlConnectionStringBuilder(platformConnectionString)
        {
            InitialCatalog = $"brand_{brandSlug}"
        };

        return builder.ConnectionString;
    }

    private async Task EnsureDatabaseExistsAsync(
        string brandConnectionString,
        string brandSlug,
        CancellationToken cancellationToken)
    {
        // Connect to master to issue CREATE DATABASE — the brand DB may not exist yet
        var masterBuilder = new SqlConnectionStringBuilder(brandConnectionString)
        {
            InitialCatalog = "master"
        };

        await using var connection = new SqlConnection(masterBuilder.ConnectionString);
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

    private async Task ApplyMigrationsAsync(
        string brandConnectionString,
        string brandSlug,
        CancellationToken cancellationToken)
    {
        var options = new DbContextOptionsBuilder<BrandDbContext>()
            .UseSqlServer(brandConnectionString)
            .Options;

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

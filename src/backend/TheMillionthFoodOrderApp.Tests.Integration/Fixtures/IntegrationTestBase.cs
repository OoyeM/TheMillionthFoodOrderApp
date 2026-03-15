using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;
using TheMillionthFoodOrderApp.Domain.Brands;
using TheMillionthFoodOrderApp.Infrastructure.Persistence;

namespace TheMillionthFoodOrderApp.Tests.Integration.Fixtures;

/// <summary>
/// Base class for integration tests. Manages a shared SQL Server Testcontainer
/// and creates isolated brand databases for each test brand (alpha, beta).
///
/// One container is shared across all tests in the collection via <see cref="IClassFixture{T}"/>.
/// </summary>
public sealed class IntegrationTestBase : IAsyncLifetime
{
    private readonly MsSqlContainer _sqlContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    /// <summary>Platform database connection string (points at the shared test SQL Server).</summary>
    public string PlatformConnectionString { get; private set; } = string.Empty;

    /// <summary>The HTTP client factory for sending requests to the test app.</summary>
    public IntegrationTestWebAppFactory Factory { get; private set; } = null!;

    /// <summary>Slug of the first test brand.</summary>
    public const string AlphaSlug = "alpha";

    /// <summary>Slug of the second test brand (used to prove cross-brand isolation).</summary>
    public const string BetaSlug = "beta";

    /// <summary>Slug of a brand that is never written to — used for "empty state" assertions.</summary>
    public const string GammaSlug = "gamma";

    public async Task InitializeAsync()
    {
        await _sqlContainer.StartAsync();

        PlatformConnectionString = _sqlContainer.GetConnectionString();

        Factory = new IntegrationTestWebAppFactory(PlatformConnectionString);

        // Apply platform DB migrations
        await using var scope = Factory.Services.CreateAsyncScope();
        var platformDb = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        await platformDb.Database.MigrateAsync();

        // Seed platform with Alpha and Beta brands so middleware validation passes
        await SeedPlatformBrandsAsync(platformDb);

        // Provision brand databases for alpha, beta, and gamma
        await ProvisionBrandDatabaseAsync(AlphaSlug);
        await ProvisionBrandDatabaseAsync(BetaSlug);
        await ProvisionBrandDatabaseAsync(GammaSlug);
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await _sqlContainer.DisposeAsync();
    }

    /// <summary>
    /// Derives a brand database connection string from the platform connection string.
    /// Mirrors the logic in <see cref="Infrastructure.Persistence.BrandDbContextFactory"/>.
    /// </summary>
    public string GetBrandConnectionString(string brandSlug)
    {
        var builder = new SqlConnectionStringBuilder(PlatformConnectionString)
        {
            InitialCatalog = $"brand_{brandSlug}"
        };
        return builder.ConnectionString;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static async Task SeedPlatformBrandsAsync(PlatformDbContext platformDb)
    {
        var alpha = Brand.Create("Alpha Brand", AlphaSlug, "alpha@test.com", null);
        var beta = Brand.Create("Beta Brand", BetaSlug, "beta@test.com", null);
        var gamma = Brand.Create("Gamma Brand", GammaSlug, "gamma@test.com", null);

        await platformDb.Brands.AddRangeAsync(alpha, beta, gamma);
        await platformDb.SaveChangesAsync();
    }

    private async Task ProvisionBrandDatabaseAsync(string brandSlug)
    {
        var brandConnectionString = GetBrandConnectionString(brandSlug);

        // Create the brand database on the SQL Server container
        var masterBuilder = new SqlConnectionStringBuilder(PlatformConnectionString)
        {
            InitialCatalog = "master"
        };

        await using var connection = new SqlConnection(masterBuilder.ConnectionString);
        await connection.OpenAsync();

        var databaseName = $"brand_{brandSlug}";
        var sql = $"""
            IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'{databaseName}')
            BEGIN
                CREATE DATABASE [{databaseName}]
            END
            """;

        await using var command = new SqlCommand(sql, connection);
        await command.ExecuteNonQueryAsync();

        // Apply BrandDbContext migrations to the new brand database
        var options = new DbContextOptionsBuilder<BrandDbContext>()
            .UseSqlServer(brandConnectionString)
            .Options;

        await using var context = new BrandDbContext(options);
        await context.Database.MigrateAsync();
    }
}

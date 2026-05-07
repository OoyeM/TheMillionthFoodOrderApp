using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;
using TheMillionthFoodOrderApp.Domain.Brands;
using TheMillionthFoodOrderApp.Infrastructure.Persistence;
using TUnit.Core.Interfaces;

namespace TheMillionthFoodOrderApp.Tests.Integration.Fixtures;

/// <summary>
/// Base class for integration tests. Manages a shared SQL Server Testcontainer
/// and creates isolated brand databases for each test brand (alpha, beta).
///
/// One container is shared across all tests in the class via <see cref="ClassDataSource{T}"/>.
/// </summary>
public sealed class IntegrationTestBase : IAsyncInitializer, IAsyncDisposable
{
    // FastEndpoints 8.0.x mutates a static JsonSerializerOptions.TypeInfoResolverChain
    // in UseFastEndpoints(). Concurrent WebApplicationFactory.StartServer() calls from
    // parallel TUnit class fixtures corrupt the shared list — serialize them instead.
    private static readonly SemaphoreSlim _serverStartLock = new(1, 1);

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

    /// <summary>Slug of a brand reserved for tests that need exclusive control over its staff (e.g. last-admin guard).</summary>
    public const string DeltaSlug = "delta";

    public async Task InitializeAsync()
    {
        await _sqlContainer.StartAsync();

        PlatformConnectionString = _sqlContainer.GetConnectionString();

        Factory = new IntegrationTestWebAppFactory(PlatformConnectionString);

        await _serverStartLock.WaitAsync();
        try
        {
            _ = Factory.Services; // triggers StartServer() once, serialized
        }
        finally
        {
            _serverStartLock.Release();
        }

        // Apply platform DB migrations
        await using var scope = Factory.Services.CreateAsyncScope();
        var platformDb = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
        await platformDb.Database.MigrateAsync();

        // Seed platform with Alpha and Beta brands so middleware validation passes
        await SeedPlatformBrandsAsync(platformDb);

        // Provision brand databases for alpha, beta, gamma, and delta
        await ProvisionBrandDatabaseAsync(AlphaSlug);
        await ProvisionBrandDatabaseAsync(BetaSlug);
        await ProvisionBrandDatabaseAsync(GammaSlug);
        await ProvisionBrandDatabaseAsync(DeltaSlug);
    }

    public async ValueTask DisposeAsync()
    {
        await Factory.DisposeAsync();
        await _sqlContainer.DisposeAsync();
    }

    /// <summary>
    /// Derives a brand database connection string from the platform connection string.
    /// Delegates to <see cref="BrandConnectionStringHelper"/> — the single source of truth.
    /// </summary>
    public string GetBrandConnectionString(string brandSlug) =>
        BrandConnectionStringHelper.DeriveBrandConnectionString(PlatformConnectionString, brandSlug);

    // ── Private helpers ───────────────────────────────────────────────────────

    private static async Task SeedPlatformBrandsAsync(PlatformDbContext platformDb)
    {
        var alpha = Brand.Create("Alpha Brand", AlphaSlug, "alpha@test.com", null);
        var beta = Brand.Create("Beta Brand", BetaSlug, "beta@test.com", null);
        var gamma = Brand.Create("Gamma Brand", GammaSlug, "gamma@test.com", null);
        var delta = Brand.Create("Delta Brand", DeltaSlug, "delta@test.com", null);

        await platformDb.Brands.AddRangeAsync(alpha, beta, gamma, delta);
        await platformDb.SaveChangesAsync();
    }

    private async Task ProvisionBrandDatabaseAsync(string brandSlug)
    {
        var brandConnectionString = GetBrandConnectionString(brandSlug);
        var masterConnectionString = BrandConnectionStringHelper.DeriveMasterConnectionString(
            PlatformConnectionString);

        // Create the brand database on the SQL Server container
        await using var connection = new SqlConnection(masterConnectionString);
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

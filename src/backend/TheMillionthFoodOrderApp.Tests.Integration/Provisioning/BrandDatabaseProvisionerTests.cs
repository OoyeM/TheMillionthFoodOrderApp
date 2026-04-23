using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using TheMillionthFoodOrderApp.Infrastructure.Persistence;
using TheMillionthFoodOrderApp.Tests.Integration.Fixtures;

namespace TheMillionthFoodOrderApp.Tests.Integration.Provisioning;

/// <summary>
/// Integration tests for <see cref="BrandDatabaseProvisioner"/>.
/// Uses a real SQL Server via Testcontainers (shared container from <see cref="IntegrationTestBase"/>).
/// The provisioner is instantiated directly with test-scoped configuration — it is not DI-registered.
/// </summary>
[ClassDataSource<IntegrationTestBase>(Shared = SharedType.PerClass)]
public sealed class BrandDatabaseProvisionerTests(IntegrationTestBase fixture)
{
    /// <summary>
    /// Builds an IConfiguration pointing the "platform" connection string at the test SQL container.
    /// </summary>
    private IConfiguration BuildConfiguration() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:platform"] = fixture.PlatformConnectionString,
            })
            .Build();

    private BrandDatabaseProvisioner BuildProvisioner() =>
        new(BuildConfiguration(), NullLogger<BrandDatabaseProvisioner>.Instance);

    private async Task<bool> DatabaseExistsAsync(string slug, CancellationToken cancellationToken = default)
    {
        var masterConnectionString = BrandConnectionStringHelper.DeriveMasterConnectionString(
            fixture.PlatformConnectionString);

        await using var connection = new SqlConnection(masterConnectionString);
        await connection.OpenAsync(cancellationToken);

        var databaseName = $"brand_{slug}";
        await using var command = new SqlCommand(
            $"SELECT COUNT(1) FROM sys.databases WHERE name = N'{databaseName}'",
            connection);

        var count = (int)(await command.ExecuteScalarAsync(cancellationToken) ?? 0);
        return count > 0;
    }

    private async Task<int> GetAppliedMigrationCountAsync(string slug, CancellationToken cancellationToken = default)
    {
        var brandConnectionString = fixture.GetBrandConnectionString(slug);

        var options = new DbContextOptionsBuilder<BrandDbContext>()
            .UseSqlServer(brandConnectionString)
            .Options;

        await using var context = new BrandDbContext(options);
        var applied = await context.Database.GetAppliedMigrationsAsync(cancellationToken);
        return applied.Count();
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Test]
    public async Task Provisioner_CreatesBrandDatabase_WhenNotExists()
    {
        const string slug = "test-prov-create";
        var provisioner = BuildProvisioner();

        var existsBefore = await DatabaseExistsAsync(slug);
        await Assert.That(existsBefore).IsFalse();

        await provisioner.HandleAsync(
            new TheMillionthFoodOrderApp.Domain.Brands.BrandCreatedEvent(Guid.CreateVersion7(), "Test Prov Create", slug),
            CancellationToken.None);

        var existsAfter = await DatabaseExistsAsync(slug);
        await Assert.That(existsAfter).IsTrue();
    }

    [Test]
    public async Task Provisioner_AppliesMigrations_ToNewDatabase()
    {
        const string slug = "test-prov-migrations";
        var provisioner = BuildProvisioner();

        await provisioner.HandleAsync(
            new TheMillionthFoodOrderApp.Domain.Brands.BrandCreatedEvent(Guid.CreateVersion7(), "Test Prov Migrations", slug),
            CancellationToken.None);

        var migrationCount = await GetAppliedMigrationCountAsync(slug);
        await Assert.That(migrationCount).IsGreaterThan(0);
    }

    [Test]
    public async Task Provisioner_IsIdempotent_WhenCalledTwice()
    {
        const string slug = "test-prov-idempotent";
        var provisioner = BuildProvisioner();
        var @event = new TheMillionthFoodOrderApp.Domain.Brands.BrandCreatedEvent(
            Guid.CreateVersion7(), "Test Prov Idempotent", slug);

        // First call — provisions database and applies migrations
        await provisioner.HandleAsync(@event, CancellationToken.None);

        // Second call — should be a no-op, no exception thrown
        Exception? exception = null;
        try
        {
            await provisioner.HandleAsync(@event, CancellationToken.None);
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        await Assert.That(exception).IsNull();

        // Database should still be correctly provisioned
        var existsAfter = await DatabaseExistsAsync(slug);
        await Assert.That(existsAfter).IsTrue();
    }

    [Test]
    public async Task Provisioner_VerifiesDatabase_AfterProvisioning()
    {
        const string slug = "test-prov-verify";
        var provisioner = BuildProvisioner();

        await provisioner.HandleAsync(
            new TheMillionthFoodOrderApp.Domain.Brands.BrandCreatedEvent(Guid.CreateVersion7(), "Test Prov Verify", slug),
            CancellationToken.None);

        // Directly call VerifyDatabaseProvisionedAsync — it should not throw
        Exception? exception = null;
        try
        {
            await provisioner.VerifyDatabaseProvisionedAsync(
                fixture.PlatformConnectionString,
                fixture.GetBrandConnectionString(slug),
                slug,
                CancellationToken.None);
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        await Assert.That(exception).IsNull();
    }

    [Test]
    public async Task Provisioner_HandlesInvalidSlug_Gracefully()
    {
        var provisioner = BuildProvisioner();

        // Empty slug produces an empty database name after sanitization — ArgumentException expected
        Exception? exception = null;
        try
        {
            await provisioner.HandleAsync(
                new TheMillionthFoodOrderApp.Domain.Brands.BrandCreatedEvent(
                    Guid.CreateVersion7(), "Invalid Brand", string.Empty),
                CancellationToken.None);
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        await Assert.That(exception).IsNotNull();
        await Assert.That(exception).IsTypeOf<ArgumentException>();
    }
}

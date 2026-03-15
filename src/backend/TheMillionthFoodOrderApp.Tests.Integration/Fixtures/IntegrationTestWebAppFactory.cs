using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TheMillionthFoodOrderApp.Infrastructure.Persistence;
using TheMillionthFoodOrderApp.Infrastructure.Persistence.Interceptors;

namespace TheMillionthFoodOrderApp.Tests.Integration.Fixtures;

/// <summary>
/// <see cref="WebApplicationFactory{TEntryPoint}"/> that replaces the Aspire-injected
/// SQL Server connection strings with Testcontainers-provided ones.
///
/// The factory accepts the SQL Server connection string for the platform database.
/// Brand databases are derived from the platform connection string at runtime
/// (same SQL Server instance, different Initial Catalog) — exactly as production does.
/// </summary>
public sealed class IntegrationTestWebAppFactory(string platformConnectionString)
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Override configuration so Aspire connection string lookup finds our test value
        builder.ConfigureAppConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                // Aspire reads connection strings via IConfiguration["ConnectionStrings:platform"]
                ["ConnectionStrings:platform"] = platformConnectionString,
            });
        });

        builder.ConfigureServices(services =>
        {
            // Remove the Aspire-registered PlatformDbContext (pooled, with Aspire health checks)
            // and replace with a standard EF Core registration pointing at the test container.
            services.RemoveAll<DbContextOptions<PlatformDbContext>>();
            services.RemoveAll<PlatformDbContext>();

            services.AddDbContext<PlatformDbContext>((sp, options) =>
            {
                options.UseSqlServer(platformConnectionString);
                var interceptor = sp.GetRequiredService<AuditSaveChangesInterceptor>();
                options.AddInterceptors(interceptor);
            });
        });

        // Use "Testing" environment so seeding and migrations behave predictably.
        // We explicitly run migrations in IntegrationTestBase.InitializeAsync().
        builder.UseEnvironment("Testing");
    }
}

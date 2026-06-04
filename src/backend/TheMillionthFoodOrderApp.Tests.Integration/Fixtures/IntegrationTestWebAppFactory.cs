using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TheMillionthFoodOrderApp.Application.Email;
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
            // Remove ALL Aspire-registered PlatformDbContext infrastructure.
            // Aspire's AddSqlServerDbContext registers a pooled context with multiple
            // service descriptors (DbContextOptions, pooled factory, the context itself).
            // We must remove every descriptor whose service or implementation type
            // references PlatformDbContext, then re-register as a standard DbContext.
            services.RemoveAll<DbContextOptions<PlatformDbContext>>();
            services.RemoveAll<PlatformDbContext>();

            // Remove pooled context registrations that Aspire adds internally.
            // These are generic types (IDbContextPool<T>, IDbContextFactory<T>) that
            // we can't reference directly since IDbContextPool is internal to EF Core.
            for (var i = services.Count - 1; i >= 0; i--)
            {
                var descriptor = services[i];
                var serviceType = descriptor.ServiceType;

                if (serviceType.IsGenericType &&
                    serviceType.GenericTypeArguments.Length == 1 &&
                    serviceType.GenericTypeArguments[0] == typeof(PlatformDbContext))
                {
                    services.RemoveAt(i);
                }
            }

            // Re-register as a standard (non-pooled) DbContext pointing at the test container.
            services.AddDbContext<PlatformDbContext>((sp, options) =>
            {
                options.UseSqlServer(platformConnectionString);
                var interceptor = sp.GetRequiredService<AuditSaveChangesInterceptor>();
                options.AddInterceptors(interceptor);
            });

            // Replace the real IEmailSender with a recording fake so integration tests
            // can inspect outbound emails without an SMTP relay (US-FP-051).
            services.RemoveAll<IEmailSender>();
            var recordingEmailSender = new RecordingEmailSender();
            services.AddSingleton<RecordingEmailSender>(recordingEmailSender);
            services.AddSingleton<IEmailSender>(recordingEmailSender);
        });

        // Use "Testing" environment so seeding and migrations behave predictably.
        // We explicitly run migrations in IntegrationTestBase.InitializeAsync().
        builder.UseEnvironment("Testing");
    }
}

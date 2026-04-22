using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace TheMillionthFoodOrderApp.Bff.Tests.Fixtures;

/// <summary>
/// <see cref="WebApplicationFactory{TEntryPoint}"/> for BFF integration tests.
///
/// Sets <c>Authentication:UseMockAuth=true</c> so the mock login flow is active.
/// The BFF does not talk to any SQL Server directly when mock auth is enabled —
/// no container is needed.
/// </summary>
public sealed class BffTestWebAppFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Explicitly enable mock auth so tests are hermetic and do not require
        // Keycloak, a database, or any external services.
        builder.ConfigureAppConfiguration(config =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Authentication:UseMockAuth"] = "true",

                // Provide a minimal YARP destination so the reverse proxy starts
                // without service discovery (Aspire is not running in tests).
                ["ReverseProxy:Clusters:api-cluster:Destinations:api-destination:Address"] =
                    "http://localhost:9999",
            });
        });

        // "Development" is required for mock auth to be registered by Program.cs
        // (the guard is: env.IsDevelopment() && config.GetValue<bool>("Authentication:UseMockAuth"))
        builder.UseEnvironment("Development");
    }
}

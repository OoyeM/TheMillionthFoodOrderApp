using FastEndpoints;
using FastEndpoints.Swagger;
using Microsoft.EntityFrameworkCore;
using TheMillionthFoodOrderApp.Api.Auth;
using TheMillionthFoodOrderApp.Api.Middleware;
using TheMillionthFoodOrderApp.Application;
using TheMillionthFoodOrderApp.Infrastructure;
using TheMillionthFoodOrderApp.Infrastructure.Multitenancy;
using TheMillionthFoodOrderApp.Infrastructure.Persistence;
using TheMillionthFoodOrderApp.Infrastructure.Persistence.Seeding;
using TheMillionthFoodOrderApp.ServiceDefaults;
using Wolverine;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Register infrastructure services first (includes AuditSaveChangesInterceptor as singleton)
builder.Services.AddInfrastructure();

// ---------------------------------------------------------------------------
// Authentication
// Dev: no-op pass-through so endpoints work without Azure subscription.
// Prod: JWT bearer validation against Entra External ID (TODO: wire when ready).
// The middleware (UseAuthentication/UseAuthorization) is always in the pipeline
// so adding a real scheme later doesn't require pipeline changes.
// ---------------------------------------------------------------------------
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddAuthentication("DevPassThrough")
        .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions,
                   DevPassThroughHandler>("DevPassThrough", _ => { });
}
else
{
    // TODO: register JWT bearer scheme for Entra External ID
    builder.Services.AddAuthentication();
}

builder.Services.AddAuthorization();

// Platform SQL Server database — connection string injected by Aspire via the name "platform".
// The Aspire integration sets up health checks, retries, and telemetry automatically.
builder.AddSqlServerDbContext<PlatformDbContext>("platform",
    configureDbContextOptions: options =>
    {
        options.AddInterceptors(new TheMillionthFoodOrderApp.Infrastructure.Persistence.Interceptors.AuditSaveChangesInterceptor());
    });

builder.Services.AddApplication();
builder.Host.UseWolverine();
builder.Services.AddFastEndpoints();
builder.Services.SwaggerDocument(o =>
{
    o.DocumentSettings = s =>
    {
        s.Title = "TheMillionthFoodOrderApp API";
        s.Version = "v1";
    };
});

var app = builder.Build();

// Run platform DB migrations and dev seeding before serving requests
await using (var scope = app.Services.CreateAsyncScope())
{
    var platformDb = scope.ServiceProvider.GetRequiredService<PlatformDbContext>();
    await platformDb.Database.MigrateAsync();

    if (app.Environment.IsDevelopment())
    {
        var platformSeeder = scope.ServiceProvider.GetRequiredService<PlatformDbSeeder>();
        await platformSeeder.SeedAsync();

        // Provision the brand database before seeding — in production this happens
        // asynchronously via Wolverine when BrandCreatedEvent is raised, but during
        // startup seeding Wolverine hasn't started processing messages yet.
        var brandProvisioner = new BrandDatabaseProvisioner(
            scope.ServiceProvider.GetRequiredService<IConfiguration>(),
            scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger<BrandDatabaseProvisioner>());
        await brandProvisioner.HandleAsync(
            new TheMillionthFoodOrderApp.Domain.Brands.BrandCreatedEvent(Guid.Empty, "Frietjes?", "frietjes"),
            CancellationToken.None);

        // Seed the Frietjes? brand database.
        // We manually set the brand slug on the scoped accessor so BrandDbContextFactory
        // can derive the correct connection string for the brand DB.
        var brandContextAccessor = scope.ServiceProvider.GetRequiredService<BrandContextAccessor>();
        brandContextAccessor.BrandSlug = "frietjes";

        var brandSeeder = scope.ServiceProvider.GetRequiredService<BrandDbSeeder>();
        await brandSeeder.SeedAsync();
    }
}

app.MapDefaultEndpoints();

app.UseAuthentication();
app.UseAuthorization();

// Extract and validate brand slug from route/header early in the pipeline
app.UseMiddleware<BrandContextMiddleware>();

app.UseFastEndpoints();
app.UseSwaggerGen();

app.Run();

// Make Program accessible to WebApplicationFactory in integration tests
public partial class Program { }

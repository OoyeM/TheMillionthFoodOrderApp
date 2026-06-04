using FastEndpoints;
using FastEndpoints.Swagger;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using TheMillionthFoodOrderApp.Api.Auth;
using TheMillionthFoodOrderApp.Api.Middleware;
using TheMillionthFoodOrderApp.Application;
using TheMillionthFoodOrderApp.Infrastructure;
using TheMillionthFoodOrderApp.Infrastructure.Email;
using TheMillionthFoodOrderApp.Infrastructure.FileStorage;
using TheMillionthFoodOrderApp.Infrastructure.Multitenancy;
using TheMillionthFoodOrderApp.Infrastructure.Persistence;
using TheMillionthFoodOrderApp.Infrastructure.Persistence.Seeding;
using TheMillionthFoodOrderApp.ServiceDefaults;
using Wolverine;
using Wolverine.ErrorHandling;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Configure local file storage — uploads land in wwwroot/uploads/ and are served as static files.
// In production this registration can be replaced with an Azure Blob Storage implementation.
var uploadsPath = Path.Combine(builder.Environment.WebRootPath ?? Path.Combine(builder.Environment.ContentRootPath, "wwwroot"), "uploads");
builder.Services.Configure<LocalFileStorageOptions>(opts =>
{
    opts.UploadsPath = uploadsPath;
    opts.UrlPrefix = "/uploads";
});

// Configure SMTP for the digital receipt email (US-FP-051). Host/Port are injected by Aspire
// from the mailpit container in dev (Email__Host / Email__Port); prod overrides the "Email"
// section to point at a real SMTP relay.
builder.Services.Configure<SmtpOptions>(builder.Configuration.GetSection("Email"));

// Register infrastructure services first (includes AuditSaveChangesInterceptor as singleton)
builder.Services.AddInfrastructure();

// ---------------------------------------------------------------------------
// Authentication
// Dev: no-op pass-through so endpoints work without identity provider.
// Prod: JWT bearer validation against Keycloak-issued tokens.
// Toggle: set Authentication:UseDevPassThrough=false to use real JWT validation.
// ---------------------------------------------------------------------------
var useDevPassThrough = builder.Environment.IsDevelopment() &&
    builder.Configuration.GetValue<bool>("Authentication:UseDevPassThrough", true);

if (useDevPassThrough)
{
    builder.Services.AddAuthentication("DevPassThrough")
        .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions,
                   DevPassThroughHandler>("DevPassThrough", _ => { });
}
else
{
    builder.Services.AddAuthentication("Bearer")
        .AddJwtBearer("Bearer", options =>
        {
            options.Authority = builder.Configuration["Authentication:Keycloak:Authority"];
            options.Audience = builder.Configuration["Authentication:Keycloak:Audience"];
            options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();

            options.MapInboundClaims = false;
            options.TokenValidationParameters.NameClaimType = "preferred_username";
        });
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
builder.Host.UseWolverine(opts =>
{
    // Scan the Infrastructure assembly for Wolverine handlers (e.g. OrderStatusChangedHandler,
    // BrandDatabaseProvisioner). Wolverine only scans the entry assembly by default.
    opts.Discovery.IncludeAssembly(typeof(BrandDatabaseProvisioner).Assembly);

    // Retry transient SQL failures with cooldown periods: 1s, 5s, then 15s before dead-lettering.
    opts.OnException<SqlException>()
        .RetryWithCooldown(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(15));
});

builder.Services.AddHealthChecks()
    .AddCheck<BrandDatabaseHealthCheck>("brand-databases");

builder.Services.AddSignalR();
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

// Serve static files from wwwroot (includes uploaded logos at /uploads/*)
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

// Extract and validate brand slug from route/header early in the pipeline
app.UseMiddleware<BrandContextMiddleware>();

app.UseFastEndpoints();
app.UseSwaggerGen();

// SignalR hub for real-time order updates (US-FP-068)
app.MapHub<TheMillionthFoodOrderApp.Infrastructure.Notifications.OrderHub>("/api/hubs/orders");

app.Run();

// Make Program accessible to WebApplicationFactory in integration tests
public partial class Program { }

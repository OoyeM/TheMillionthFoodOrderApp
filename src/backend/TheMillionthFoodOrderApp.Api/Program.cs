using FastEndpoints;
using FastEndpoints.Swagger;
using Microsoft.EntityFrameworkCore;
using TheMillionthFoodOrderApp.Api.Auth;
using TheMillionthFoodOrderApp.Api.Middleware;
using TheMillionthFoodOrderApp.Application;
using TheMillionthFoodOrderApp.Infrastructure;
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
    }
}

app.MapDefaultEndpoints();

app.UseAuthentication();
app.UseAuthorization();

// Extract brand slug from route/header early in the pipeline
app.UseMiddleware<BrandContextMiddleware>();

app.UseFastEndpoints();
app.UseSwaggerGen();

app.Run();

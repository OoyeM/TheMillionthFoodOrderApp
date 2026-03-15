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
// In Development the API trusts requests forwarded by the BFF. Authentication
// is skipped (all endpoints currently use AllowAnonymous). In production,
// JWT bearer validation against Entra External ID will be wired here.
// ---------------------------------------------------------------------------
if (builder.Environment.IsDevelopment())
{
    // Register a no-op authentication so the auth middleware is present but
    // never rejects requests. This keeps the pipeline consistent across envs.
    builder.Services.AddAuthentication("DevPassThrough")
        .AddScheme<Microsoft.AspNetCore.Authentication.AuthenticationSchemeOptions,
                   DevPassThroughHandler>("DevPassThrough", _ => { });
    builder.Services.AddAuthorization();
}

// Platform SQL Server database — connection string injected by Aspire via the name "platform".
// The Aspire integration sets up health checks, retries, and telemetry automatically.
builder.AddSqlServerDbContext<PlatformDbContext>("platform");

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

if (app.Environment.IsDevelopment())
{
    app.UseAuthentication();
    app.UseAuthorization();
}

// Extract brand slug from route/header early in the pipeline
app.UseMiddleware<BrandContextMiddleware>();

app.UseFastEndpoints();
app.UseSwaggerGen();

app.Run();

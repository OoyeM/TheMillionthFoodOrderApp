using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using TheMillionthFoodOrderApp.Bff.Auth;
using TheMillionthFoodOrderApp.Bff.Endpoints;
using TheMillionthFoodOrderApp.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Aspire service defaults (telemetry, health checks, service discovery)
// ---------------------------------------------------------------------------
builder.AddServiceDefaults();

// ---------------------------------------------------------------------------
// Authentication
// ---------------------------------------------------------------------------

var slidingHours = builder.Configuration.GetValue<int>("Authentication:Cookie:SlidingExpirationHours", 8);

var authBuilder = builder.Services
    .AddAuthentication(defaultScheme: AuthConstants.Schemes.Cookie)
    .AddCookie(AuthConstants.Schemes.Cookie, options =>
    {
        options.Cookie.Name       = "bff_session";
        options.Cookie.HttpOnly   = true;
        options.Cookie.SameSite   = builder.Environment.IsDevelopment()
                                        ? SameSiteMode.Lax
                                        : SameSiteMode.Strict;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
                                        ? CookieSecurePolicy.SameAsRequest
                                        : CookieSecurePolicy.Always;

        options.SlidingExpiration = true;
        options.ExpireTimeSpan    = TimeSpan.FromHours(slidingHours);

        // Never redirect to /Account/Login — BFF returns structured responses
        options.Events.OnRedirectToLogin        = ctx => { ctx.Response.StatusCode = 401; return Task.CompletedTask; };
        options.Events.OnRedirectToAccessDenied = ctx => { ctx.Response.StatusCode = 403; return Task.CompletedTask; };
    });

// Mock auth — only registered in Development + config flag
var useMockAuth = builder.Environment.IsDevelopment() &&
                  builder.Configuration.GetValue<bool>("Authentication:UseMockAuth");

if (useMockAuth)
{
    authBuilder.AddScheme<AuthenticationSchemeOptions, MockAuthHandler>(
        AuthConstants.Schemes.Mock, _ => { });
}

// ---------------------------------------------------------------------------
// Authorization policies
// ---------------------------------------------------------------------------
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthConstants.Policies.RequireAuthenticated, policy =>
        policy.RequireAuthenticatedUser());

    options.AddPolicy(AuthConstants.Policies.RequireStaff, policy =>
        policy.RequireRole(
            AuthConstants.Roles.PlatformAdmin,
            AuthConstants.Roles.BrandAdmin,
            AuthConstants.Roles.ShopManager,
            AuthConstants.Roles.CounterStaff,
            AuthConstants.Roles.KitchenStaff,
            AuthConstants.Roles.FloorStaff));

    options.AddPolicy(AuthConstants.Policies.RequireBrandAdmin, policy =>
        policy.RequireRole(
            AuthConstants.Roles.PlatformAdmin,
            AuthConstants.Roles.BrandAdmin));

    options.AddPolicy(AuthConstants.Policies.RequirePlatformAdmin, policy =>
        policy.RequireRole(AuthConstants.Roles.PlatformAdmin));
});

// ---------------------------------------------------------------------------
// YARP reverse proxy with Aspire service discovery
// ---------------------------------------------------------------------------
builder.Services
    .AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver();

// ---------------------------------------------------------------------------
// Build
// ---------------------------------------------------------------------------
var app = builder.Build();

// Log a critical warning so mock auth is impossible to miss in the console
if (useMockAuth)
{
    var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
    startupLogger.LogCritical(
        "*** MOCK AUTHENTICATION IS ACTIVE — NOT FOR PRODUCTION USE ***");
}

// ---------------------------------------------------------------------------
// Middleware pipeline (order matters)
// ---------------------------------------------------------------------------
app.UseAuthentication();
app.UseAuthorization();

// BFF management endpoints (/bff/login, /bff/logout, /bff/user, /bff/session/keepalive)
app.MapBffEndpoints();

// Forward /api/** to the upstream API via YARP
app.MapReverseProxy(proxyPipeline =>
{
    // Forward X-Brand-Slug header from the incoming request
    proxyPipeline.Use(async (context, next) =>
    {
        if (context.Request.Headers.TryGetValue("X-Brand-Slug", out var brandSlug))
            context.Request.Headers["X-Brand-Slug"] = brandSlug;

        await next();
    });
});

// Aspire health / liveness / readiness endpoints
app.MapDefaultEndpoints();

app.Run();

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using TheMillionthFoodOrderApp.Application;
using TheMillionthFoodOrderApp.Bff.Auth;
using TheMillionthFoodOrderApp.Bff.Endpoints;
using TheMillionthFoodOrderApp.Bff.Security;
using TheMillionthFoodOrderApp.Infrastructure;
using TheMillionthFoodOrderApp.Infrastructure.Persistence;
using TheMillionthFoodOrderApp.Infrastructure.Persistence.Interceptors;
using TheMillionthFoodOrderApp.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Kestrel — keep request bodies small by default. The YARP /api/* route
// overrides this with a 10 MiB cap to allow logo uploads etc.
// ---------------------------------------------------------------------------
builder.WebHost.ConfigureKestrel(o =>
{
    o.Limits.MaxRequestBodySize = 8 * 1024; // 8 KiB
});

// ---------------------------------------------------------------------------
// Aspire service defaults (telemetry, health checks, service discovery)
// ---------------------------------------------------------------------------
builder.AddServiceDefaults();

// ---------------------------------------------------------------------------
// Data Protection — keys must be shared across replicas and survive deploys.
// Default location lives next to the binaries; production should override
// `DataProtection:KeyPath` to point at a persistent volume mounted into the
// container (or replace with a remote key store such as Azure Blob).
// ---------------------------------------------------------------------------
var keyPath = builder.Configuration["DataProtection:KeyPath"]
              ?? Path.Combine(AppContext.BaseDirectory, "dpkeys");

Directory.CreateDirectory(keyPath);

builder.Services.AddDataProtection()
    .SetApplicationName("TheMillionthFoodOrderApp")
    .PersistKeysToFileSystem(new DirectoryInfo(keyPath));

// ---------------------------------------------------------------------------
// Authentication
// ---------------------------------------------------------------------------

var slidingHours = builder.Configuration.GetValue<int>("Authentication:Cookie:SlidingExpirationHours", 8);

var authBuilder = builder.Services
    .AddAuthentication(defaultScheme: AuthConstants.Schemes.Cookie)
    .AddCookie(AuthConstants.Schemes.Cookie, options =>
    {
        // The "__Host-" prefix prevents subdomain or non-Secure cookies from
        // shadowing the session cookie; it requires Secure=true, Path=/, and
        // no Domain attribute. We can only use it in production where
        // SecurePolicy=Always is enforced.
        options.Cookie.Name       = builder.Environment.IsDevelopment()
                                        ? "bff_session"
                                        : "__Host-bff_session";
        options.Cookie.Path       = "/";
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

        // Periodic session revocation check via OIDC introspection.
        // No-op for mock auth (tokens absent) and for sessions that fail to
        // resolve the validator (e.g. introspection endpoint unreachable).
        options.Events.OnValidatePrincipal = async ctx =>
        {
            var validator = ctx.HttpContext.RequestServices.GetService<SessionRevocationValidator>();
            if (validator is not null)
                await validator.ValidateAsync(ctx);
        };
    });

// Mock auth — only registered in Development + config flag
var useMockAuth = builder.Environment.IsDevelopment() &&
                  builder.Configuration.GetValue<bool>("Authentication:UseMockAuth");

if (useMockAuth)
{
    authBuilder.AddScheme<AuthenticationSchemeOptions, MockAuthHandler>(
        AuthConstants.Schemes.Mock, _ => { });
}
else
{
    // Real OIDC via Keycloak (or any OIDC provider)
    authBuilder.AddOpenIdConnect(AuthConstants.Schemes.Oidc, options =>
    {
        options.Authority = builder.Configuration["Authentication:Keycloak:Authority"];
        options.ClientId = builder.Configuration["Authentication:Keycloak:ClientId"];
        options.ClientSecret = builder.Configuration["Authentication:Keycloak:ClientSecret"];
        options.ResponseType = "code";
        // PKCE is enabled even though this is a confidential client (with client_secret).
        // Defense-in-depth per OAuth 2.1: PKCE recommended for ALL clients.
        options.UsePkce = true;
        options.SaveTokens = true;
        options.GetClaimsFromUserInfoEndpoint = true;
        options.SignInScheme = AuthConstants.Schemes.Cookie;

        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");
        // phone scope yields the phone_number claim used to prefill storefront checkout (US-FP-051).
        options.Scope.Add("phone");
        // offline_access is required for Keycloak to issue refresh tokens —
        // without it, access tokens stored in the cookie cannot be renewed
        // and proxied API calls start failing with 401 after ~5 minutes.
        options.Scope.Add("offline_access");

        options.MapInboundClaims = false;
        options.TokenValidationParameters.NameClaimType = "preferred_username";

        if (builder.Environment.IsDevelopment())
        {
            options.RequireHttpsMetadata = false;
        }

        options.Events = new OpenIdConnectEvents
        {
            OnTokenValidated = async context =>
            {
                var enrichmentService = context.HttpContext.RequestServices
                    .GetRequiredService<ClaimsEnrichmentService>();
                await enrichmentService.EnrichClaimsAsync(context);
            }
        };
    });

    // Claims enrichment requires Application + Infrastructure layers
    builder.Services.AddScoped<ClaimsEnrichmentService>();
    builder.Services.AddSingleton<TokenRefreshService>();
    builder.Services.AddSingleton<SessionRevocationValidator>();
    builder.Services.AddHttpClient();
    builder.Services.AddInfrastructure();
    builder.Services.AddApplication();
    builder.AddSqlServerDbContext<PlatformDbContext>("platform",
        configureDbContextOptions: options =>
        {
            options.AddInterceptors(new AuditSaveChangesInterceptor());
        });
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
// Rate limiting — per-IP fixed window on the login endpoint to slow brute-force
// and persona enumeration. Other endpoints use the default global no-limit.
// ---------------------------------------------------------------------------
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy(AuthConstants.RateLimitPolicies.Login, httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit          = 10,
                Window               = TimeSpan.FromMinutes(1),
                QueueLimit           = 0,
                AutoReplenishment    = true,
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            }));
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

// Security response headers must precede everything so 401/403 short-circuits
// also receive them.
app.UseMiddleware<SecurityHeadersMiddleware>();

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseAuthentication();
app.UseAuthorization();

// CSRF check runs after authentication so it can short-circuit anonymous
// requests cleanly, and before any state-changing endpoint logic executes.
app.UseMiddleware<CsrfHeaderMiddleware>();

app.UseRateLimiter();

// BFF management endpoints (/bff/login, /bff/logout, /bff/user, /bff/session/keepalive)
app.MapBffEndpoints();

// Forward /api/** to the upstream API via YARP — authenticated users only.
// Anonymous requests are rejected at the BFF before YARP forwards anything.
app.MapReverseProxy(proxyPipeline =>
{
    proxyPipeline.Use(async (context, next) =>
    {
        // Proxied API calls (logo uploads etc.) need a larger body cap than the
        // global 8 KiB BFF limit. Override per-request via the body-size feature.
        var bodySizeFeature = context.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpMaxRequestBodySizeFeature>();
        if (bodySizeFeature is { IsReadOnly: false })
            bodySizeFeature.MaxRequestBodySize = 10 * 1024 * 1024;

        // SECURITY: never trust a client-supplied X-Brand-Slug. The canonical brand
        // slug for a request is the route value {brandSlug} on the API side; this
        // header is only repopulated server-side from the user's claims as a
        // convenience for non-route paths and for users with a single brand.
        context.Request.Headers.Remove("X-Brand-Slug");

        var brandSlugs = context.User
            .FindAll(AuthConstants.Claims.BrandSlug)
            .Select(c => c.Value)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct()
            .ToArray();

        if (brandSlugs.Length == 1)
            context.Request.Headers["X-Brand-Slug"] = brandSlugs[0];

        // Forward access token as Bearer header. The refresh service silently
        // renews near-expiry tokens so proxied calls don't 401 mid-session.
        var refreshService = context.RequestServices.GetService<TokenRefreshService>();
        var accessToken = refreshService is null
            ? await context.GetTokenAsync("access_token")
            : await refreshService.GetFreshAccessTokenAsync(context);

        if (!string.IsNullOrEmpty(accessToken))
        {
            context.Request.Headers.Authorization = $"Bearer {accessToken}";
        }

        await next();
    });
})
.RequireAuthorization(AuthConstants.Policies.RequireAuthenticated);

// Aspire health / liveness / readiness endpoints
app.MapDefaultEndpoints();

app.Run();

// Exposes the auto-generated Program class so WebApplicationFactory<Program> can reference it.
// Zero runtime behavior impact — only enables test projects to reference this entry point.
public partial class Program { }

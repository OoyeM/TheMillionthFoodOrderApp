using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using TheMillionthFoodOrderApp.Bff.Auth;

namespace TheMillionthFoodOrderApp.Bff.Endpoints;

/// <summary>
/// Minimal-API endpoint registrations for all /bff/* routes.
/// The BFF layer is deliberately thin — no FastEndpoints, no controllers.
/// </summary>
public static class BffEndpoints
{
    public static IEndpointRouteBuilder MapBffEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/bff/login",               (Delegate)HandleLogin);
        app.MapPost("/bff/logout",             (Delegate)HandleLogout);
        app.MapGet("/bff/user",               (Delegate)HandleUser);
        app.MapPost("/bff/session/keepalive", (Delegate)HandleKeepalive);

        return app;
    }

    // -------------------------------------------------------------------------
    // GET /bff/login
    // -------------------------------------------------------------------------

    /// <summary>
    /// In dev/mock mode: resolves the persona from <c>?mock=</c>, signs the user in via
    /// the cookie scheme, then redirects to <c>?returnUrl=</c>.
    ///
    /// When mock auth is disabled, triggers an OIDC challenge against the configured identity provider.
    /// </summary>
    private static async Task<IResult> HandleLogin(
        HttpContext context,
        IHostEnvironment env,
        IConfiguration config,
        ILogger<BffEndpointMarker> logger,
        string? mock      = null,
        string? returnUrl = null)
    {
        var useMock = env.IsDevelopment() &&
                      config.GetValue<bool>("Authentication:UseMockAuth");

        if (useMock && !string.IsNullOrWhiteSpace(mock))
        {
            var principal = MockAuthHandler.BuildPrincipal(mock);
            if (principal is null)
            {
                logger.LogWarning("Mock login attempted with unknown persona '{Persona}'", mock);
                return Results.BadRequest(new { error = $"Unknown mock persona '{mock}'" });
            }

            await context.SignInAsync(AuthConstants.Schemes.Cookie, principal);
            logger.LogWarning(
                "[MOCK AUTH] Signed in as persona '{Persona}' (sub={Sub})",
                mock,
                principal.FindFirstValue(ClaimTypes.NameIdentifier));

            var redirect = ResolveReturnUrl(returnUrl);
            return Results.Redirect(redirect);
        }

        // OIDC challenge via Keycloak (or any configured OIDC provider)
        return Results.Challenge(
            new AuthenticationProperties { RedirectUri = ResolveReturnUrl(returnUrl) },
            [AuthConstants.Schemes.Oidc]);
    }

    // -------------------------------------------------------------------------
    // POST /bff/logout
    // -------------------------------------------------------------------------

    /// <summary>
    /// Signs the user out of the cookie session.
    /// When mock auth is disabled, also triggers a federated sign-out redirect to the OIDC provider.
    /// </summary>
    private static async Task<IResult> HandleLogout(
        HttpContext context,
        IHostEnvironment env,
        IConfiguration config)
    {
        await context.SignOutAsync(AuthConstants.Schemes.Cookie);

        var useMock = env.IsDevelopment() &&
                      config.GetValue<bool>("Authentication:UseMockAuth");

        if (!useMock)
        {
            // OIDC SignOut sets a 302 redirect to the IdP's end_session_endpoint.
            // Do NOT return a JSON body — let the redirect take effect.
            await context.SignOutAsync(AuthConstants.Schemes.Oidc);
            return Results.Empty;
        }

        return Results.Ok(new { message = "Signed out" });
    }

    // -------------------------------------------------------------------------
    // GET /bff/user
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns the authenticated user's information as JSON.
    /// Always returns HTTP 200 — the storefront must be able to call this without
    /// triggering a redirect. Returns <c>{ isAuthenticated: false }</c> for anonymous requests.
    /// </summary>
    private static IResult HandleUser(HttpContext context)
    {
        var user = context.User;

        if (user.Identity is null || !user.Identity.IsAuthenticated)
            return Results.Ok(new { isAuthenticated = false });

        var roles = user.FindAll(ClaimTypes.Role)
                        .Select(c => c.Value)
                        .ToArray();

        return Results.Ok(new
        {
            isAuthenticated = true,
            userId          = user.FindFirstValue(ClaimTypes.NameIdentifier),
            displayName     = user.FindFirstValue(ClaimTypes.Name),
            email           = user.FindFirstValue(ClaimTypes.Email),
            roles,
            brandSlug       = user.FindFirstValue(AuthConstants.Claims.BrandSlug)
        });
    }

    // -------------------------------------------------------------------------
    // POST /bff/session/keepalive
    // -------------------------------------------------------------------------

    /// <summary>
    /// Slides the session cookie expiry window (requires an authenticated session).
    /// The sliding expiration is handled automatically by ASP.NET Core cookie auth
    /// when configured with <c>SlidingExpiration = true</c>.
    /// </summary>
    private static IResult HandleKeepalive(HttpContext context)
    {
        if (context.User.Identity is null || !context.User.Identity.IsAuthenticated)
            return Results.Unauthorized();

        // Cookie sliding is automatic; touching the endpoint is sufficient.
        return Results.Ok(new { message = "Session extended" });
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Validates and returns a safe returnUrl. Only relative paths are accepted
    /// to prevent open redirect attacks. Falls back to "/" if the value is absent
    /// or looks like an absolute URL.
    /// </summary>
    private static string ResolveReturnUrl(string? returnUrl)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
            return "/";

        // Must start with "/" but not "//" (which would be protocol-relative)
        if (returnUrl.StartsWith('/') && !returnUrl.StartsWith("//"))
            return returnUrl;

        return "/";
    }
}

/// <summary>Marker type for ILogger injection in BFF endpoints (static class cannot be used as type parameter).</summary>
internal sealed class BffEndpointMarker;

using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace TheMillionthFoodOrderApp.Bff.Auth;

/// <summary>
/// Dev-only authentication handler that creates a <see cref="ClaimsPrincipal"/> from a predefined
/// persona name without any real identity provider interaction.
///
/// Only registered when:
///   - <c>ASPNETCORE_ENVIRONMENT=Development</c>
///   - <c>Authentication:UseMockAuth=true</c> in configuration
///
/// Usage: <c>GET /bff/login?mock=brand-admin@frietjes&amp;returnUrl=/admin</c>
/// </summary>
public sealed class MockAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    // Supported persona names
    private static readonly HashSet<string> KnownPersonas =
    [
        MockPersonas.PlatformAdmin,
        MockPersonas.BrandAdminFrietjes,
        MockPersonas.CounterStaffFrietjes,
        MockPersonas.Customer
    ];

    /// <summary>
    /// Builds claims for the requested mock persona and signs the user in via the cookie scheme.
    /// Called from BffEndpoints.MapBffEndpoints — not used as a default authenticate handler.
    /// </summary>
    /// <param name="persona">One of the well-known persona identifiers.</param>
    /// <param name="httpContext">Current HTTP context.</param>
    /// <returns>The resulting <see cref="ClaimsPrincipal"/>, or null if the persona is unknown.</returns>
    public static ClaimsPrincipal? BuildPrincipal(string persona)
    {
        if (!KnownPersonas.Contains(persona))
            return null;

        var claims = persona switch
        {
            MockPersonas.PlatformAdmin => new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "mock-platform-admin"),
                new Claim(ClaimTypes.Name,           "Platform Admin"),
                new Claim(ClaimTypes.Email,          "platform-admin@mock.local"),
                new Claim(ClaimTypes.Role,           AuthConstants.Roles.PlatformAdmin),
                new Claim(AuthConstants.Claims.PlatformRole, AuthConstants.Roles.PlatformAdmin),
                new Claim(AuthConstants.Claims.GivenName,   "Platform"),
                new Claim(AuthConstants.Claims.FamilyName,  "Admin"),
                new Claim(AuthConstants.Claims.PhoneNumber, "+32470000001")
            },

            MockPersonas.BrandAdminFrietjes => new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "mock-brand-admin-frietjes"),
                new Claim(ClaimTypes.Name,           "Brand Admin (Frietjes)"),
                new Claim(ClaimTypes.Email,          "brand-admin@frietjes.mock.local"),
                new Claim(ClaimTypes.Role,           AuthConstants.Roles.BrandAdmin),
                new Claim(AuthConstants.Claims.BrandSlug,  "frietjes"),
                new Claim(AuthConstants.Claims.BrandRoles, "frietjes:BrandAdmin"),
                new Claim(AuthConstants.Claims.GivenName,   "Brand"),
                new Claim(AuthConstants.Claims.FamilyName,  "Admin"),
                new Claim(AuthConstants.Claims.PhoneNumber, "+32470000002")
            },

            MockPersonas.CounterStaffFrietjes => new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "mock-counter-staff-frietjes"),
                new Claim(ClaimTypes.Name,           "Counter Staff (Frietjes)"),
                new Claim(ClaimTypes.Email,          "counter-staff@frietjes.mock.local"),
                new Claim(ClaimTypes.Role,           AuthConstants.Roles.CounterStaff),
                new Claim(AuthConstants.Claims.BrandSlug,  "frietjes"),
                new Claim(AuthConstants.Claims.BrandRoles, "frietjes:CounterStaff"),
                new Claim(AuthConstants.Claims.GivenName,   "Counter"),
                new Claim(AuthConstants.Claims.FamilyName,  "Staff"),
                new Claim(AuthConstants.Claims.PhoneNumber, "+32470000003")
            },

            MockPersonas.Customer => new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "mock-customer"),
                new Claim(ClaimTypes.Name,           "Test Customer"),
                new Claim(ClaimTypes.Email,          "customer@mock.local"),
                new Claim(ClaimTypes.Role,           AuthConstants.Roles.Customer),
                new Claim(AuthConstants.Claims.GivenName,   "Test"),
                new Claim(AuthConstants.Claims.FamilyName,  "Customer"),
                new Claim(AuthConstants.Claims.PhoneNumber, "+32470000004")
            },

            _ => Array.Empty<Claim>()
        };

        var identity  = new ClaimsIdentity(claims, AuthConstants.Schemes.Mock);
        return new ClaimsPrincipal(identity);
    }

    /// <inheritdoc/>
    /// <remarks>
    /// The mock handler is only used to issue the sign-in cookie via the login endpoint.
    /// It does not handle inbound request authentication — the cookie scheme does that.
    /// Returning NoResult here ensures the cookie scheme remains the active default.
    /// </remarks>
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        => Task.FromResult(AuthenticateResult.NoResult());
}

/// <summary>Well-known persona identifiers accepted by the mock login endpoint.</summary>
public static class MockPersonas
{
    public const string PlatformAdmin         = "platform-admin";
    public const string BrandAdminFrietjes    = "brand-admin@frietjes";
    public const string CounterStaffFrietjes  = "counter-staff@frietjes";
    public const string Customer              = "customer";
}

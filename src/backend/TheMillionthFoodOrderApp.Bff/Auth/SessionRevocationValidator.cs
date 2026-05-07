using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;

namespace TheMillionthFoodOrderApp.Bff.Auth;

/// <summary>
/// Periodically introspects the stored access token against the OIDC provider
/// so that admin-side user disable / token revocation takes effect within a
/// bounded window without polling on every request.
///
/// Wired into <see cref="CookieAuthenticationOptions.Events"/>'s
/// <see cref="CookieAuthenticationEvents.OnValidatePrincipal"/>. Disabled for
/// mock auth (no introspection endpoint, no real tokens).
/// </summary>
public sealed class SessionRevocationValidator(
    IHttpClientFactory httpClientFactory,
    IOptionsMonitor<OpenIdConnectOptions> oidcOptionsMonitor,
    ILogger<SessionRevocationValidator> logger)
{
    /// <summary>How long between introspection calls. 5 min keeps Keycloak load minimal.</summary>
    private static readonly TimeSpan ValidationInterval = TimeSpan.FromMinutes(5);

    private const string LastValidatedKey = "session.last_validated";

    public async Task ValidateAsync(CookieValidatePrincipalContext context)
    {
        var accessToken = context.Properties.GetTokenValue("access_token");
        if (string.IsNullOrEmpty(accessToken))
        {
            // Mock auth or pre-OIDC sessions don't store tokens — skip silently.
            return;
        }

        var lastValidatedRaw = context.Properties.GetString(LastValidatedKey);
        if (DateTimeOffset.TryParse(lastValidatedRaw, CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind, out var lastValidated) &&
            DateTimeOffset.UtcNow - lastValidated < ValidationInterval)
        {
            return;
        }

        var active = await IsTokenActiveAsync(context.HttpContext, accessToken);
        if (active is false)
        {
            logger.LogInformation("Session rejected — introspection reported access token inactive");
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync(AuthConstants.Schemes.Cookie);
            return;
        }

        // Either confirmed active (true) or introspection unreachable (null).
        // In both cases we mark the session as recently validated to avoid
        // hammering the IdP when it's flaky; a hard failure on the IdP doesn't
        // log everyone out.
        context.Properties.SetString(
            LastValidatedKey,
            DateTimeOffset.UtcNow.ToString("o", CultureInfo.InvariantCulture));
        context.ShouldRenew = true;
    }

    private async Task<bool?> IsTokenActiveAsync(HttpContext httpContext, string accessToken)
    {
        var oidcOptions = oidcOptionsMonitor.Get(AuthConstants.Schemes.Oidc);
        if (oidcOptions.ConfigurationManager is null)
            return null;

        try
        {
            var configuration = await oidcOptions.ConfigurationManager.GetConfigurationAsync(httpContext.RequestAborted);

            // The standard introspection endpoint is rarely advertised in OIDC
            // discovery — derive it from the token endpoint by convention.
            var introspectionEndpoint = configuration.AdditionalData.TryGetValue(
                                            "introspection_endpoint", out var advertised) && advertised is string url
                ? url
                : configuration.TokenEndpoint?.Replace("/token", "/token/introspect", StringComparison.Ordinal);

            if (string.IsNullOrEmpty(introspectionEndpoint))
                return null;

            var http = httpClientFactory.CreateClient();
            using var request = new HttpRequestMessage(HttpMethod.Post, introspectionEndpoint)
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["token"]         = accessToken,
                    ["client_id"]     = oidcOptions.ClientId!,
                    ["client_secret"] = oidcOptions.ClientSecret!,
                })
            };
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            using var response = await http.SendAsync(request, httpContext.RequestAborted);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogDebug("Introspection HTTP {StatusCode} — treating session as still valid", (int)response.StatusCode);
                return null;
            }

            var payload = await response.Content.ReadFromJsonAsync<IntrospectionResponse>(httpContext.RequestAborted);
            return payload?.Active;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Introspection failed; leaving session intact");
            return null;
        }
    }

    private sealed record IntrospectionResponse(
        [property: System.Text.Json.Serialization.JsonPropertyName("active")] bool Active);
}

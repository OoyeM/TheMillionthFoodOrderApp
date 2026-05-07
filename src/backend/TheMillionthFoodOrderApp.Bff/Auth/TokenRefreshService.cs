using System.Collections.Concurrent;
using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Extensions.Options;

namespace TheMillionthFoodOrderApp.Bff.Auth;

/// <summary>
/// Refreshes the OIDC access token stored in the cookie session before it
/// expires, so YARP-proxied API calls always carry a valid bearer token.
///
/// The refresh is single-flight per <see cref="ClaimsPrincipal"/> to prevent
/// stampedes when many concurrent API calls arrive on the same session
/// immediately after token expiry.
/// </summary>
public sealed class TokenRefreshService(
    IHttpClientFactory httpClientFactory,
    IOptionsMonitor<OpenIdConnectOptions> oidcOptionsMonitor,
    ILogger<TokenRefreshService> logger)
{
    /// <summary>Refresh slightly before expiry so concurrent in-flight requests don't race the boundary.</summary>
    private static readonly TimeSpan RefreshSkew = TimeSpan.FromSeconds(60);

    private static readonly ConcurrentDictionary<string, SemaphoreSlim> Locks = new();

    /// <summary>
    /// Returns a non-expired access token for the current session, refreshing
    /// it via the OIDC token endpoint if less than 60 s remains. Returns null
    /// if no token is stored or the refresh fails.
    /// </summary>
    public async Task<string?> GetFreshAccessTokenAsync(HttpContext context)
    {
        var accessToken = await context.GetTokenAsync("access_token");
        if (string.IsNullOrEmpty(accessToken))
            return null;

        var expiresAtRaw = await context.GetTokenAsync("expires_at");
        if (DateTimeOffset.TryParse(expiresAtRaw, CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind, out var expiresAt) &&
            expiresAt - DateTimeOffset.UtcNow > RefreshSkew)
        {
            return accessToken;
        }

        var refreshToken = await context.GetTokenAsync("refresh_token");
        if (string.IsNullOrEmpty(refreshToken))
        {
            logger.LogDebug("Access token near/past expiry but no refresh_token stored — returning expired token");
            return accessToken;
        }

        var sessionKey = context.User.FindFirstValue(ClaimTypes.NameIdentifier)
                         ?? context.User.FindFirstValue("sub")
                         ?? "anonymous";

        var gate = Locks.GetOrAdd(sessionKey, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(context.RequestAborted);
        try
        {
            // Re-check after acquiring the lock — a concurrent refresh may have
            // already updated the cookie.
            accessToken = await context.GetTokenAsync("access_token");
            expiresAtRaw = await context.GetTokenAsync("expires_at");
            if (DateTimeOffset.TryParse(expiresAtRaw, CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind, out expiresAt) &&
                expiresAt - DateTimeOffset.UtcNow > RefreshSkew)
            {
                return accessToken;
            }

            return await RefreshAndPersistAsync(context, refreshToken!);
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task<string?> RefreshAndPersistAsync(HttpContext context, string refreshToken)
    {
        var oidcOptions = oidcOptionsMonitor.Get(AuthConstants.Schemes.Oidc);
        var configuration = await oidcOptions.ConfigurationManager!
            .GetConfigurationAsync(context.RequestAborted);

        var http = httpClientFactory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, configuration.TokenEndpoint)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"]    = "refresh_token",
                ["refresh_token"] = refreshToken,
                ["client_id"]     = oidcOptions.ClientId!,
                ["client_secret"] = oidcOptions.ClientSecret!,
            })
        };
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await http.SendAsync(request, context.RequestAborted);
        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "Token refresh failed: HTTP {StatusCode}. Session will continue with the stale token until next request.",
                (int)response.StatusCode);
            return await context.GetTokenAsync("access_token");
        }

        var payload = await response.Content.ReadFromJsonAsync<TokenResponse>(context.RequestAborted);
        if (payload is null || string.IsNullOrEmpty(payload.AccessToken))
        {
            logger.LogWarning("Token refresh response was empty or missing access_token");
            return await context.GetTokenAsync("access_token");
        }

        var authenticateResult = await context.AuthenticateAsync(AuthConstants.Schemes.Cookie);
        if (!authenticateResult.Succeeded || authenticateResult.Properties is null)
            return payload.AccessToken;

        var newExpiresAt = DateTimeOffset.UtcNow.AddSeconds(payload.ExpiresIn);
        var newTokens = new List<AuthenticationToken>
        {
            new() { Name = "access_token",  Value = payload.AccessToken },
            new() { Name = "expires_at",    Value = newExpiresAt.ToString("o", CultureInfo.InvariantCulture) },
        };
        if (!string.IsNullOrEmpty(payload.RefreshToken))
            newTokens.Add(new AuthenticationToken { Name = "refresh_token", Value = payload.RefreshToken });
        if (!string.IsNullOrEmpty(payload.IdToken))
            newTokens.Add(new AuthenticationToken { Name = "id_token", Value = payload.IdToken });
        if (!string.IsNullOrEmpty(payload.TokenType))
            newTokens.Add(new AuthenticationToken { Name = "token_type", Value = payload.TokenType });

        authenticateResult.Properties.StoreTokens(newTokens);

        // Re-issue the cookie so the new tokens are persisted on the next response.
        await context.SignInAsync(
            AuthConstants.Schemes.Cookie,
            authenticateResult.Principal,
            authenticateResult.Properties);

        logger.LogDebug("Refreshed access token; new expiry {Expiry:o}", newExpiresAt);
        return payload.AccessToken;
    }

    private sealed record TokenResponse(
        [property: System.Text.Json.Serialization.JsonPropertyName("access_token")]  string  AccessToken,
        [property: System.Text.Json.Serialization.JsonPropertyName("expires_in")]    int     ExpiresIn,
        [property: System.Text.Json.Serialization.JsonPropertyName("refresh_token")] string? RefreshToken,
        [property: System.Text.Json.Serialization.JsonPropertyName("id_token")]      string? IdToken,
        [property: System.Text.Json.Serialization.JsonPropertyName("token_type")]    string? TokenType);
}

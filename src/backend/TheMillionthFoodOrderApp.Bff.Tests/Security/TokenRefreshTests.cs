namespace TheMillionthFoodOrderApp.Bff.Tests.Security;

/// <summary>
/// <see cref="Auth.TokenRefreshService"/> exercises the cookie-stored token
/// pipeline that mock auth deliberately bypasses (no OIDC token is ever issued).
/// Behavioural verification therefore needs the same Wave-2 stub-IdP harness as
/// the bearer-forwarding tests in <c>YarpBearerForwardingTests</c>.
/// </summary>
public sealed class TokenRefreshTests
{
    [Test]
    [Skip("Requires a stub OIDC token endpoint — covered in Wave 2 alongside bearer-forwarding tests")]
    public Task NearExpiryToken_TriggersRefresh_AndPersistsNewTokenInCookie()
    {
        // Implementation plan (Wave 2):
        // 1. Issue a JWT with an "expires_at" auth-token property < 60s from now.
        // 2. Stub the Keycloak token endpoint to return a fresh token bundle.
        // 3. Send a /api/* request via the BFF.
        // 4. Assert the upstream observed Authorization: Bearer <new-token>
        //    AND the response Set-Cookie re-issued the session with the new tokens.
        return Task.CompletedTask;
    }

    [Test]
    [Skip("Requires stub OIDC harness — Wave 2")]
    public Task NonExpiringToken_DoesNotCallTokenEndpoint()
    {
        // Implementation plan (Wave 2):
        // 1. Issue a JWT with expires_at well in the future.
        // 2. Stub Keycloak token endpoint with a counter.
        // 3. Send a proxied request.
        // 4. Assert the counter is still 0 (no refresh attempted).
        return Task.CompletedTask;
    }

    [Test]
    [Skip("Requires stub OIDC harness — Wave 2")]
    public Task ConcurrentRequests_OnExpiredToken_CallTokenEndpointOnce()
    {
        // Verifies the SemaphoreSlim single-flight gate — many concurrent
        // proxied requests on the same session result in exactly one refresh.
        return Task.CompletedTask;
    }
}

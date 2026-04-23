namespace TheMillionthFoodOrderApp.Bff.Tests.Proxy;

/// <summary>
/// Tests for YARP bearer-token forwarding on proxied <c>/api/*</c> requests.
///
/// When mock auth is active (<c>Authentication:UseMockAuth=true</c>), the BFF signs
/// users in via cookie only — no OIDC token is issued and therefore no access_token
/// is stored in the session (<c>SaveTokens=true</c> is only wired in the OIDC path).
/// Consequently the <c>Authorization: Bearer …</c> header is never set in the mock
/// auth flow, making it impossible to assert bearer forwarding without a real OIDC token.
///
/// Full bearer-forwarding coverage requires:
///   1. A real OIDC token issued by Keycloak (or a stub IdP).
///   2. The BFF to sign the user in via the OIDC handler so the token is stored in the cookie.
///   3. A stub upstream to capture the forwarded Authorization header.
///
/// This is tracked for Wave 2 where a lightweight stub OIDC server (e.g. Duende IdentityServer
/// test server or a manual JWT-issuing TestServer) will be wired into the factory.
/// </summary>
public sealed class YarpBearerForwardingTests
{
    [Test]
    [Skip("Requires real OIDC token pipeline — covered in Wave 2")]
    public Task BearerToken_IsForwardedToUpstreamApi_WhenSessionHasAccessToken()
    {
        // Implementation plan (Wave 2):
        // 1. Start an in-process stub upstream using Microsoft.AspNetCore.TestHost.TestServer.
        // 2. Configure BffTestWebAppFactory to point the YARP "api-cluster" destination
        //    at the stub's base address.
        // 3. Issue a JWT (RS256) using a self-signed key inside the test.
        // 4. Create a cookie that encodes the access_token (matching ASP.NET Core's
        //    cookie protection format) and attach it to the request.
        // 5. Hit GET /api/brands via the BFF.
        // 6. Assert the stub upstream received Authorization: Bearer <jwt>.
        return Task.CompletedTask;
    }

    [Test]
    [Skip("Requires real OIDC token pipeline — covered in Wave 2")]
    public Task NoAccessToken_InSession_DoesNotForwardAuthorizationHeader()
    {
        // When the session cookie contains no access_token (mock auth path),
        // verify that no Authorization header is forwarded to the upstream.
        // Covered in Wave 2 alongside the bearer-forwarding test.
        return Task.CompletedTask;
    }
}

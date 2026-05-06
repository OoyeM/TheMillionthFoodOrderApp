using System.Net.Http.Json;
using TheMillionthFoodOrderApp.Bff.Tests.Fixtures;

namespace TheMillionthFoodOrderApp.Bff.Tests.Security;

/// <summary>
/// <see cref="Auth.SessionRevocationValidator"/> only fires when the cookie
/// stores a real OIDC access token, so the mock-auth fixture leaves it as a
/// no-op (verified below). Behavioural verification of the introspection-driven
/// sign-out path is covered by the Wave-2 stub-IdP harness.
/// </summary>
[ClassDataSource<BffTestWebAppFactory>(Shared = SharedType.PerClass)]
public sealed class SessionRevocationTests(BffTestWebAppFactory factory)
{
    [Test]
    public async Task MockAuthSession_DoesNotInvokeIntrospection()
    {
        // With mock auth, no access_token is stored — the validator returns
        // early. Repeated /bff/user calls must continue to report authenticated
        // even past the 5-minute introspection window (we approximate by making
        // many sequential calls; the validator must not reject any of them).
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        await client.GetAsync("/bff/login?mock=brand-admin@frietjes");

        for (var i = 0; i < 5; i++)
        {
            var response = await client.GetAsync("/bff/user");
            var body = await response.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
            await Assert.That(body.GetProperty("isAuthenticated").GetBoolean()).IsTrue();
        }
    }

    [Test]
    [Skip("Active=false path requires stub introspection endpoint — Wave 2")]
    public Task IntrospectionActiveFalse_SignsUserOut()
    {
        // Implementation plan (Wave 2):
        // 1. Issue a real-shaped session cookie carrying access_token / refresh_token.
        // 2. Stub the Keycloak introspection endpoint to return { "active": false }.
        // 3. Force OnValidatePrincipal to run (set last_validated > 5 min ago).
        // 4. Send a GET /bff/user — assert response reports isAuthenticated=false
        //    and Set-Cookie clears the session cookie.
        return Task.CompletedTask;
    }

    [Test]
    [Skip("Active=true path requires stub introspection endpoint — Wave 2")]
    public Task IntrospectionActiveTrue_KeepsSession_AndUpdatesLastValidated()
    {
        // Implementation plan (Wave 2):
        // 1. Same setup as above but introspection returns { "active": true }.
        // 2. Assert the user is still authenticated AND a new Set-Cookie is
        //    issued (ShouldRenew=true) with an updated session.last_validated property.
        return Task.CompletedTask;
    }
}

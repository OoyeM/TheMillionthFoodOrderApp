using System.Net;
using System.Text.Json;
using TheMillionthFoodOrderApp.Bff.Tests.Fixtures;

namespace TheMillionthFoodOrderApp.Bff.Tests.Endpoints;

/// <summary>
/// Integration tests for <c>GET /bff/user</c>.
/// Uses <see cref="BffTestWebAppFactory"/> — no database or Keycloak required.
/// </summary>
[ClassDataSource<BffTestWebAppFactory>(Shared = SharedType.PerClass)]
public sealed class UserEndpointTests(BffTestWebAppFactory factory)
{
    // ── Anonymous ─────────────────────────────────────────────────────────────

    [Test]
    public async Task GetUser_Anonymous_Returns200WithIsAuthenticatedFalse()
    {
        // Arrange
        var client = factory.CreateClient(new()
        {
            // Do not follow redirects — endpoint should return 200 for anonymous
            AllowAutoRedirect = false,
        });

        // Act
        var response = await client.GetAsync("/bff/user");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var body = await ParseJsonAsync(response);
        await Assert.That(body.GetProperty("isAuthenticated").GetBoolean()).IsFalse();
    }

    // ── Authenticated ─────────────────────────────────────────────────────────

    [Test]
    public async Task GetUser_AfterMockLogin_Returns200WithUserInfo()
    {
        // Arrange — first sign in via the mock login endpoint to get a session cookie
        var client = factory.CreateClient(new()
        {
            AllowAutoRedirect = false,
        });

        // Sign in as brand-admin@frietjes; the response sets a bff_session cookie
        var loginResponse = await client.GetAsync("/bff/login?mock=brand-admin@frietjes");
        await Assert.That(loginResponse.StatusCode).IsEqualTo(HttpStatusCode.Redirect);

        // The HttpClient automatically stores cookies via CookieContainer when
        // we use CreateClient() — the session cookie is included on subsequent calls.

        // Act
        var userResponse = await client.GetAsync("/bff/user");

        // Assert
        await Assert.That(userResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var body = await ParseJsonAsync(userResponse);
        await Assert.That(body.GetProperty("isAuthenticated").GetBoolean()).IsTrue();
        await Assert.That(body.GetProperty("displayName").GetString()).IsEqualTo("Brand Admin (Frietjes)");
        await Assert.That(body.GetProperty("email").GetString()).IsEqualTo("brand-admin@frietjes.mock.local");

        // Roles must contain BrandAdmin
        var roles = body.GetProperty("roles").EnumerateArray()
                        .Select(r => r.GetString())
                        .ToArray();
        await Assert.That(roles).Contains("BrandAdmin");

        await Assert.That(body.GetProperty("brandSlug").GetString()).IsEqualTo("frietjes");
    }

    [Test]
    public async Task GetUser_AfterMockLoginAsPlatformAdmin_HasPlatformAdminRole()
    {
        // Arrange
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        await client.GetAsync("/bff/login?mock=platform-admin");

        // Act
        var userResponse = await client.GetAsync("/bff/user");

        // Assert
        await Assert.That(userResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var body = await ParseJsonAsync(userResponse);
        await Assert.That(body.GetProperty("isAuthenticated").GetBoolean()).IsTrue();

        var roles = body.GetProperty("roles").EnumerateArray()
                        .Select(r => r.GetString())
                        .ToArray();
        await Assert.That(roles).Contains("PlatformAdmin");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static async Task<JsonElement> ParseJsonAsync(HttpResponseMessage response)
    {
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.Clone();
    }
}

using System.Net;
using System.Text.Json;
using Shouldly;
using TheMillionthFoodOrderApp.Bff.Tests.Fixtures;

namespace TheMillionthFoodOrderApp.Bff.Tests.Endpoints;

/// <summary>
/// Integration tests for <c>GET /bff/user</c>.
/// Uses <see cref="BffTestWebAppFactory"/> — no database or Keycloak required.
/// </summary>
public sealed class UserEndpointTests(BffTestWebAppFactory factory)
    : IClassFixture<BffTestWebAppFactory>
{
    // ── Anonymous ─────────────────────────────────────────────────────────────

    [Fact]
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
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var body = await ParseJsonAsync(response);
        body.GetProperty("isAuthenticated").GetBoolean().ShouldBeFalse();
    }

    // ── Authenticated ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetUser_AfterMockLogin_Returns200WithUserInfo()
    {
        // Arrange — first sign in via the mock login endpoint to get a session cookie
        var client = factory.CreateClient(new()
        {
            AllowAutoRedirect = false,
        });

        // Sign in as brand-admin@frietjes; the response sets a bff_session cookie
        var loginResponse = await client.GetAsync("/bff/login?mock=brand-admin@frietjes");
        loginResponse.StatusCode.ShouldBe(HttpStatusCode.Redirect);

        // The HttpClient automatically stores cookies via CookieContainer when
        // we use CreateClient() — the session cookie is included on subsequent calls.

        // Act
        var userResponse = await client.GetAsync("/bff/user");

        // Assert
        userResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var body = await ParseJsonAsync(userResponse);
        body.GetProperty("isAuthenticated").GetBoolean().ShouldBeTrue();
        body.GetProperty("displayName").GetString().ShouldBe("Brand Admin (Frietjes)");
        body.GetProperty("email").GetString().ShouldBe("brand-admin@frietjes.mock.local");

        // Roles must contain BrandAdmin
        var roles = body.GetProperty("roles").EnumerateArray()
                        .Select(r => r.GetString())
                        .ToArray();
        roles.ShouldContain("BrandAdmin");

        body.GetProperty("brandSlug").GetString().ShouldBe("frietjes");
    }

    [Fact]
    public async Task GetUser_AfterMockLoginAsPlatformAdmin_HasPlatformAdminRole()
    {
        // Arrange
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        await client.GetAsync("/bff/login?mock=platform-admin");

        // Act
        var userResponse = await client.GetAsync("/bff/user");

        // Assert
        userResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var body = await ParseJsonAsync(userResponse);
        body.GetProperty("isAuthenticated").GetBoolean().ShouldBeTrue();

        var roles = body.GetProperty("roles").EnumerateArray()
                        .Select(r => r.GetString())
                        .ToArray();
        roles.ShouldContain("PlatformAdmin");
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static async Task<JsonElement> ParseJsonAsync(HttpResponseMessage response)
    {
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.Clone();
    }
}

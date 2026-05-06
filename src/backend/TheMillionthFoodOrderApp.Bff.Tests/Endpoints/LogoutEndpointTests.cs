using System.Net;
using System.Net.Http.Json;
using TheMillionthFoodOrderApp.Bff.Tests.Fixtures;

namespace TheMillionthFoodOrderApp.Bff.Tests.Endpoints;

/// <summary>
/// Integration tests for <c>POST /bff/logout</c>.
/// </summary>
[ClassDataSource<BffTestWebAppFactory>(Shared = SharedType.PerClass)]
public sealed class LogoutEndpointTests(BffTestWebAppFactory factory)
{
    private static HttpRequestMessage LogoutRequest()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/bff/logout");
        request.Headers.Add("X-CSRF", "1");
        return request;
    }

    [Test]
    public async Task Logout_AfterLogin_Returns200()
    {
        // Arrange — sign in first so a session exists
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        await client.GetAsync("/bff/login?mock=brand-admin@frietjes");

        // Act
        var response = await client.SendAsync(LogoutRequest());

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task Logout_AfterLogin_SessionIsInvalidated()
    {
        // Arrange
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        await client.GetAsync("/bff/login?mock=brand-admin@frietjes");

        // Confirm the session was established before we test logout
        var beforeLogout = await client.GetAsync("/bff/user");
        var beforeBody = await beforeLogout.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        await Assert.That(beforeBody.GetProperty("isAuthenticated").GetBoolean()).IsTrue();

        // Act
        await client.SendAsync(LogoutRequest());

        // Assert — /bff/user must now report anonymous; this is the definitive behavioral check
        // that the session was cleared (regardless of how the cookie header is formatted).
        var afterLogout = await client.GetAsync("/bff/user");
        var afterBody = await afterLogout.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        await Assert.That(afterBody.GetProperty("isAuthenticated").GetBoolean()).IsFalse();
    }

    [Test]
    public async Task Logout_WhenNotLoggedIn_Returns200()
    {
        // Arrange — fresh client with no session
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });

        // Act — logout without being logged in should still succeed.
        // Anonymous calls bypass the CSRF check (no session to forge against).
        var response = await client.PostAsync("/bff/logout", content: null);

        // Assert — 200 OK (SignOutAsync is idempotent)
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }
}

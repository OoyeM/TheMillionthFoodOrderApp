using System.Net;
using System.Net.Http.Json;
using Shouldly;
using TheMillionthFoodOrderApp.Bff.Tests.Fixtures;

namespace TheMillionthFoodOrderApp.Bff.Tests.Endpoints;

/// <summary>
/// Integration tests for <c>POST /bff/logout</c>.
/// </summary>
public sealed class LogoutEndpointTests(BffTestWebAppFactory factory)
    : IClassFixture<BffTestWebAppFactory>
{
    [Fact]
    public async Task Logout_AfterLogin_Returns200()
    {
        // Arrange — sign in first so a session exists
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        await client.GetAsync("/bff/login?mock=brand-admin@frietjes");

        // Act
        var response = await client.PostAsync("/bff/logout", content: null);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Logout_AfterLogin_SessionIsInvalidated()
    {
        // Arrange
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        await client.GetAsync("/bff/login?mock=brand-admin@frietjes");

        // Confirm the session was established before we test logout
        var beforeLogout = await client.GetAsync("/bff/user");
        var beforeBody = await beforeLogout.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        beforeBody.GetProperty("isAuthenticated").GetBoolean().ShouldBeTrue();

        // Act
        await client.PostAsync("/bff/logout", content: null);

        // Assert — /bff/user must now report anonymous; this is the definitive behavioral check
        // that the session was cleared (regardless of how the cookie header is formatted).
        var afterLogout = await client.GetAsync("/bff/user");
        var afterBody = await afterLogout.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>();
        afterBody.GetProperty("isAuthenticated").GetBoolean().ShouldBeFalse();
    }

    [Fact]
    public async Task Logout_WhenNotLoggedIn_Returns200()
    {
        // Arrange — fresh client with no session
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });

        // Act — logout without being logged in should still succeed
        var response = await client.PostAsync("/bff/logout", content: null);

        // Assert — 200 OK (SignOutAsync is idempotent)
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }
}

using System.Net;
using TheMillionthFoodOrderApp.Bff.Tests.Fixtures;

namespace TheMillionthFoodOrderApp.Bff.Tests.Endpoints;

/// <summary>
/// Integration tests for <c>POST /bff/session/keepalive</c>.
/// </summary>
[ClassDataSource<BffTestWebAppFactory>(Shared = SharedType.PerClass)]
public sealed class KeepaliveEndpointTests(BffTestWebAppFactory factory)
{
    [Test]
    public async Task Keepalive_WithoutCookie_Returns401()
    {
        // Arrange — fresh client, no session cookie
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });

        // Act
        var response = await client.PostAsync("/bff/session/keepalive", content: null);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Keepalive_WithMockPersonaCookie_Returns200()
    {
        // Arrange — sign in to get a session
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        await client.GetAsync("/bff/login?mock=brand-admin@frietjes");

        // Act
        var response = await client.PostAsync("/bff/session/keepalive", content: null);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    [Arguments("platform-admin")]
    [Arguments("brand-admin@frietjes")]
    [Arguments("counter-staff@frietjes")]
    [Arguments("customer")]
    public async Task Keepalive_AllPersonas_Returns200(string persona)
    {
        // Arrange
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        await client.GetAsync($"/bff/login?mock={Uri.EscapeDataString(persona)}");

        // Act
        var response = await client.PostAsync("/bff/session/keepalive", content: null);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }
}

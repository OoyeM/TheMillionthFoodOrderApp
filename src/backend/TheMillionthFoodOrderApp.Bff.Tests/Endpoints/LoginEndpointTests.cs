using System.Net;
using TheMillionthFoodOrderApp.Bff.Tests.Fixtures;

namespace TheMillionthFoodOrderApp.Bff.Tests.Endpoints;

/// <summary>
/// Integration tests for <c>GET /bff/login</c>.
/// Covers mock auth flow only — Keycloak OIDC tests are skipped (see note below).
/// </summary>
[ClassDataSource<BffTestWebAppFactory>(Shared = SharedType.PerClass)]
public sealed class LoginEndpointTests(BffTestWebAppFactory factory)
{
    // ── Mock login — happy path ───────────────────────────────────────────────

    [Test]
    public async Task Login_WithValidMockPersona_Returns302Redirect()
    {
        // Arrange
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });

        // Act
        var response = await client.GetAsync("/bff/login?mock=brand-admin@frietjes");

        // Assert — mock login signs the user in and redirects to returnUrl (defaults to "/")
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Redirect);
    }

    [Test]
    public async Task Login_WithValidMockPersona_SetsBffSessionCookie()
    {
        // Arrange
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });

        // Act
        var response = await client.GetAsync("/bff/login?mock=brand-admin@frietjes");

        // Assert — a bff_session cookie must be present in Set-Cookie
        var setCookie = response.Headers.GetValues("Set-Cookie");
        await Assert.That(setCookie).Contains(cookie => cookie.StartsWith("bff_session=", StringComparison.OrdinalIgnoreCase));
    }

    [Test]
    public async Task Login_WithReturnUrl_RedirectsToReturnUrl()
    {
        // Arrange
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });

        // Act
        var response = await client.GetAsync("/bff/login?mock=brand-admin@frietjes&returnUrl=/admin");

        // Assert — Location header points to /admin
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Redirect);
        await Assert.That(response.Headers.Location?.ToString()).IsEqualTo("/admin");
    }

    [Test]
    public async Task Login_WithDefaultReturnUrl_RedirectsToRoot()
    {
        // Arrange
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });

        // Act — no returnUrl query param
        var response = await client.GetAsync("/bff/login?mock=platform-admin");

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Redirect);
        await Assert.That(response.Headers.Location?.ToString()).IsEqualTo("/");
    }

    // ── ReturnUrl validation ──────────────────────────────────────────────────

    [Test]
    [Arguments("https://evil.example.com/steal")]
    [Arguments("http://attacker.io")]
    [Arguments("//attacker.io/path")]
    public async Task Login_WithExternalReturnUrl_FallsBackToRoot(string externalUrl)
    {
        // Arrange — ResolveReturnUrl in BffEndpoints rejects non-relative URLs
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        var encoded = Uri.EscapeDataString(externalUrl);

        // Act
        var response = await client.GetAsync($"/bff/login?mock=platform-admin&returnUrl={encoded}");

        // Assert — must redirect to "/" not the external URL
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Redirect);
        await Assert.That(response.Headers.Location?.ToString()).IsEqualTo("/");
    }

    // ── Unknown persona ───────────────────────────────────────────────────────

    [Test]
    public async Task Login_WithUnknownMockPersona_Returns400()
    {
        // Arrange
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });

        // Act
        var response = await client.GetAsync("/bff/login?mock=unknown-persona");

        // Assert — BffEndpoints returns BadRequest for unrecognised personas
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    // ── Keycloak OIDC path ────────────────────────────────────────────────────

    [Test]
    [Skip("Requires Docker/Keycloak — covered in Wave 2")]
    public Task Login_WithoutMockFlag_TriggersOidcChallenge()
    {
        // When Authentication:UseMockAuth=false the endpoint returns an OIDC
        // challenge (302 to Keycloak). This needs a running Keycloak container.
        return Task.CompletedTask;
    }
}

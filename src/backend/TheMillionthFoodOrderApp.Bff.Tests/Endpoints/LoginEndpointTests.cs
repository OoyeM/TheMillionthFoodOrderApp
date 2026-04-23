using System.Net;
using Shouldly;
using TheMillionthFoodOrderApp.Bff.Tests.Fixtures;

namespace TheMillionthFoodOrderApp.Bff.Tests.Endpoints;

/// <summary>
/// Integration tests for <c>GET /bff/login</c>.
/// Covers mock auth flow only — Keycloak OIDC tests are skipped (see note below).
/// </summary>
public sealed class LoginEndpointTests(BffTestWebAppFactory factory)
    : IClassFixture<BffTestWebAppFactory>
{
    // ── Mock login — happy path ───────────────────────────────────────────────

    [Fact]
    public async Task Login_WithValidMockPersona_Returns302Redirect()
    {
        // Arrange
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });

        // Act
        var response = await client.GetAsync("/bff/login?mock=brand-admin@frietjes");

        // Assert — mock login signs the user in and redirects to returnUrl (defaults to "/")
        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
    }

    [Fact]
    public async Task Login_WithValidMockPersona_SetsBffSessionCookie()
    {
        // Arrange
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });

        // Act
        var response = await client.GetAsync("/bff/login?mock=brand-admin@frietjes");

        // Assert — a bff_session cookie must be present in Set-Cookie
        var setCookie = response.Headers.GetValues("Set-Cookie");
        setCookie.ShouldContain(cookie => cookie.StartsWith("bff_session=", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Login_WithReturnUrl_RedirectsToReturnUrl()
    {
        // Arrange
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });

        // Act
        var response = await client.GetAsync("/bff/login?mock=brand-admin@frietjes&returnUrl=/admin");

        // Assert — Location header points to /admin
        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().ShouldBe("/admin");
    }

    [Fact]
    public async Task Login_WithDefaultReturnUrl_RedirectsToRoot()
    {
        // Arrange
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });

        // Act — no returnUrl query param
        var response = await client.GetAsync("/bff/login?mock=platform-admin");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().ShouldBe("/");
    }

    // ── ReturnUrl validation ──────────────────────────────────────────────────

    [Theory]
    [InlineData("https://evil.example.com/steal")]
    [InlineData("http://attacker.io")]
    [InlineData("//attacker.io/path")]
    public async Task Login_WithExternalReturnUrl_FallsBackToRoot(string externalUrl)
    {
        // Arrange — ResolveReturnUrl in BffEndpoints rejects non-relative URLs
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        var encoded = Uri.EscapeDataString(externalUrl);

        // Act
        var response = await client.GetAsync($"/bff/login?mock=platform-admin&returnUrl={encoded}");

        // Assert — must redirect to "/" not the external URL
        response.StatusCode.ShouldBe(HttpStatusCode.Redirect);
        response.Headers.Location?.ToString().ShouldBe("/");
    }

    // ── Unknown persona ───────────────────────────────────────────────────────

    [Fact]
    public async Task Login_WithUnknownMockPersona_Returns400()
    {
        // Arrange
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });

        // Act
        var response = await client.GetAsync("/bff/login?mock=unknown-persona");

        // Assert — BffEndpoints returns BadRequest for unrecognised personas
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // ── Keycloak OIDC path ────────────────────────────────────────────────────

    [Fact(Skip = "Requires Docker/Keycloak — covered in Wave 2")]
    public Task Login_WithoutMockFlag_TriggersOidcChallenge()
    {
        // When Authentication:UseMockAuth=false the endpoint returns an OIDC
        // challenge (302 to Keycloak). This needs a running Keycloak container.
        return Task.CompletedTask;
    }
}

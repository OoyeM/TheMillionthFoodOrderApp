using TheMillionthFoodOrderApp.Bff.Tests.Fixtures;

namespace TheMillionthFoodOrderApp.Bff.Tests.Security;

/// <summary>
/// Verifies the OWASP-recommended security response headers are written by
/// <c>SecurityHeadersMiddleware</c> on every BFF response, including
/// short-circuited authorization responses.
/// </summary>
[ClassDataSource<BffTestWebAppFactory>(Shared = SharedType.PerClass)]
public sealed class SecurityHeadersTests(BffTestWebAppFactory factory)
{
    [Test]
    public async Task Response_Has_ContentSecurityPolicy()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/bff/user");

        var csp = response.Headers.GetValues("Content-Security-Policy").FirstOrDefault();
        await Assert.That(csp).IsNotNull();
        await Assert.That(csp!).Contains("default-src 'self'");
        await Assert.That(csp!).Contains("frame-ancestors 'none'");
    }

    [Test]
    public async Task Response_Has_XContentTypeOptions_Nosniff()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/bff/user");

        var value = response.Headers.GetValues("X-Content-Type-Options").FirstOrDefault();
        await Assert.That(value).IsEqualTo("nosniff");
    }

    [Test]
    public async Task Response_Has_XFrameOptions_Deny()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/bff/user");

        var value = response.Headers.GetValues("X-Frame-Options").FirstOrDefault();
        await Assert.That(value).IsEqualTo("DENY");
    }

    [Test]
    public async Task Response_Has_ReferrerPolicy()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/bff/user");

        var value = response.Headers.GetValues("Referrer-Policy").FirstOrDefault();
        await Assert.That(value).IsEqualTo("strict-origin-when-cross-origin");
    }

    [Test]
    public async Task Response_Has_PermissionsPolicy()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/bff/user");

        var value = response.Headers.GetValues("Permissions-Policy").FirstOrDefault();
        await Assert.That(value).IsNotNull();
        await Assert.That(value!).Contains("camera=()");
        await Assert.That(value!).Contains("microphone=()");
        await Assert.That(value!).Contains("geolocation=()");
    }

    [Test]
    public async Task UnauthorizedResponse_Also_Has_SecurityHeaders()
    {
        // Anonymous /api/* short-circuits with 401 — middleware must still write headers.
        var client = factory.CreateClient();
        var response = await client.GetAsync("/api/anything");

        await Assert.That((int)response.StatusCode).IsEqualTo(401);
        var csp = response.Headers.GetValues("Content-Security-Policy").FirstOrDefault();
        await Assert.That(csp).IsNotNull();
    }
}

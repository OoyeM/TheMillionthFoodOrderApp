using TheMillionthFoodOrderApp.Bff.Tests.Fixtures;

namespace TheMillionthFoodOrderApp.Bff.Tests.Security;

/// <summary>
/// Verifies the session cookie carries the expected security attributes.
/// In Development the cookie is named <c>bff_session</c>; in Production it is
/// <c>__Host-bff_session</c> with HttpOnly + Secure + Path=/ enforced.
/// </summary>
[ClassDataSource<BffTestWebAppFactory>(Shared = SharedType.PerClass)]
public sealed class CookieAttributesTests(BffTestWebAppFactory factory)
{
    [Test]
    public async Task DevSession_CookieIsNamed_BffSession()
    {
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        var response = await client.GetAsync("/bff/login?mock=brand-admin@frietjes");

        var setCookie = response.Headers.GetValues("Set-Cookie");
        await Assert.That(setCookie).Contains(c => c.StartsWith("bff_session=", StringComparison.Ordinal));
    }

    [Test]
    public async Task DevSession_CookieIsHttpOnly()
    {
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        var response = await client.GetAsync("/bff/login?mock=brand-admin@frietjes");

        var cookieLine = response.Headers.GetValues("Set-Cookie")
            .First(c => c.StartsWith("bff_session=", StringComparison.Ordinal));

        await Assert.That(cookieLine.Contains("httponly", StringComparison.OrdinalIgnoreCase)).IsTrue();
    }

    [Test]
    public async Task DevSession_CookieHasPathRoot()
    {
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        var response = await client.GetAsync("/bff/login?mock=brand-admin@frietjes");

        var cookieLine = response.Headers.GetValues("Set-Cookie")
            .First(c => c.StartsWith("bff_session=", StringComparison.Ordinal));

        await Assert.That(cookieLine.Contains("path=/", StringComparison.OrdinalIgnoreCase)).IsTrue();
    }

    [Test]
    [Skip("Requires a Production-environment WebApplicationFactory with mock auth — covered in Wave 2 alongside the production smoke harness")]
    public Task ProductionSession_CookieIsNamed_HostBffSession_WithSecureFlag()
    {
        // Implementation plan (Wave 2):
        // 1. Build a separate factory that calls UseEnvironment("Production") and
        //    leaves Authentication:UseMockAuth=true via in-memory config.
        // 2. Adjust the login flow guard to allow mock auth in Production for this
        //    specific factory (e.g. behind a test-only configuration flag), or
        //    issue a cookie via a stub endpoint.
        // 3. Assert the Set-Cookie header carries "__Host-bff_session=" along with
        //    the "secure" and "httponly" attributes.
        return Task.CompletedTask;
    }
}

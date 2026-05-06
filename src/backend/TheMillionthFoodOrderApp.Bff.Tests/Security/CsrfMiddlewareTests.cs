using System.Net;
using TheMillionthFoodOrderApp.Bff.Tests.Fixtures;

namespace TheMillionthFoodOrderApp.Bff.Tests.Security;

/// <summary>
/// Verifies <c>CsrfHeaderMiddleware</c>: authenticated state-changing calls
/// must carry <c>X-CSRF: 1</c>; safe methods and anonymous calls bypass.
/// </summary>
[ClassDataSource<BffTestWebAppFactory>(Shared = SharedType.PerClass)]
public sealed class CsrfMiddlewareTests(BffTestWebAppFactory factory)
{
    [Test]
    public async Task AuthenticatedPost_WithoutCsrfHeader_Returns403()
    {
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        await client.GetAsync("/bff/login?mock=brand-admin@frietjes");

        var response = await client.PostAsync("/bff/logout", content: null);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task AuthenticatedPost_WithCsrfHeader_Succeeds()
    {
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        await client.GetAsync("/bff/login?mock=brand-admin@frietjes");

        var request = new HttpRequestMessage(HttpMethod.Post, "/bff/logout");
        request.Headers.Add("X-CSRF", "1");
        var response = await client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task SafeMethod_WithoutCsrfHeader_Succeeds()
    {
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        await client.GetAsync("/bff/login?mock=brand-admin@frietjes");

        // GETs are not state-changing — the middleware does not enforce on them.
        var response = await client.GetAsync("/bff/user");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task AnonymousPost_WithoutCsrfHeader_BypassesCheck()
    {
        // No session → there is nothing to forge against. Logout is idempotent.
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });

        var response = await client.PostAsync("/bff/logout", content: null);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task AuthenticatedPost_WithWrongCsrfValue_Returns403()
    {
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        await client.GetAsync("/bff/login?mock=brand-admin@frietjes");

        var request = new HttpRequestMessage(HttpMethod.Post, "/bff/logout");
        request.Headers.Add("X-CSRF", "0");
        var response = await client.SendAsync(request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Forbidden);
    }
}

using System.Net;
using TheMillionthFoodOrderApp.Bff.Tests.Fixtures;

namespace TheMillionthFoodOrderApp.Bff.Tests.Security;

/// <summary>
/// Defends the YARP proxy against client-supplied <c>X-Brand-Slug</c> spoofing:
/// the BFF strips the header on inbound requests and only repopulates it from
/// the authenticated user's <c>brand_slug</c> claim.
///
/// Full forwarding verification (asserting the upstream actually sees the
/// claim-derived slug) requires a stub upstream and is tracked alongside the
/// existing Wave 2 bearer-forwarding work.
/// </summary>
[ClassDataSource<BffTestWebAppFactory>(Shared = SharedType.PerClass)]
public sealed class XBrandSlugInjectionTests(BffTestWebAppFactory factory)
{
    [Test]
    public async Task AnonymousRequestToProxiedApi_Returns401()
    {
        // Arrange — no session cookie, no auth
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });

        // Act
        var response = await client.GetAsync("/api/brands/frietjes/products");

        // Assert — RequireAuthorization on MapReverseProxy short-circuits with 401
        // before the YARP middleware runs, so no upstream call is made.
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task AnonymousRequestWithSpoofedBrandSlug_Returns401()
    {
        // Arrange
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/brands/frietjes/products");
        request.Headers.Add("X-Brand-Slug", "evil-other-brand");

        // Act
        var response = await client.SendAsync(request);

        // Assert — header injection cannot bypass the auth gate
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }

    [Test]
    [Skip("Requires stub upstream to capture forwarded headers — covered in Wave 2 alongside bearer-forwarding tests")]
    public Task AuthenticatedRequestWithSpoofedBrandSlug_StripsHeaderBeforeForwarding()
    {
        // Implementation plan (Wave 2):
        // 1. Spin up an in-process stub upstream (Kestrel) that records inbound headers.
        // 2. Point the YARP "api-destination" cluster at the stub's base address.
        // 3. Sign in as brand-admin@frietjes via /bff/login?mock=...
        // 4. Send GET /api/brands/frietjes/products with a malicious X-Brand-Slug header.
        // 5. Assert the upstream observed X-Brand-Slug: frietjes (from claim) — never the spoofed value.
        return Task.CompletedTask;
    }

    [Test]
    [Skip("Requires stub upstream — covered in Wave 2")]
    public Task AuthenticatedRequestWithMultipleBrandClaims_DoesNotSetHeader()
    {
        // PlatformAdmins or users assigned to multiple brands have multiple
        // brand_slug claims. In that case the BFF leaves the header unset and
        // relies on the API's route value {brandSlug} as the authoritative source.
        return Task.CompletedTask;
    }
}

using System.Net;
using TheMillionthFoodOrderApp.Bff.Tests.Fixtures;

namespace TheMillionthFoodOrderApp.Bff.Tests.Security;

/// <summary>
/// Verifies the per-IP fixed-window limiter on <c>GET /bff/login</c> rejects
/// the 11th request inside one minute with HTTP 429.
/// </summary>
[ClassDataSource<BffTestWebAppFactory>(Shared = SharedType.PerClass)]
public sealed class LoginRateLimitTests(BffTestWebAppFactory factory)
{
    [Test]
    public async Task LoginEndpoint_RejectsBurstOver10PerMinute_With429()
    {
        var client = factory.CreateClient(new() { AllowAutoRedirect = false });

        // Burst 10 requests — all should succeed (302 redirect for valid persona).
        for (var i = 0; i < 10; i++)
        {
            var ok = await client.GetAsync("/bff/login?mock=platform-admin");
            await Assert.That(ok.StatusCode).IsEqualTo(HttpStatusCode.Redirect);
        }

        // 11th request from the same IP within the same window must be rejected.
        var rejected = await client.GetAsync("/bff/login?mock=platform-admin");
        await Assert.That((int)rejected.StatusCode).IsEqualTo(429);
    }
}

namespace TheMillionthFoodOrderApp.Bff.Tests.Security;

/// <summary>
/// Body-size limits are enforced by Kestrel and the per-endpoint
/// <see cref="Microsoft.AspNetCore.Http.Features.IHttpMaxRequestBodySizeFeature"/>.
/// <see cref="Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory{TEntryPoint}"/>
/// uses an in-process TestServer that does not enforce these limits, so behavioural
/// verification requires a real Kestrel host. Tracked alongside the existing
/// Wave 2 OIDC harness work.
/// </summary>
public sealed class BodySizeLimitTests
{
    [Test]
    [Skip("Body-size limits are a Kestrel feature — requires a real-Kestrel test host (Wave 2)")]
    public Task BffEndpoint_RejectsLargeBody_With413()
    {
        // Implementation plan (Wave 2):
        // 1. Spin up the BFF using WebHost.UseKestrel on a free port.
        // 2. POST a 64 KiB body to /bff/logout (after sign-in, with X-CSRF: 1).
        // 3. Assert the response is 413 Payload Too Large.
        return Task.CompletedTask;
    }

    [Test]
    [Skip("Requires real Kestrel host — Wave 2")]
    public Task ProxiedApiRoute_AcceptsLargerBody_UpTo10MiB()
    {
        // Implementation plan (Wave 2):
        // 1. Real-Kestrel host with stub upstream.
        // 2. Sign in, POST a 1 MiB body to /api/anything.
        // 3. Assert the upstream observed the body in full and returned 200.
        return Task.CompletedTask;
    }
}

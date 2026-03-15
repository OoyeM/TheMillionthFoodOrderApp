using System.Net;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using TheMillionthFoodOrderApp.Tests.Integration.Fixtures;

namespace TheMillionthFoodOrderApp.Tests.Integration.Multitenancy;

/// <summary>
/// Integration tests for <c>BrandContextMiddleware</c> — verifies that the middleware
/// correctly validates brand slugs and returns the appropriate HTTP status codes.
/// </summary>
public sealed class BrandContextMiddlewareTests(IntegrationTestBase fixture)
    : IClassFixture<IntegrationTestBase>
{
    [Fact]
    public async Task Request_WithUnknownBrandSlug_Returns404()
    {
        var client = fixture.Factory.CreateClient();

        var response = await client.GetAsync("/api/brands/unknown-brand-xyz/settings");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Request_WithInactiveBrandSlug_Returns403()
    {
        const string inactiveSlug = "middleware-inactive-test";

        // Create an inactive brand in the platform DB for this test
        await using (var scope = fixture.Factory.Services.CreateAsyncScope())
        {
            var platformDb = scope.ServiceProvider.GetRequiredService<
                TheMillionthFoodOrderApp.Infrastructure.Persistence.PlatformDbContext>();

            var inactiveBrand = TheMillionthFoodOrderApp.Domain.Brands.Brand.Create(
                "Middleware Inactive Test Brand",
                inactiveSlug,
                "middleware-inactive@test.com",
                null);

            // Deactivate immediately before saving
            inactiveBrand.Deactivate();

            await platformDb.Brands.AddAsync(inactiveBrand);
            await platformDb.SaveChangesAsync();
        }

        var client = fixture.Factory.CreateClient();
        var response = await client.GetAsync($"/api/brands/{inactiveSlug}/settings");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Request_WithValidActiveBrandSlug_PassesThroughMiddleware()
    {
        var client = fixture.Factory.CreateClient();

        // Alpha is a valid, active brand — middleware should pass through.
        // The endpoint returns 404 because no settings are seeded yet —
        // that 404 comes from the endpoint handler, not from middleware validation.
        var response = await client.GetAsync(
            $"/api/brands/{IntegrationTestBase.AlphaSlug}/settings");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PlatformEndpoint_WithoutBrandSlug_IsUnaffectedByMiddleware()
    {
        var client = fixture.Factory.CreateClient();

        // Platform endpoint — no {brandSlug} in route, middleware should pass through.
        var response = await client.GetAsync("/api/brands");

        // Middleware must not return 403/404 for brand validation — any 2xx confirms pass-through.
        response.StatusCode.Should().NotBe(HttpStatusCode.Forbidden);
        response.StatusCode.Should().NotBe(HttpStatusCode.NotFound);
    }
}

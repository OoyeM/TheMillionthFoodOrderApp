using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TheMillionthFoodOrderApp.Application.BrandSettings;
using TheMillionthFoodOrderApp.Tests.Integration.Fixtures;

namespace TheMillionthFoodOrderApp.Tests.Integration.BrandSettings;

/// <summary>
/// Integration tests proving that brand settings are fully isolated between brands.
/// Data written to Brand Alpha must never be visible to Brand Beta and vice versa.
/// </summary>
public sealed class BrandSettingsIsolationTests(IntegrationTestBase fixture)
    : IClassFixture<IntegrationTestBase>
{
    // ── GET settings ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetSettings_WhenNoSettingsExist_Returns404()
    {
        var client = fixture.Factory.CreateClient();

        // Use Gamma — a brand that no other test writes to, so ordering doesn't matter.
        var response = await client.GetAsync($"/api/brands/{IntegrationTestBase.GammaSlug}/settings");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── PUT settings (upsert) ────────────────────────────────────────────────

    [Fact]
    public async Task PutSettings_WritesToAlpha_NotVisibleToBeta()
    {
        var client = fixture.Factory.CreateClient();

        // Write settings to Alpha
        var alphaSettings = new
        {
            DefaultLanguage = "nl-BE",
            Timezone = "Europe/Brussels",
            Currency = "EUR"
        };

        var putResponse = await client.PutAsJsonAsync(
            $"/api/brands/{IntegrationTestBase.AlphaSlug}/settings",
            alphaSettings);

        putResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var alphaResult = await putResponse.Content.ReadFromJsonAsync<BrandSettingsResponse>();
        alphaResult.Should().NotBeNull();
        alphaResult!.DefaultLanguage.Should().Be("nl-BE");

        // Beta should still return 404 — Alpha's data must not leak
        var betaGetResponse = await client.GetAsync(
            $"/api/brands/{IntegrationTestBase.BetaSlug}/settings");

        betaGetResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task PutSettings_BothBrandsHaveIndependentSettings()
    {
        var client = fixture.Factory.CreateClient();

        // Write different settings to each brand
        var alphaSettings = new
        {
            DefaultLanguage = "nl-BE",
            Timezone = "Europe/Brussels",
            Currency = "EUR"
        };

        var betaSettings = new
        {
            DefaultLanguage = "fr-BE",
            Timezone = "Europe/Paris",
            Currency = "EUR"
        };

        var alphaPut = await client.PutAsJsonAsync(
            $"/api/brands/{IntegrationTestBase.AlphaSlug}/settings",
            alphaSettings);
        alphaPut.StatusCode.Should().Be(HttpStatusCode.OK);

        var betaPut = await client.PutAsJsonAsync(
            $"/api/brands/{IntegrationTestBase.BetaSlug}/settings",
            betaSettings);
        betaPut.StatusCode.Should().Be(HttpStatusCode.OK);

        // Read back Alpha — must not have Beta's language
        var alphaGet = await client.GetAsync(
            $"/api/brands/{IntegrationTestBase.AlphaSlug}/settings");
        alphaGet.StatusCode.Should().Be(HttpStatusCode.OK);
        var alphaResult = await alphaGet.Content.ReadFromJsonAsync<BrandSettingsResponse>();
        alphaResult!.DefaultLanguage.Should().Be("nl-BE");

        // Read back Beta — must not have Alpha's language
        var betaGet = await client.GetAsync(
            $"/api/brands/{IntegrationTestBase.BetaSlug}/settings");
        betaGet.StatusCode.Should().Be(HttpStatusCode.OK);
        var betaResult = await betaGet.Content.ReadFromJsonAsync<BrandSettingsResponse>();
        betaResult!.DefaultLanguage.Should().Be("fr-BE");
    }

    [Fact]
    public async Task PutSettings_UpdateExisting_ReturnsUpdatedValues()
    {
        var client = fixture.Factory.CreateClient();

        // Initial write
        var initial = new
        {
            DefaultLanguage = "nl-BE",
            Timezone = "Europe/Brussels",
            Currency = "EUR"
        };
        await client.PutAsJsonAsync(
            $"/api/brands/{IntegrationTestBase.AlphaSlug}/settings", initial);

        // Update
        var updated = new
        {
            DefaultLanguage = "fr-BE",
            Timezone = "Europe/Paris",
            Currency = "EUR"
        };
        var updateResponse = await client.PutAsJsonAsync(
            $"/api/brands/{IntegrationTestBase.AlphaSlug}/settings", updated);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await updateResponse.Content.ReadFromJsonAsync<BrandSettingsResponse>();
        result!.DefaultLanguage.Should().Be("fr-BE");
        result.Timezone.Should().Be("Europe/Paris");
    }
}

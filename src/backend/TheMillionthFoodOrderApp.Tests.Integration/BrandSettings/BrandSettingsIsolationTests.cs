using System.Net;
using System.Net.Http.Json;
using TheMillionthFoodOrderApp.Application.BrandSettings;
using TheMillionthFoodOrderApp.Tests.Integration.Fixtures;

namespace TheMillionthFoodOrderApp.Tests.Integration.BrandSettings;

/// <summary>
/// Integration tests proving that brand settings are fully isolated between brands.
/// Data written to Brand Alpha must never be visible to Brand Beta and vice versa.
/// </summary>
[ClassDataSource<IntegrationTestBase>(Shared = SharedType.PerClass)]
public sealed class BrandSettingsIsolationTests(IntegrationTestBase fixture)
{
    // ── GET settings ─────────────────────────────────────────────────────────

    [Test]
    public async Task GetSettings_WhenNoSettingsExist_Returns404()
    {
        var client = fixture.Factory.CreateClient();

        // Use Gamma — a brand that no other test writes to, so ordering doesn't matter.
        var response = await client.GetAsync($"/api/brands/{IntegrationTestBase.GammaSlug}/settings");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    // ── PUT settings (upsert) ────────────────────────────────────────────────

    [Test]
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

        await Assert.That(putResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var alphaResult = await putResponse.Content.ReadFromJsonAsync<BrandSettingsResponse>();
        await Assert.That(alphaResult).IsNotNull();
        await Assert.That(alphaResult!.DefaultLanguage).IsEqualTo("nl-BE");

        // Beta should still return 404 — Alpha's data must not leak
        var betaGetResponse = await client.GetAsync(
            $"/api/brands/{IntegrationTestBase.BetaSlug}/settings");

        await Assert.That(betaGetResponse.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
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
        await Assert.That(alphaPut.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var betaPut = await client.PutAsJsonAsync(
            $"/api/brands/{IntegrationTestBase.BetaSlug}/settings",
            betaSettings);
        await Assert.That(betaPut.StatusCode).IsEqualTo(HttpStatusCode.OK);

        // Read back Alpha — must not have Beta's language
        var alphaGet = await client.GetAsync(
            $"/api/brands/{IntegrationTestBase.AlphaSlug}/settings");
        await Assert.That(alphaGet.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var alphaResult = await alphaGet.Content.ReadFromJsonAsync<BrandSettingsResponse>();
        await Assert.That(alphaResult!.DefaultLanguage).IsEqualTo("nl-BE");

        // Read back Beta — must not have Alpha's language
        var betaGet = await client.GetAsync(
            $"/api/brands/{IntegrationTestBase.BetaSlug}/settings");
        await Assert.That(betaGet.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var betaResult = await betaGet.Content.ReadFromJsonAsync<BrandSettingsResponse>();
        await Assert.That(betaResult!.DefaultLanguage).IsEqualTo("fr-BE");
    }

    [Test]
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

        await Assert.That(updateResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var result = await updateResponse.Content.ReadFromJsonAsync<BrandSettingsResponse>();
        await Assert.That(result!.DefaultLanguage).IsEqualTo("fr-BE");
        await Assert.That(result.Timezone).IsEqualTo("Europe/Paris");
    }
}

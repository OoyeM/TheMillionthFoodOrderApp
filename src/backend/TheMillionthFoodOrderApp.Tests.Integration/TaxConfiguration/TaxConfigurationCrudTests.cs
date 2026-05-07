using System.Net;
using System.Net.Http.Json;
using TheMillionthFoodOrderApp.Application.TaxConfiguration;
using TheMillionthFoodOrderApp.Tests.Integration.Fixtures;

namespace TheMillionthFoodOrderApp.Tests.Integration.TaxConfiguration;

/// <summary>
/// Integration tests for the Belgian VAT tax configuration CRUD endpoints.
/// Covers GET, PUT, and POST /calculate for /api/brands/{brandSlug}/tax-configuration.
///
/// NOTE: The BrandDbSeeder only runs in the Development environment, not in Testing.
/// Tests that need a pre-existing config must first PUT one via the API.
/// </summary>
[ClassDataSource<IntegrationTestBase>(Shared = SharedType.PerClass)]
public sealed class TaxConfigurationCrudTests(IntegrationTestBase fixture)
{
    private HttpClient CreateClient() => fixture.Factory.CreateClient();

    private static string TaxConfigUrl(string brandSlug) =>
        $"/api/brands/{brandSlug}/tax-configuration";

    private static string CalculateUrl(string brandSlug) =>
        $"/api/brands/{brandSlug}/tax-configuration/calculate";

    // ── GET tax-configuration ────────────────────────────────────────────────

    /// <summary>
    /// Verifies that after writing Belgian default rates the GET endpoint
    /// returns Takeaway=6 and EatIn=21.
    /// The BrandDbSeeder does not run in Testing, so we seed via PUT first.
    /// </summary>
    [Test]
    public async Task GetTaxConfiguration_AfterSeeding_ReturnsBelgianDefaults()
    {
        // Arrange — seed Belgian defaults via PUT (upsert creates if missing)
        var client = CreateClient();

        var seedRequest = new
        {
            VatRates = new[]
            {
                new { ConsumptionMode = "Takeaway", RatePercentage = 6m },
                new { ConsumptionMode = "EatIn",    RatePercentage = 21m },
            }
        };

        var putResponse = await client.PutAsJsonAsync(TaxConfigUrl(IntegrationTestBase.AlphaSlug), seedRequest);
        await Assert.That(putResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        // Act
        var response = await client.GetAsync(TaxConfigUrl(IntegrationTestBase.AlphaSlug));

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var config = await response.Content.ReadFromJsonAsync<TaxConfigurationResponse>();
        await Assert.That(config).IsNotNull();
        await Assert.That(config!.VatRates.Count).IsEqualTo(2);

        var takeaway = config.VatRates.FirstOrDefault(r => r.ConsumptionMode == "Takeaway");
        await Assert.That(takeaway).IsNotNull();
        await Assert.That(takeaway!.RatePercentage).IsEqualTo(6m);

        var eatIn = config.VatRates.FirstOrDefault(r => r.ConsumptionMode == "EatIn");
        await Assert.That(eatIn).IsNotNull();
        await Assert.That(eatIn!.RatePercentage).IsEqualTo(21m);
    }

    [Test]
    public async Task GetTaxConfiguration_NonExistentBrand_Returns404()
    {
        // Arrange
        var client = CreateClient();

        // Act — use a brand slug that was never registered in the platform DB
        var response = await client.GetAsync(TaxConfigUrl("non-existent-brand"));

        // Assert — BrandContextMiddleware returns 404 for unknown brands
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    // ── PUT tax-configuration ────────────────────────────────────────────────

    [Test]
    public async Task UpdateTaxConfiguration_ValidRates_Returns200AndUpdatedConfig()
    {
        // Arrange
        var client = CreateClient();

        var request = new
        {
            VatRates = new[]
            {
                new { ConsumptionMode = "Takeaway", RatePercentage = 7m },
                new { ConsumptionMode = "EatIn",    RatePercentage = 22m },
            }
        };

        // Act
        var response = await client.PutAsJsonAsync(TaxConfigUrl(IntegrationTestBase.BetaSlug), request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var config = await response.Content.ReadFromJsonAsync<TaxConfigurationResponse>();
        await Assert.That(config).IsNotNull();
        await Assert.That(config!.VatRates.Count).IsEqualTo(2);

        var takeaway = config.VatRates.FirstOrDefault(r => r.ConsumptionMode == "Takeaway");
        await Assert.That(takeaway).IsNotNull();
        await Assert.That(takeaway!.RatePercentage).IsEqualTo(7m);

        var eatIn = config.VatRates.FirstOrDefault(r => r.ConsumptionMode == "EatIn");
        await Assert.That(eatIn).IsNotNull();
        await Assert.That(eatIn!.RatePercentage).IsEqualTo(22m);
    }

    [Test]
    public async Task UpdateTaxConfiguration_InvalidRate_Returns400()
    {
        // Arrange — rate > 100 violates FluentValidation rule
        var client = CreateClient();

        var request = new
        {
            VatRates = new[]
            {
                new { ConsumptionMode = "Takeaway", RatePercentage = 101m },
                new { ConsumptionMode = "EatIn",    RatePercentage = 21m },
            }
        };

        // Act
        var response = await client.PutAsJsonAsync(TaxConfigUrl(IntegrationTestBase.AlphaSlug), request);

        // Assert
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task UpdateTaxConfiguration_MissingMode_Returns400()
    {
        // Arrange — only one consumption mode provided; domain requires exactly one entry per mode
        var client = CreateClient();

        var request = new
        {
            VatRates = new[]
            {
                new { ConsumptionMode = "Takeaway", RatePercentage = 6m },
            }
        };

        // Act
        var response = await client.PutAsJsonAsync(TaxConfigUrl(IntegrationTestBase.AlphaSlug), request);

        // Assert — UpdateRates throws ArgumentException; endpoint maps it to 400
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task UpdateTaxConfiguration_InvalidModeName_Returns400()
    {
        // Arrange — "Invalid" is not a recognised ConsumptionMode
        var client = CreateClient();

        var request = new
        {
            VatRates = new[]
            {
                new { ConsumptionMode = "Invalid", RatePercentage = 6m },
                new { ConsumptionMode = "EatIn",   RatePercentage = 21m },
            }
        };

        // Act
        var response = await client.PutAsJsonAsync(TaxConfigUrl(IntegrationTestBase.AlphaSlug), request);

        // Assert — FluentValidation rejects unknown mode
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task UpdateTaxConfiguration_DuplicateMode_Returns400()
    {
        // Arrange — two Takeaway entries violate the uniqueness rule
        var client = CreateClient();

        var request = new
        {
            VatRates = new[]
            {
                new { ConsumptionMode = "Takeaway", RatePercentage = 6m },
                new { ConsumptionMode = "Takeaway", RatePercentage = 9m },
            }
        };

        // Act
        var response = await client.PutAsJsonAsync(TaxConfigUrl(IntegrationTestBase.AlphaSlug), request);

        // Assert — FluentValidation rejects duplicate modes
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task UpdateTaxConfiguration_PreservesCreatedAtAndBumpsUpdatedAt()
    {
        // Arrange — create initial config
        var client = CreateClient();
        var slug = IntegrationTestBase.BetaSlug;

        var initialRequest = new
        {
            VatRates = new[]
            {
                new { ConsumptionMode = "Takeaway", RatePercentage = 6m },
                new { ConsumptionMode = "EatIn",    RatePercentage = 21m },
            }
        };

        var createResponse = await client.PutAsJsonAsync(TaxConfigUrl(slug), initialRequest);
        await Assert.That(createResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var created = await createResponse.Content.ReadFromJsonAsync<TaxConfigurationResponse>();
        await Assert.That(created).IsNotNull();

        // Act — update rates
        var updateRequest = new
        {
            VatRates = new[]
            {
                new { ConsumptionMode = "Takeaway", RatePercentage = 8m },
                new { ConsumptionMode = "EatIn",    RatePercentage = 23m },
            }
        };

        var updateResponse = await client.PutAsJsonAsync(TaxConfigUrl(slug), updateRequest);
        await Assert.That(updateResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<TaxConfigurationResponse>();
        await Assert.That(updated).IsNotNull();

        // Assert — CreatedAt unchanged, UpdatedAt bumped
        await Assert.That(updated!.CreatedAt.ToUnixTimeSeconds()).IsEquivalentTo(created!.CreatedAt.ToUnixTimeSeconds());
        await Assert.That(updated.UpdatedAt).IsGreaterThanOrEqualTo(created.UpdatedAt);
    }

    // ── POST tax-configuration/calculate ────────────────────────────────────

    [Test]
    public async Task CalculateTax_Takeaway_ReturnsCorrect6PercentBreakdown()
    {
        // Arrange — the fixture pre-seeds Belgian defaults (Takeaway 6%, EatIn 21%) for all brands.
        // No PUT needed here; doing one would cause concurrent write contention with other tests
        // that also use AlphaSlug, making this test flaky.
        var client = CreateClient();

        var request = new
        {
            GrossAmount     = 3.50m,
            ConsumptionMode = "Takeaway",
        };

        // Act
        var response = await client.PostAsJsonAsync(CalculateUrl(IntegrationTestBase.AlphaSlug), request);

        // Assert
        // net  = Round(3.50 / 1.06, 2, AwayFromZero) = 3.30
        // vat  = 3.50 - 3.30 = 0.20
        // gross = 3.50, rate = 6
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var breakdown = await response.Content.ReadFromJsonAsync<TaxBreakdownDto>();
        await Assert.That(breakdown).IsNotNull();
        await Assert.That(breakdown!.GrossAmount).IsEqualTo(3.50m);
        await Assert.That(breakdown.NetAmount).IsEqualTo(3.30m);
        await Assert.That(breakdown.VatAmount).IsEqualTo(0.20m);
        await Assert.That(breakdown.VatRatePercentage).IsEqualTo(6m);
    }

    [Test]
    public async Task CalculateTax_EatIn_ReturnsCorrect21PercentBreakdown()
    {
        // Arrange — the fixture pre-seeds Belgian defaults (Takeaway 6%, EatIn 21%) for all brands.
        // No PUT needed here; doing one would cause concurrent write contention with other tests
        // that also use AlphaSlug, making this test flaky.
        var client = CreateClient();

        var request = new
        {
            GrossAmount     = 3.50m,
            ConsumptionMode = "EatIn",
        };

        // Act
        var response = await client.PostAsJsonAsync(CalculateUrl(IntegrationTestBase.AlphaSlug), request);

        // Assert
        // net  = Round(3.50 / 1.21, 2, AwayFromZero) = 2.89
        // vat  = 3.50 - 2.89 = 0.61
        // gross = 3.50, rate = 21
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var breakdown = await response.Content.ReadFromJsonAsync<TaxBreakdownDto>();
        await Assert.That(breakdown).IsNotNull();
        await Assert.That(breakdown!.GrossAmount).IsEqualTo(3.50m);
        await Assert.That(breakdown.NetAmount).IsEqualTo(2.89m);
        await Assert.That(breakdown.VatAmount).IsEqualTo(0.61m);
        await Assert.That(breakdown.VatRatePercentage).IsEqualTo(21m);
    }

    [Test]
    public async Task CalculateTax_InvalidMode_Returns400()
    {
        // Arrange
        var client = CreateClient();

        var request = new
        {
            GrossAmount     = 3.50m,
            ConsumptionMode = "DineIn",  // Not a valid ConsumptionMode value
        };

        // Act
        var response = await client.PostAsJsonAsync(CalculateUrl(IntegrationTestBase.AlphaSlug), request);

        // Assert — FluentValidation rejects unknown mode
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }
}

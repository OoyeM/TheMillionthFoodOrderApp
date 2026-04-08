using System.Net;
using System.Net.Http.Json;
using Shouldly;
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
public sealed class TaxConfigurationCrudTests(IntegrationTestBase fixture)
    : IClassFixture<IntegrationTestBase>
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
    [Fact]
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
        putResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Act
        var response = await client.GetAsync(TaxConfigUrl(IntegrationTestBase.AlphaSlug));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var config = await response.Content.ReadFromJsonAsync<TaxConfigurationResponse>();
        config.ShouldNotBeNull();
        config.VatRates.Count.ShouldBe(2);

        var takeaway = config.VatRates.FirstOrDefault(r => r.ConsumptionMode == "Takeaway");
        takeaway.ShouldNotBeNull();
        takeaway.RatePercentage.ShouldBe(6m);

        var eatIn = config.VatRates.FirstOrDefault(r => r.ConsumptionMode == "EatIn");
        eatIn.ShouldNotBeNull();
        eatIn.RatePercentage.ShouldBe(21m);
    }

    [Fact]
    public async Task GetTaxConfiguration_NonExistentBrand_Returns404()
    {
        // Arrange
        var client = CreateClient();

        // Act — use a brand slug that was never registered in the platform DB
        var response = await client.GetAsync(TaxConfigUrl("non-existent-brand"));

        // Assert — BrandContextMiddleware returns 404 for unknown brands
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── PUT tax-configuration ────────────────────────────────────────────────

    [Fact]
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
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var config = await response.Content.ReadFromJsonAsync<TaxConfigurationResponse>();
        config.ShouldNotBeNull();
        config.VatRates.Count.ShouldBe(2);

        var takeaway = config.VatRates.FirstOrDefault(r => r.ConsumptionMode == "Takeaway");
        takeaway.ShouldNotBeNull();
        takeaway.RatePercentage.ShouldBe(7m);

        var eatIn = config.VatRates.FirstOrDefault(r => r.ConsumptionMode == "EatIn");
        eatIn.ShouldNotBeNull();
        eatIn.RatePercentage.ShouldBe(22m);
    }

    [Fact]
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
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
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
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
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
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
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
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // ── POST tax-configuration/calculate ────────────────────────────────────

    [Fact]
    public async Task CalculateTax_Takeaway_ReturnsCorrect6PercentBreakdown()
    {
        // Arrange — ensure a tax config with 6% Takeaway exists
        var client = CreateClient();

        await client.PutAsJsonAsync(TaxConfigUrl(IntegrationTestBase.AlphaSlug), new
        {
            VatRates = new[]
            {
                new { ConsumptionMode = "Takeaway", RatePercentage = 6m },
                new { ConsumptionMode = "EatIn",    RatePercentage = 21m },
            }
        });

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
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var breakdown = await response.Content.ReadFromJsonAsync<TaxBreakdownDto>();
        breakdown.ShouldNotBeNull();
        breakdown.GrossAmount.ShouldBe(3.50m);
        breakdown.NetAmount.ShouldBe(3.30m);
        breakdown.VatAmount.ShouldBe(0.20m);
        breakdown.VatRatePercentage.ShouldBe(6m);
    }

    [Fact]
    public async Task CalculateTax_EatIn_ReturnsCorrect21PercentBreakdown()
    {
        // Arrange — ensure a tax config with 21% EatIn exists
        var client = CreateClient();

        await client.PutAsJsonAsync(TaxConfigUrl(IntegrationTestBase.AlphaSlug), new
        {
            VatRates = new[]
            {
                new { ConsumptionMode = "Takeaway", RatePercentage = 6m },
                new { ConsumptionMode = "EatIn",    RatePercentage = 21m },
            }
        });

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
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var breakdown = await response.Content.ReadFromJsonAsync<TaxBreakdownDto>();
        breakdown.ShouldNotBeNull();
        breakdown.GrossAmount.ShouldBe(3.50m);
        breakdown.NetAmount.ShouldBe(2.89m);
        breakdown.VatAmount.ShouldBe(0.61m);
        breakdown.VatRatePercentage.ShouldBe(21m);
    }

    [Fact]
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
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}

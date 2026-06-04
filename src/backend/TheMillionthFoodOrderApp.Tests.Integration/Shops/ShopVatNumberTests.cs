using System.Net;
using System.Net.Http.Json;
using TheMillionthFoodOrderApp.Application.Shops;
using TheMillionthFoodOrderApp.Tests.Integration.Fixtures;

namespace TheMillionthFoodOrderApp.Tests.Integration.Shops;

/// <summary>
/// Integration tests for the shop VAT number (US-FP-052) — the legal identifier printed on
/// customer receipts. Verifies it round-trips through create, get, and update.
/// Runs against a real SQL Server via Testcontainers.
/// </summary>
[ClassDataSource<IntegrationTestBase>(Shared = SharedType.PerClass)]
public sealed class ShopVatNumberTests(IntegrationTestBase fixture)
{
    private HttpClient CreateClient() => fixture.Factory.CreateClient();

    private static string ShopsUrl(string brandSlug) =>
        $"/api/brands/{brandSlug}/shops";

    private static string ShopUrl(string brandSlug, Guid shopId) =>
        $"/api/brands/{brandSlug}/shops/{shopId}";

    [Test]
    public async Task CreateShop_WithVatNumber_PersistsAndReturnsIt()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.AlphaSlug;

        var request = new
        {
            Name = "VAT Shop",
            Slug = $"vat-shop-{Guid.NewGuid():N}",
            Address = new
            {
                Street = "Btwstraat",
                Number = "7",
                City = "Gent",
                PostalCode = "9000",
                Country = "BE"
            },
            ContactEmail = "vat@frietjes.be",
            ContactPhone = (string?)null,
            VatNumber = "BE0123456789"
        };

        var createResponse = await client.PostAsJsonAsync(ShopsUrl(brand), request);
        await Assert.That(createResponse.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<ShopResponse>();
        await Assert.That(created).IsNotNull();
        await Assert.That(created!.VatNumber).IsEqualTo("BE0123456789");

        // Re-fetch to prove it persisted (not just echoed from the request).
        var getResponse = await client.GetAsync(ShopUrl(brand, created.Id));
        await Assert.That(getResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var fetched = await getResponse.Content.ReadFromJsonAsync<ShopResponse>();
        await Assert.That(fetched).IsNotNull();
        await Assert.That(fetched!.VatNumber).IsEqualTo("BE0123456789");
    }

    [Test]
    public async Task CreateShop_WithoutVatNumber_ReturnsNull()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.BetaSlug;

        var request = new
        {
            Name = "No-VAT Shop",
            Slug = $"novat-shop-{Guid.NewGuid():N}",
            Address = new
            {
                Street = "Btwstraat",
                Number = "8",
                City = "Gent",
                PostalCode = "9000",
                Country = "BE"
            },
            ContactEmail = "novat@frietjes.be",
            ContactPhone = (string?)null
        };

        var createResponse = await client.PostAsJsonAsync(ShopsUrl(brand), request);
        await Assert.That(createResponse.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<ShopResponse>();
        await Assert.That(created).IsNotNull();
        await Assert.That(created!.VatNumber).IsNull();
    }

    [Test]
    public async Task UpdateShop_SetsVatNumber()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.GammaSlug;

        // Create without a VAT number.
        var createRequest = new
        {
            Name = "Editable Shop",
            Slug = $"edit-vat-{Guid.NewGuid():N}",
            Address = new
            {
                Street = "Btwstraat",
                Number = "9",
                City = "Gent",
                PostalCode = "9000",
                Country = "BE"
            },
            ContactEmail = "edit@frietjes.be",
            ContactPhone = (string?)null
        };

        var createResponse = await client.PostAsJsonAsync(ShopsUrl(brand), createRequest);
        await Assert.That(createResponse.StatusCode).IsEqualTo(HttpStatusCode.Created);
        var created = await createResponse.Content.ReadFromJsonAsync<ShopResponse>();
        await Assert.That(created).IsNotNull();
        await Assert.That(created!.VatNumber).IsNull();

        // Update it with a VAT number.
        var updateRequest = new
        {
            Name = created.Name,
            Address = new
            {
                Street = created.Address.Street,
                Number = created.Address.Number,
                City = created.Address.City,
                PostalCode = created.Address.PostalCode,
                Country = created.Address.Country
            },
            ContactEmail = created.ContactEmail,
            ContactPhone = (string?)null,
            KitchenDisplayEnabled = false,
            TicketPrinterEnabled = false,
            PushNotificationEnabled = false,
            SoundAlertEnabled = false,
            EatIn = new { IsEnabled = true, RequiresTableNumber = true },
            TimeSlotOrdering = new { IsEnabled = false, IntervalMinutes = (int?)null, MaxOrdersPerInterval = (int?)null },
            VatNumber = "BE0987654321"
        };

        var updateResponse = await client.PutAsJsonAsync(ShopUrl(brand, created.Id), updateRequest);
        await Assert.That(updateResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var updated = await updateResponse.Content.ReadFromJsonAsync<ShopResponse>();
        await Assert.That(updated).IsNotNull();
        await Assert.That(updated!.VatNumber).IsEqualTo("BE0987654321");
    }
}

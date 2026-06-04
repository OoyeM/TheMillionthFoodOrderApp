using System.Net;
using System.Net.Http.Json;
using TheMillionthFoodOrderApp.Application.Shops;
using TheMillionthFoodOrderApp.Tests.Integration.Fixtures;

namespace TheMillionthFoodOrderApp.Tests.Integration.Shops;

/// <summary>
/// Integration tests for the per-shop eat-in toggle (US-FP-066) and time-slot ordering settings
/// (US-FP-020). Verifies defaults, that both round-trip through create → get → update, that an
/// invalid time-slot interval is rejected, and that eat-in settings are exposed on the public
/// storefront endpoint. Runs against a real SQL Server via Testcontainers.
/// </summary>
[ClassDataSource<IntegrationTestBase>(Shared = SharedType.PerClass)]
public sealed class ShopEatInTimeSlotTests(IntegrationTestBase fixture)
{
    private HttpClient CreateClient() => fixture.Factory.CreateClient();

    private static string ShopsUrl(string brandSlug) => $"/api/brands/{brandSlug}/shops";
    private static string ShopUrl(string brandSlug, Guid shopId) => $"/api/brands/{brandSlug}/shops/{shopId}";
    private static string ActiveShopsUrl(string brandSlug) => $"/api/brands/{brandSlug}/shops/active";

    private static object BuildCreateBody() => new
    {
        Name = "Eat-in Shop",
        Slug = $"eatin-{Guid.NewGuid():N}",
        Address = new { Street = "Frietstraat", Number = "1", City = "Gent", PostalCode = "9000", Country = "BE" },
        ContactEmail = "eatin@frietjes.be",
        ContactPhone = (string?)null,
    };

    [Test]
    public async Task CreateShop_Defaults_EatInEnabledRequiresTable_TimeSlotDisabled()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.AlphaSlug;

        var createResponse = await client.PostAsJsonAsync(ShopsUrl(brand), BuildCreateBody());
        await Assert.That(createResponse.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<ShopResponse>();
        await Assert.That(created).IsNotNull();
        await Assert.That(created!.EatIn.IsEnabled).IsTrue();
        await Assert.That(created.EatIn.RequiresTableNumber).IsTrue();
        await Assert.That(created.TimeSlotOrdering.IsEnabled).IsFalse();
        await Assert.That(created.TimeSlotOrdering.IntervalMinutes).IsNull();
        await Assert.That(created.TimeSlotOrdering.MaxOrdersPerInterval).IsNull();
    }

    [Test]
    public async Task UpdateShop_DisablesEatIn_AndEnablesTimeSlot_Persists()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.BetaSlug;

        var createResponse = await client.PostAsJsonAsync(ShopsUrl(brand), BuildCreateBody());
        var created = await createResponse.Content.ReadFromJsonAsync<ShopResponse>();
        await Assert.That(created).IsNotNull();

        var updateRequest = new
        {
            Name = created!.Name,
            Address = new
            {
                created.Address.Street,
                created.Address.Number,
                created.Address.City,
                created.Address.PostalCode,
                created.Address.Country,
            },
            ContactEmail = created.ContactEmail,
            ContactPhone = (string?)null,
            KitchenDisplayEnabled = false,
            TicketPrinterEnabled = false,
            PushNotificationEnabled = false,
            SoundAlertEnabled = false,
            EatIn = new { IsEnabled = false, RequiresTableNumber = false },
            TimeSlotOrdering = new { IsEnabled = true, IntervalMinutes = 15, MaxOrdersPerInterval = 4 },
            VatNumber = (string?)null,
        };

        var updateResponse = await client.PutAsJsonAsync(ShopUrl(brand, created.Id), updateRequest);
        await Assert.That(updateResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        // Re-fetch to prove persistence (not just an echo of the request).
        var getResponse = await client.GetAsync(ShopUrl(brand, created.Id));
        var fetched = await getResponse.Content.ReadFromJsonAsync<ShopResponse>();
        await Assert.That(fetched).IsNotNull();
        await Assert.That(fetched!.EatIn.IsEnabled).IsFalse();
        await Assert.That(fetched.EatIn.RequiresTableNumber).IsFalse();
        await Assert.That(fetched.TimeSlotOrdering.IsEnabled).IsTrue();
        await Assert.That(fetched.TimeSlotOrdering.IntervalMinutes).IsEqualTo(15);
        await Assert.That(fetched.TimeSlotOrdering.MaxOrdersPerInterval).IsEqualTo(4);
    }

    [Test]
    public async Task UpdateShop_WithInvalidTimeSlotInterval_Returns400()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.GammaSlug;

        var createResponse = await client.PostAsJsonAsync(ShopsUrl(brand), BuildCreateBody());
        var created = await createResponse.Content.ReadFromJsonAsync<ShopResponse>();
        await Assert.That(created).IsNotNull();

        var updateRequest = new
        {
            Name = created!.Name,
            Address = new
            {
                created.Address.Street,
                created.Address.Number,
                created.Address.City,
                created.Address.PostalCode,
                created.Address.Country,
            },
            ContactEmail = created.ContactEmail,
            ContactPhone = (string?)null,
            KitchenDisplayEnabled = false,
            TicketPrinterEnabled = false,
            PushNotificationEnabled = false,
            SoundAlertEnabled = false,
            EatIn = new { IsEnabled = true, RequiresTableNumber = true },
            TimeSlotOrdering = new { IsEnabled = true, IntervalMinutes = 7, MaxOrdersPerInterval = 4 }, // 7 is invalid
            VatNumber = (string?)null,
        };

        var updateResponse = await client.PutAsJsonAsync(ShopUrl(brand, created.Id), updateRequest);
        await Assert.That(updateResponse.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task ActiveShops_ExposeEatInSettings()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.AlphaSlug;

        // Ensure at least one active shop exists for the brand.
        await client.PostAsJsonAsync(ShopsUrl(brand), BuildCreateBody());

        var response = await client.GetAsync(ActiveShopsUrl(brand));
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var shops = await response.Content.ReadFromJsonAsync<List<StorefrontShopResponse>>();
        await Assert.That(shops).IsNotNull();
        await Assert.That(shops!.Count).IsGreaterThan(0);
        // The storefront response carries the eat-in settings so the customer app can gate eat-in.
        await Assert.That(shops.TrueForAll(s => s.EatIn is not null)).IsTrue();
    }
}

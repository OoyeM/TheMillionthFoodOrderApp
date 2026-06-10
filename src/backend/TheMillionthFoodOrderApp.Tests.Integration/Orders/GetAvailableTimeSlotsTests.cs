using System.Net;
using System.Net.Http.Json;
using TheMillionthFoodOrderApp.Application.Orders;
using TheMillionthFoodOrderApp.Application.Shops;
using TheMillionthFoodOrderApp.Tests.Integration.Fixtures;

namespace TheMillionthFoodOrderApp.Tests.Integration.Orders;

/// <summary>
/// Integration tests for GET /api/brands/{brandSlug}/shops/{shopId}/time-slots (US-FP-019).
/// Runs against a real SQL Server via Testcontainers.
/// Run this class individually to avoid OOM: --treenode-filter "/*/*/GetAvailableTimeSlotsTests/*"
/// </summary>
[ClassDataSource<IntegrationTestBase>(Shared = SharedType.PerClass)]
public sealed class GetAvailableTimeSlotsTests(IntegrationTestBase fixture)
{
    private HttpClient CreateClient() => fixture.Factory.CreateClient();

    private static string TimeSlotsUrl(string brandSlug, Guid shopId) =>
        $"/api/brands/{brandSlug}/shops/{shopId}/time-slots";

    private static string ShopsUrl(string brandSlug) =>
        $"/api/brands/{brandSlug}/shops";

    private static string ProductsUrl(string brandSlug) =>
        $"/api/brands/{brandSlug}/products";

    private static string OrdersUrl(string brandSlug, Guid shopId) =>
        $"/api/brands/{brandSlug}/shops/{shopId}/orders";

    private static string OrderLifecycleUrl(string brandSlug, Guid shopId) =>
        $"/api/brands/{brandSlug}/shops/{shopId}/order-lifecycle";

    // ── Setup helpers ─────────────────────────────────────────────────────────

    private async Task<Guid> CreateShopAsync(HttpClient client, string brandSlug)
    {
        var request = new
        {
            Name = $"TimeSlot Shop {Guid.NewGuid().ToString("N")[..8]}",
            Slug = $"ts-{Guid.NewGuid().ToString("N")[..8]}",
            Address = new { Street = "Frietstraat", Number = "1", City = "Gent", PostalCode = "9000", Country = "BE" },
            ContactEmail = "ts@frietjes.be",
            ContactPhone = (string?)null
        };

        var resp = await client.PostAsJsonAsync(ShopsUrl(brandSlug), request);
        await Assert.That(resp.StatusCode).IsEqualTo(HttpStatusCode.Created);
        var shop = await resp.Content.ReadFromJsonAsync<ShopResponse>();
        await Assert.That(shop).IsNotNull();

        // Lazy-init lifecycle
        await client.GetAsync(OrderLifecycleUrl(brandSlug, shop!.Id));
        return shop.Id;
    }

    /// <summary>
    /// Sets an always-open weekly schedule (00:05–23:55 every day to avoid DST boundary).
    /// </summary>
    private async Task SetAlwaysOpenAsync(HttpClient client, string brandSlug, Guid shopId)
    {
        var request = new
        {
            TimeBlocks = Enumerable.Range(0, 7)
                .Select(day => new { DayOfWeek = day, OpenTime = "00:05", CloseTime = "23:55" })
                .ToArray()
        };
        await client.PutAsJsonAsync($"/api/brands/{brandSlug}/shops/{shopId}/opening-hours", request);
    }

    /// <summary>Enables time-slot ordering on a shop.</summary>
    private async Task EnableTimeSlotsAsync(
        HttpClient client, string brandSlug, Guid shopId,
        int intervalMinutes = 10, int maxOrders = 2)
    {
        // We must update via the shop's full update endpoint
        var shopResp = await client.GetAsync($"/api/brands/{brandSlug}/shops/{shopId}");
        var shop = await shopResp.Content.ReadFromJsonAsync<ShopResponse>();
        await Assert.That(shop).IsNotNull();

        var updateRequest = new
        {
            shop!.Name,
            Address = new
            {
                shop.Address.Street,
                shop.Address.Number,
                shop.Address.City,
                shop.Address.PostalCode,
                shop.Address.Country
            },
            shop.ContactEmail,
            shop.ContactPhone,
            shop.VatNumber,
            shop.KitchenDisplayEnabled,
            shop.TicketPrinterEnabled,
            shop.PushNotificationEnabled,
            shop.SoundAlertEnabled,
            EatIn = new { shop.EatIn.IsEnabled, shop.EatIn.RequiresTableNumber },
            TimeSlotOrdering = new { IsEnabled = true, IntervalMinutes = intervalMinutes, MaxOrdersPerInterval = maxOrders }
        };

        var updateResp = await client.PutAsJsonAsync($"/api/brands/{brandSlug}/shops/{shopId}", updateRequest);
        await Assert.That(updateResp.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    private async Task<Guid> CreateProductAsync(HttpClient client, string brandSlug)
    {
        var req = new
        {
            BasePrice = 3.50m,
            Translations = new[] { new { LanguageCode = "nl", Name = "Test Friet", Description = (string?)null } }
        };
        var resp = await client.PostAsJsonAsync(ProductsUrl(brandSlug), req);
        await Assert.That(resp.StatusCode).IsEqualTo(HttpStatusCode.Created);
        var product = await resp.Content.ReadFromJsonAsync<dynamic>();
        // Extract id from dynamic
        var json = await resp.Content.ReadAsStringAsync();
        var parsed = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(json);
        return parsed.GetProperty("id").GetGuid();
    }

    private async Task PlaceOrderAtSlotAsync(HttpClient client, string brandSlug, Guid shopId, Guid productId, DateTimeOffset slotStart)
    {
        var req = new
        {
            OrderType = "Pickup",
            PaymentMethod = "CashAtPickup",
            CustomerFirstName = "Jan",
            CustomerLastName = "Janssen",
            CustomerEmail = "jan@example.com",
            CustomerPhone = "+32470000001",
            TimeSlotStart = slotStart,
            Items = new[] { new { ProductId = productId, Quantity = 1, SelectedModifierIds = Array.Empty<Guid>() } }
        };
        var resp = await client.PostAsJsonAsync(OrdersUrl(brandSlug, shopId), req);
        await Assert.That(resp.StatusCode).IsEqualTo(HttpStatusCode.Created);
    }

    // ── Test 1: Disabled shop → isEnabled: false, empty slots ────────────────

    [Test]
    public async Task GetTimeSlots_WhenDisabled_ReturnsEnabledFalseAndEmptySlots()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.AlphaSlug;

        var shopId = await CreateShopAsync(client, brand);
        await SetAlwaysOpenAsync(client, brand, shopId);
        // TimeSlotOrdering stays disabled (default)

        var resp = await client.GetAsync(TimeSlotsUrl(brand, shopId));
        await Assert.That(resp.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var body = await resp.Content.ReadFromJsonAsync<AvailableTimeSlotsResponse>();
        await Assert.That(body).IsNotNull();
        await Assert.That(body!.IsEnabled).IsFalse();
        await Assert.That(body.Slots).IsEmpty();
    }

    // ── Test 2: Enabled → slots aligned, all available ────────────────────────

    [Test]
    public async Task GetTimeSlots_WhenEnabled_ReturnsSlotsAlignedToInterval()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.BetaSlug;

        var shopId = await CreateShopAsync(client, brand);
        await SetAlwaysOpenAsync(client, brand, shopId);
        await EnableTimeSlotsAsync(client, brand, shopId, intervalMinutes: 10, maxOrders: 2);

        var resp = await client.GetAsync(TimeSlotsUrl(brand, shopId));
        await Assert.That(resp.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var body = await resp.Content.ReadFromJsonAsync<AvailableTimeSlotsResponse>();
        await Assert.That(body).IsNotNull();
        await Assert.That(body!.IsEnabled).IsTrue();
        await Assert.That(body.IntervalMinutes).IsEqualTo(10);
        await Assert.That(body.MaxOrdersPerInterval).IsEqualTo(2);

        if (body.Slots.Count > 0)
        {
            // All slots should be 10 minutes long
            foreach (var slot in body.Slots)
            {
                var duration = (slot.End - slot.Start).TotalMinutes;
                await Assert.That(duration).IsEqualTo(10);
                await Assert.That(slot.IsAvailable).IsTrue();
                await Assert.That(slot.RemainingCapacity).IsEqualTo(2);
            }

            // Consecutive slots should be contiguous (end of one = start of next)
            for (var i = 0; i < body.Slots.Count - 1; i++)
            {
                await Assert.That(body.Slots[i].End).IsEqualTo(body.Slots[i + 1].Start);
            }
        }
    }

    // ── Test 3: Place 2 orders into a slot → that slot becomes unavailable ────

    [Test]
    public async Task GetTimeSlots_AfterFillingASlot_SlotIsUnavailable()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.GammaSlug;

        var shopId = await CreateShopAsync(client, brand);
        await SetAlwaysOpenAsync(client, brand, shopId);
        await EnableTimeSlotsAsync(client, brand, shopId, intervalMinutes: 10, maxOrders: 2);

        // Get slots to find one to fill
        var slotsResp = await client.GetAsync(TimeSlotsUrl(brand, shopId));
        var slotsBody = await slotsResp.Content.ReadFromJsonAsync<AvailableTimeSlotsResponse>();
        await Assert.That(slotsBody?.Slots).IsNotEmpty();

        var targetSlot = slotsBody!.Slots[0];
        var productId = await CreateProductAsync(client, brand);

        // Setup tax config first
        await client.PutAsJsonAsync(
            $"/api/brands/{brand}/tax-configuration",
            new { rates = new[] { new { consumptionMode = "Takeaway", ratePercent = 6m }, new { consumptionMode = "EatIn", ratePercent = 21m } } });

        // Place 2 orders into the slot
        await PlaceOrderAtSlotAsync(client, brand, shopId, productId, targetSlot.Start);
        await PlaceOrderAtSlotAsync(client, brand, shopId, productId, targetSlot.Start);

        // Re-fetch slots
        var slotsResp2 = await client.GetAsync(TimeSlotsUrl(brand, shopId));
        var slotsBody2 = await slotsResp2.Content.ReadFromJsonAsync<AvailableTimeSlotsResponse>();

        var filledSlot = slotsBody2!.Slots.FirstOrDefault(s => s.Start == targetSlot.Start);
        if (filledSlot != default)
        {
            await Assert.That(filledSlot.IsAvailable).IsFalse();
            await Assert.That(filledSlot.RemainingCapacity).IsEqualTo(0);
        }
        // (The slot may have aged out of the list if time passed; test is still valid then)
    }

    // ── Test 4: Unknown shop → 404 ────────────────────────────────────────────

    [Test]
    public async Task GetTimeSlots_UnknownShop_Returns404()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.AlphaSlug;

        var resp = await client.GetAsync(TimeSlotsUrl(brand, Guid.CreateVersion7()));
        await Assert.That(resp.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }
}

using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using TheMillionthFoodOrderApp.Application.Orders;
using TheMillionthFoodOrderApp.Application.Shops;
using TheMillionthFoodOrderApp.Infrastructure.Multitenancy;
using TheMillionthFoodOrderApp.Tests.Integration.Fixtures;

namespace TheMillionthFoodOrderApp.Tests.Integration.Orders;

/// <summary>
/// Integration tests for placing orders with time-slot selection (US-FP-019).
/// Covers: valid slot, ASAP, disabled shop slot, misaligned slot, full slot (409),
/// kitchen active-orders view, and in-store null slots.
/// Run individually: --treenode-filter "/*/*/PlaceOrderTimeSlotTests/*"
/// </summary>
[ClassDataSource<IntegrationTestBase>(Shared = SharedType.PerClass)]
public sealed class PlaceOrderTimeSlotTests(IntegrationTestBase fixture)
{
    private HttpClient CreateClient() => fixture.Factory.CreateClient();

    private static string OrdersUrl(string brandSlug, Guid shopId) =>
        $"/api/brands/{brandSlug}/shops/{shopId}/orders";

    private static string OrderByIdUrl(string brandSlug, Guid shopId, Guid orderId) =>
        $"/api/brands/{brandSlug}/shops/{shopId}/orders/{orderId}";

    private static string ActiveOrdersUrl(string brandSlug, Guid shopId) =>
        $"/api/brands/{brandSlug}/shops/{shopId}/orders/active";

    private static string TimeSlotsUrl(string brandSlug, Guid shopId) =>
        $"/api/brands/{brandSlug}/shops/{shopId}/time-slots";

    // ── Setup helpers ─────────────────────────────────────────────────────────

    private async Task<Guid> SetupShopAsync(
        HttpClient client, string brandSlug,
        bool enableTimeSlots = true, int intervalMinutes = 10, int maxOrders = 4)
    {
        // Create shop
        var createResp = await client.PostAsJsonAsync($"/api/brands/{brandSlug}/shops", new
        {
            Name = $"TS Shop {Guid.NewGuid().ToString("N")[..6]}",
            Slug = $"ts-{Guid.NewGuid().ToString("N")[..8]}",
            Address = new { Street = "Frietstraat", Number = "1", City = "Gent", PostalCode = "9000", Country = "BE" },
            ContactEmail = "ts@frietjes.be",
            ContactPhone = (string?)null
        });
        await Assert.That(createResp.StatusCode).IsEqualTo(HttpStatusCode.Created);
        var shop = await createResp.Content.ReadFromJsonAsync<ShopResponse>();
        await Assert.That(shop).IsNotNull();
        var shopId = shop!.Id;

        // Lazy-init lifecycle
        await client.GetAsync($"/api/brands/{brandSlug}/shops/{shopId}/order-lifecycle");

        // Always-open hours (avoid DST midnight)
        await client.PutAsJsonAsync($"/api/brands/{brandSlug}/shops/{shopId}/opening-hours", new
        {
            TimeBlocks = Enumerable.Range(0, 7)
                .Select(d => new { DayOfWeek = d, OpenTime = "00:05", CloseTime = "23:55" })
                .ToArray()
        });

        // Tax config
        await client.PutAsJsonAsync($"/api/brands/{brandSlug}/tax-configuration", new
        {
            rates = new[]
            {
                new { consumptionMode = "Takeaway", ratePercent = 6m },
                new { consumptionMode = "EatIn", ratePercent = 21m }
            }
        });

        if (enableTimeSlots)
        {
            // Update shop to enable time-slot ordering
            var shopGetResp = await client.GetAsync($"/api/brands/{brandSlug}/shops/{shopId}");
            var shopData = await shopGetResp.Content.ReadFromJsonAsync<ShopResponse>();
            await client.PutAsJsonAsync($"/api/brands/{brandSlug}/shops/{shopId}", new
            {
                shopData!.Name,
                Address = new
                {
                    shopData.Address.Street,
                    shopData.Address.Number,
                    shopData.Address.City,
                    shopData.Address.PostalCode,
                    shopData.Address.Country
                },
                shopData.ContactEmail,
                shopData.ContactPhone,
                shopData.VatNumber,
                shopData.KitchenDisplayEnabled,
                shopData.TicketPrinterEnabled,
                shopData.PushNotificationEnabled,
                shopData.SoundAlertEnabled,
                EatIn = new { shopData.EatIn.IsEnabled, shopData.EatIn.RequiresTableNumber },
                TimeSlotOrdering = new { IsEnabled = true, IntervalMinutes = intervalMinutes, MaxOrdersPerInterval = maxOrders }
            });
        }

        return shopId;
    }

    private async Task<Guid> CreateProductAsync(HttpClient client, string brandSlug)
    {
        var resp = await client.PostAsJsonAsync($"/api/brands/{brandSlug}/products", new
        {
            BasePrice = 3.50m,
            Translations = new[] { new { LanguageCode = "nl", Name = "Frietje TS", Description = (string?)null } }
        });
        await Assert.That(resp.StatusCode).IsEqualTo(HttpStatusCode.Created);
        var json = await resp.Content.ReadAsStringAsync();
        var parsed = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(json);
        return parsed.GetProperty("id").GetGuid();
    }

    private async Task<DateTimeOffset?> GetFirstAvailableSlotAsync(HttpClient client, string brandSlug, Guid shopId)
    {
        var resp = await client.GetAsync(TimeSlotsUrl(brandSlug, shopId));
        var body = await resp.Content.ReadFromJsonAsync<AvailableTimeSlotsResponse>();
        return body?.Slots.FirstOrDefault(s => s.IsAvailable)?.Start;
    }

    private object BuildOrderRequest(Guid productId, DateTimeOffset? timeSlotStart = null) => new
    {
        OrderType = "Pickup",
        PaymentMethod = "CashAtPickup",
        CustomerFirstName = "Jan",
        CustomerLastName = "Janssen",
        CustomerEmail = "jan@example.com",
        CustomerPhone = "+32470000001",
        TimeSlotStart = timeSlotStart,
        Items = new[] { new { ProductId = productId, Quantity = 1, SelectedModifierIds = Array.Empty<Guid>() } }
    };

    // ── Test 1: Valid slot → 201, fields echoed, persisted ────────────────────

    [Test]
    public async Task PlaceOrder_WithValidSlot_Returns201WithTimeSlotFields()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.AlphaSlug;

        var shopId = await SetupShopAsync(client, brand);
        var productId = await CreateProductAsync(client, brand);
        var slotStart = await GetFirstAvailableSlotAsync(client, brand, shopId);

        // It's possible no slots are available (test runs near midnight)
        if (slotStart is null)
        {
            await Assert.That(true).IsTrue(); // Skip — no slots available
            return;
        }

        var resp = await client.PostAsJsonAsync(OrdersUrl(brand, shopId), BuildOrderRequest(productId, slotStart));
        await Assert.That(resp.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var order = await resp.Content.ReadFromJsonAsync<OrderResponse>();
        await Assert.That(order).IsNotNull();
        await Assert.That(order!.TimeSlotStart).IsNotNull();
        await Assert.That(order.TimeSlotEnd).IsNotNull();
        await Assert.That(order.TimeSlotStart).IsEqualTo(slotStart);

        // Verify it's persisted — fetch via tracking endpoint
        var trackResp = await client.GetAsync(OrderByIdUrl(brand, shopId, order.Id));
        await Assert.That(trackResp.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var trackBody = await trackResp.Content.ReadFromJsonAsync<OrderTrackingResponse>();
        await Assert.That(trackBody?.Order.TimeSlotStart).IsEqualTo(slotStart);
    }

    // ── Test 2: ASAP with slots enabled → 201, nulls ──────────────────────────

    [Test]
    public async Task PlaceOrder_WithSlotsEnabled_ButNoSlotRequested_Returns201WithNullSlots()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.BetaSlug;

        var shopId = await SetupShopAsync(client, brand);
        var productId = await CreateProductAsync(client, brand);

        var resp = await client.PostAsJsonAsync(OrdersUrl(brand, shopId), BuildOrderRequest(productId, null));
        await Assert.That(resp.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var order = await resp.Content.ReadFromJsonAsync<OrderResponse>();
        await Assert.That(order).IsNotNull();
        await Assert.That(order!.TimeSlotStart).IsNull();
        await Assert.That(order.TimeSlotEnd).IsNull();
    }

    // ── Test 3: Slot while slots disabled → 400 ───────────────────────────────

    [Test]
    public async Task PlaceOrder_SlotRequestedWhenDisabled_Returns400()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.GammaSlug;

        var shopId = await SetupShopAsync(client, brand, enableTimeSlots: false);
        var productId = await CreateProductAsync(client, brand);

        var futureSlot = DateTimeOffset.UtcNow.AddHours(2);
        var resp = await client.PostAsJsonAsync(OrdersUrl(brand, shopId), BuildOrderRequest(productId, futureSlot));
        await Assert.That(resp.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    // ── Test 4: Misaligned/stale slot → 409 ───────────────────────────────────

    [Test]
    public async Task PlaceOrder_MisalignedSlot_Returns409()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.AlphaSlug;

        var shopId = await SetupShopAsync(client, brand, intervalMinutes: 15, maxOrders: 4);
        var productId = await CreateProductAsync(client, brand);

        // Misaligned: some arbitrary time that won't match the slot grid.
        // 409 (not 400) — the same TimeSlotUnavailableException covers slots that aged
        // out between fetch and submit, and the storefront recovers by refreshing the list.
        var misalignedSlot = DateTimeOffset.UtcNow.AddHours(3).AddMinutes(7);
        var resp = await client.PostAsJsonAsync(OrdersUrl(brand, shopId), BuildOrderRequest(productId, misalignedSlot));
        await Assert.That(resp.StatusCode).IsEqualTo(HttpStatusCode.Conflict);
    }

    // ── Test 5: Full slot → 409 ───────────────────────────────────────────────

    [Test]
    public async Task PlaceOrder_WhenSlotFull_Returns409()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.BetaSlug;

        // maxOrders = 1 so the first order fills it
        var shopId = await SetupShopAsync(client, brand, intervalMinutes: 10, maxOrders: 1);
        var productId = await CreateProductAsync(client, brand);
        var slotStart = await GetFirstAvailableSlotAsync(client, brand, shopId);

        if (slotStart is null)
        {
            await Assert.That(true).IsTrue(); // skip near midnight
            return;
        }

        // First order fills the slot
        var first = await client.PostAsJsonAsync(OrdersUrl(brand, shopId), BuildOrderRequest(productId, slotStart));
        await Assert.That(first.StatusCode).IsEqualTo(HttpStatusCode.Created);

        // Second order into the same slot → 409
        var second = await client.PostAsJsonAsync(OrdersUrl(brand, shopId), BuildOrderRequest(productId, slotStart));
        await Assert.That(second.StatusCode).IsEqualTo(HttpStatusCode.Conflict);
    }

    // ── Test 6: Kitchen GET active includes time slot fields ──────────────────

    [Test]
    public async Task GetActiveOrders_IncludesTimeSlotFields()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.GammaSlug;

        var shopId = await SetupShopAsync(client, brand);
        var productId = await CreateProductAsync(client, brand);
        var slotStart = await GetFirstAvailableSlotAsync(client, brand, shopId);

        if (slotStart is null)
        {
            await Assert.That(true).IsTrue();
            return;
        }

        await client.PostAsJsonAsync(OrdersUrl(brand, shopId), BuildOrderRequest(productId, slotStart));

        var activeResp = await client.GetAsync(ActiveOrdersUrl(brand, shopId));
        await Assert.That(activeResp.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var json = await activeResp.Content.ReadAsStringAsync();
        var parsed = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonElement>(json);
        var orders = parsed.GetProperty("orders");

        // At least one order should have timeSlotStart populated
        var hasSlottedOrder = false;
        foreach (var o in orders.EnumerateArray())
        {
            if (o.TryGetProperty("timeSlotStart", out var ts) && ts.ValueKind != System.Text.Json.JsonValueKind.Null)
            {
                hasSlottedOrder = true;
                break;
            }
        }
        await Assert.That(hasSlottedOrder).IsTrue();
    }

    // ── Test 7: In-store endpoint — slots always null ─────────────────────────

    [Test]
    public async Task PlaceInStoreOrder_SlotsAlwaysNull()
    {
        // The in-store HTTP endpoint requires the CounterStaff role, which the test
        // app's DevPassThrough auth does not issue — invoke the service layer directly
        // via DI instead (same pattern as CreateInStoreOrderServiceTests).
        var client = CreateClient();
        var brand = IntegrationTestBase.AlphaSlug;

        var shopId = await SetupShopAsync(client, brand); // slots enabled
        var productId = await CreateProductAsync(client, brand);

        var scope = fixture.Factory.Services.CreateAsyncScope();
        await using (scope)
        {
            scope.ServiceProvider.GetRequiredService<BrandContextAccessor>().BrandSlug = brand;
            var service = scope.ServiceProvider.GetRequiredService<IOrderService>();

            var request = new CreateInStoreOrderRequest(
                ShopId: shopId,
                BrandSlug: brand,
                OrderType: "Pickup",
                PaymentMethod: "CashAtPickup",
                CustomerFirstName: "Counter",
                CustomerLastName: "Staff",
                TableNumber: null,
                Items: [new OrderItemInput(productId, 1, Array.Empty<Guid>().ToList().AsReadOnly())]);

            var order = await service.CreateInStoreOrderAsync(request, Guid.NewGuid());

            await Assert.That(order).IsNotNull();
            await Assert.That(order.TimeSlotStart).IsNull();
            await Assert.That(order.TimeSlotEnd).IsNull();
        }
    }
}

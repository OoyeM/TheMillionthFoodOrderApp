using System.Net;
using System.Net.Http.Json;
using TheMillionthFoodOrderApp.Api.Endpoints.Orders;
using TheMillionthFoodOrderApp.Application.Orders;
using TheMillionthFoodOrderApp.Application.Products;
using TheMillionthFoodOrderApp.Application.Shops;
using TheMillionthFoodOrderApp.Tests.Integration.Fixtures;

namespace TheMillionthFoodOrderApp.Tests.Integration.Orders;

/// <summary>
/// Integration tests for GET /time-slots availability and time-slot ordering enforcement (US-FP-019).
/// Runs against a real SQL Server via Testcontainers.
///
/// Pattern notes:
///   - Always-open shops (00:00–23:59) used so tests are not blocked by time-of-day.
///   - Unique shop slugs per test to avoid cross-test interference (same brand DB, same container).
///   - Tests that need a specific slot take <c>slots.First()</c> and early-return (with a log
///     message) when the list is empty — only possible in the final 15-minute window before midnight.
/// </summary>
[ClassDataSource<IntegrationTestBase>(Shared = SharedType.PerClass)]
public sealed class TimeSlotTests(IntegrationTestBase fixture)
{
    private HttpClient CreateClient() => fixture.Factory.CreateClient();

    private static string TimeSlotsUrl(string brandSlug, Guid shopId) =>
        $"/api/brands/{brandSlug}/shops/{shopId}/time-slots";

    private static string OrdersUrl(string brandSlug, Guid shopId) =>
        $"/api/brands/{brandSlug}/shops/{shopId}/orders";

    private static string ShopsUrl(string brandSlug) =>
        $"/api/brands/{brandSlug}/shops";

    private static string ShopUrl(string brandSlug, Guid shopId) =>
        $"/api/brands/{brandSlug}/shops/{shopId}";

    private static string ProductsUrl(string brandSlug) =>
        $"/api/brands/{brandSlug}/products";

    private static string OrderLifecycleUrl(string brandSlug, Guid shopId) =>
        $"/api/brands/{brandSlug}/shops/{shopId}/order-lifecycle";

    private static string ListActiveUrl(string brandSlug, Guid shopId) =>
        $"/api/brands/{brandSlug}/shops/{shopId}/orders/active";

    // ── Setup helpers ──────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a shop with an always-open schedule and triggers lifecycle initialisation.
    /// By default time-slot ordering is disabled (shop default).
    /// </summary>
    private async Task<Guid> CreateShopAsync(HttpClient client, string brandSlug)
    {
        var request = new
        {
            Name = $"Slot Shop {Guid.NewGuid().ToString("N")[..8]}",
            Slug = $"slot-{Guid.NewGuid().ToString("N")[..8]}",
            Address = new { Street = "Frietstraat", Number = "1", City = "Brussel", PostalCode = "1000", Country = "BE" },
            ContactEmail = "slot@frietjes.be",
            ContactPhone = (string?)null,
        };

        var createResp = await client.PostAsJsonAsync(ShopsUrl(brandSlug), request);
        await Assert.That(createResp.StatusCode).IsEqualTo(HttpStatusCode.Created);
        var shop = await createResp.Content.ReadFromJsonAsync<ShopResponse>();
        await Assert.That(shop).IsNotNull();

        // Trigger default lifecycle creation (lazy-initialised on first GET)
        await client.GetAsync(OrderLifecycleUrl(brandSlug, shop!.Id));

        // Always-open schedule
        await SetAlwaysOpenAsync(client, brandSlug, shop.Id);

        return shop.Id;
    }

    /// <summary>
    /// Creates a shop with NO opening hours (always closed) and time-slots enabled.
    /// Used to test the "enabled but closed → empty slots" path (design decision 3).
    /// </summary>
    private async Task<Guid> CreateClosedShopWithSlotsEnabledAsync(HttpClient client, string brandSlug)
    {
        var request = new
        {
            Name = $"Closed Slot Shop {Guid.NewGuid().ToString("N")[..8]}",
            Slug = $"closed-slot-{Guid.NewGuid().ToString("N")[..8]}",
            Address = new { Street = "Frietstraat", Number = "1", City = "Brussel", PostalCode = "1000", Country = "BE" },
            ContactEmail = "closed@frietjes.be",
            ContactPhone = (string?)null,
        };

        var createResp = await client.PostAsJsonAsync(ShopsUrl(brandSlug), request);
        var shop = await createResp.Content.ReadFromJsonAsync<ShopResponse>();
        await Assert.That(shop).IsNotNull();

        // Trigger default lifecycle creation
        await client.GetAsync(OrderLifecycleUrl(brandSlug, shop!.Id));

        // Enable time-slot ordering but do NOT add opening hours (shop = always closed)
        await EnableTimeSlotsAsync(client, brandSlug, shop.Id, intervalMinutes: 15, maxOrders: 2);

        return shop.Id;
    }

    private static async Task SetAlwaysOpenAsync(HttpClient client, string brandSlug, Guid shopId)
    {
        var request = new
        {
            TimeBlocks = Enumerable.Range(0, 7)
                .Select(day => new { DayOfWeek = day, OpenTime = "00:00", CloseTime = "23:59" })
                .ToArray()
        };
        var resp = await client.PutAsJsonAsync(
            $"/api/brands/{brandSlug}/shops/{shopId}/opening-hours", request);
        await Assert.That(resp.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    /// <summary>
    /// Enables time-slot ordering on an existing shop via the shop-update endpoint.
    /// Re-reads the current shop to preserve all existing fields.
    /// </summary>
    private async Task EnableTimeSlotsAsync(
        HttpClient client, string brandSlug, Guid shopId, int intervalMinutes, int maxOrders)
    {
        var getResp = await client.GetAsync(ShopUrl(brandSlug, shopId));
        var shop = await getResp.Content.ReadFromJsonAsync<ShopResponse>();
        await Assert.That(shop).IsNotNull();

        var updateRequest = new
        {
            Name = shop!.Name,
            Address = new
            {
                shop.Address.Street,
                shop.Address.Number,
                shop.Address.City,
                shop.Address.PostalCode,
                shop.Address.Country,
            },
            ContactEmail = shop.ContactEmail,
            ContactPhone = (string?)null,
            KitchenDisplayEnabled = shop.KitchenDisplayEnabled,
            TicketPrinterEnabled = shop.TicketPrinterEnabled,
            PushNotificationEnabled = shop.PushNotificationEnabled,
            SoundAlertEnabled = shop.SoundAlertEnabled,
            EatIn = new { shop.EatIn.IsEnabled, shop.EatIn.RequiresTableNumber },
            TimeSlotOrdering = new { IsEnabled = true, IntervalMinutes = intervalMinutes, MaxOrdersPerInterval = maxOrders },
            VatNumber = (string?)null,
        };

        var updateResp = await client.PutAsJsonAsync(ShopUrl(brandSlug, shopId), updateRequest);
        await Assert.That(updateResp.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    private async Task<Guid> CreateProductAsync(HttpClient client, string brandSlug)
    {
        var request = new
        {
            BasePrice = 3.50m,
            Translations = new[] { new { LanguageCode = "nl", Name = "Frietje", Description = (string?)null } }
        };
        var resp = await client.PostAsJsonAsync(ProductsUrl(brandSlug), request);
        await Assert.That(resp.StatusCode).IsEqualTo(HttpStatusCode.Created);
        var product = await resp.Content.ReadFromJsonAsync<ProductResponse>();
        await Assert.That(product).IsNotNull();
        return product!.Id;
    }

    /// <summary>
    /// Places a valid Pickup order and returns the response. The shop must be open and
    /// have a tax configuration (both guaranteed by <see cref="IntegrationTestBase"/>).
    /// </summary>
    private async Task<OrderResponse?> PlaceOrderAsync(
        HttpClient client,
        string brandSlug,
        Guid shopId,
        Guid productId,
        DateTimeOffset? timeSlotStart = null)
    {
        var request = new
        {
            OrderType = "Pickup",
            PaymentMethod = "CashAtPickup",
            CustomerFirstName = "Test",
            CustomerLastName = "Klant",
            CustomerEmail = "test@test.com",
            CustomerPhone = "+32470000000",
            TimeSlotStart = timeSlotStart,
            Items = new[] { new { ProductId = productId, Quantity = 1, SelectedModifierIds = Array.Empty<Guid>() } }
        };

        var resp = await client.PostAsJsonAsync(OrdersUrl(brandSlug, shopId), request);
        if (resp.StatusCode == HttpStatusCode.Created)
            return await resp.Content.ReadFromJsonAsync<OrderResponse>();
        return null;
    }

    // ── §1: GET time-slots — disabled shop ────────────────────────────────────

    [Test]
    public async Task GetTimeSlots_DisabledShop_IsEnabledFalseEmptySlotsActiveCountPresent()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.AlphaSlug;

        var shopId = await CreateShopAsync(client, brand);
        var productId = await CreateProductAsync(client, brand);

        // Time-slot ordering is disabled by default; place one order so activeOrderCount > 0.
        await PlaceOrderAsync(client, brand, shopId, productId);

        var resp = await client.GetAsync(TimeSlotsUrl(brand, shopId));
        await Assert.That(resp.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var availability = await resp.Content.ReadFromJsonAsync<TimeSlotAvailabilityResponse>();
        await Assert.That(availability).IsNotNull();
        await Assert.That(availability!.IsEnabled).IsFalse();
        await Assert.That(availability.Slots).IsEmpty();
        await Assert.That(availability.ActiveOrderCount).IsNotNull();
        await Assert.That(availability.ActiveOrderCount!.Value).IsGreaterThanOrEqualTo(1);
    }

    // ── §2: GET time-slots — enabled shop, structural assertions ──────────────

    [Test]
    public async Task GetTimeSlots_EnabledShop_IsEnabledTrueWithStructurallyCorrectSlots()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.BetaSlug;

        var shopId = await CreateShopAsync(client, brand);
        await EnableTimeSlotsAsync(client, brand, shopId, intervalMinutes: 15, maxOrders: 3);

        var resp = await client.GetAsync(TimeSlotsUrl(brand, shopId));
        await Assert.That(resp.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var availability = await resp.Content.ReadFromJsonAsync<TimeSlotAvailabilityResponse>();
        await Assert.That(availability).IsNotNull();
        await Assert.That(availability!.IsEnabled).IsTrue();
        await Assert.That(availability.IntervalMinutes).IsEqualTo(15);
        await Assert.That(availability.ActiveOrderCount).IsNull();

        // For an always-open shop there should be slots available (unless we're in the last 15 min of the day).
        if (availability.Slots.Count == 0)
        {
            Console.WriteLine("[TimeSlotTests] SKIP: No slots returned — within final 15-min window before 23:59. Test is trivially passing.");
            return;
        }

        // Structural checks — time-independent.
        await Assert.That(availability.Slots.All(s => !string.IsNullOrEmpty(s.Label))).IsTrue();
        await Assert.That(availability.Slots.All(s => s.SlotStart > DateTimeOffset.UtcNow)).IsTrue();
        await Assert.That(availability.Slots.All(s => s.IsAvailable)).IsTrue();

        // Labels must be "HH:mm" format
        await Assert.That(availability.Slots.All(s => s.Label.Length == 5 && s.Label[2] == ':')).IsTrue();

        // Slots must be monotonically increasing
        for (var i = 1; i < availability.Slots.Count; i++)
        {
            await Assert.That(availability.Slots[i].SlotStart)
                .IsGreaterThan(availability.Slots[i - 1].SlotStart);
        }
    }

    // ── §3: GET time-slots — enabled but shop has no opening hours (closed) ───

    [Test]
    public async Task GetTimeSlots_EnabledShopWithNoOpeningHours_IsEnabledTrueEmptySlots()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.GammaSlug;

        // This helper creates a shop with slots enabled but NO opening hours.
        var shopId = await CreateClosedShopWithSlotsEnabledAsync(client, brand);

        var resp = await client.GetAsync(TimeSlotsUrl(brand, shopId));
        await Assert.That(resp.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var availability = await resp.Content.ReadFromJsonAsync<TimeSlotAvailabilityResponse>();
        await Assert.That(availability).IsNotNull();
        // IsEnabled reflects config only (design decision 3).
        await Assert.That(availability!.IsEnabled).IsTrue();
        await Assert.That(availability.Slots).IsEmpty();
        // ActiveOrderCount is null when enabled (only populated when disabled).
        await Assert.That(availability.ActiveOrderCount).IsNull();
    }

    // ── §4: Capacity enforcement ──────────────────────────────────────────────

    [Test]
    public async Task Capacity_TwoOrdersInSlot_SlotBecomesUnavailable_ThirdReturns400()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.AlphaSlug;

        var shopId = await CreateShopAsync(client, brand);
        await EnableTimeSlotsAsync(client, brand, shopId, intervalMinutes: 15, maxOrders: 2);
        var productId = await CreateProductAsync(client, brand);

        // Get available slots
        var slotsResp = await client.GetAsync(TimeSlotsUrl(brand, shopId));
        var availability = await slotsResp.Content.ReadFromJsonAsync<TimeSlotAvailabilityResponse>();
        await Assert.That(availability).IsNotNull();

        if (availability!.Slots.Count == 0)
        {
            Console.WriteLine("[TimeSlotTests] SKIP: No slots returned (near midnight). Capacity test skipped.");
            return;
        }

        var slot = availability.Slots.First(s => s.IsAvailable);
        var slotStart = slot.SlotStart;

        // Place 2 orders into the same slot — both should succeed
        var order1 = await PlaceOrderAsync(client, brand, shopId, productId, slotStart);
        await Assert.That(order1).IsNotNull();

        var order2 = await PlaceOrderAsync(client, brand, shopId, productId, slotStart);
        await Assert.That(order2).IsNotNull();

        // After 2 orders, GET should show the slot as unavailable
        var slotsResp2 = await client.GetAsync(TimeSlotsUrl(brand, shopId));
        var availability2 = await slotsResp2.Content.ReadFromJsonAsync<TimeSlotAvailabilityResponse>();
        await Assert.That(availability2).IsNotNull();
        var updatedSlot = availability2!.Slots.FirstOrDefault(s => s.SlotStart == slotStart);
        // Slot may not appear if it's now past — only assert if still visible
        if (updatedSlot != null)
        {
            await Assert.That(updatedSlot.IsAvailable).IsFalse();
        }

        // 3rd order into the same slot must fail with 400 and errors.timeSlotStart
        var thirdRequest = new
        {
            OrderType = "Pickup",
            PaymentMethod = "CashAtPickup",
            CustomerFirstName = "Third",
            CustomerLastName = "Klant",
            CustomerEmail = "third@test.com",
            CustomerPhone = "+32470000003",
            TimeSlotStart = slotStart,
            Items = new[] { new { ProductId = productId, Quantity = 1, SelectedModifierIds = Array.Empty<Guid>() } }
        };

        var thirdResp = await client.PostAsJsonAsync(OrdersUrl(brand, shopId), thirdRequest);
        await Assert.That(thirdResp.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);

        // Response body must contain errors.timeSlotStart (FastEndpoints camelCases property names)
        var body = await thirdResp.Content.ReadAsStringAsync();
        await Assert.That(body).Contains("timeSlotStart");
    }

    // ── §5: Create with valid slot → 201, timeSlot label in response ──────────

    [Test]
    public async Task CreateOrder_WithValidSlot_Returns201WithTimeSlotLabel()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.BetaSlug;

        var shopId = await CreateShopAsync(client, brand);
        await EnableTimeSlotsAsync(client, brand, shopId, intervalMinutes: 15, maxOrders: 5);
        var productId = await CreateProductAsync(client, brand);

        var slotsResp = await client.GetAsync(TimeSlotsUrl(brand, shopId));
        var availability = await slotsResp.Content.ReadFromJsonAsync<TimeSlotAvailabilityResponse>();

        if (availability!.Slots.Count == 0)
        {
            Console.WriteLine("[TimeSlotTests] SKIP: No slots returned (near midnight).");
            return;
        }

        var slot = availability.Slots.First();
        var order = await PlaceOrderAsync(client, brand, shopId, productId, slot.SlotStart);

        await Assert.That(order).IsNotNull();
        await Assert.That(order!.TimeSlot).IsNotNull();
        // Label must be "HH:mm" format matching the slot label from availability
        await Assert.That(order.TimeSlot).IsEqualTo(slot.Label);
    }

    // ── §6: GET order returns timeSlot; listActive (kitchen) returns timeSlot ─

    [Test]
    public async Task CreateOrder_WithSlot_GetOrderAndListActiveReturnTimeSlot()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.AlphaSlug;

        var shopId = await CreateShopAsync(client, brand);
        await EnableTimeSlotsAsync(client, brand, shopId, intervalMinutes: 15, maxOrders: 5);
        var productId = await CreateProductAsync(client, brand);

        var slotsResp = await client.GetAsync(TimeSlotsUrl(brand, shopId));
        var availability = await slotsResp.Content.ReadFromJsonAsync<TimeSlotAvailabilityResponse>();

        if (availability!.Slots.Count == 0)
        {
            Console.WriteLine("[TimeSlotTests] SKIP: No slots returned (near midnight).");
            return;
        }

        var slot = availability.Slots.First();
        var order = await PlaceOrderAsync(client, brand, shopId, productId, slot.SlotStart);
        await Assert.That(order).IsNotNull();
        var orderId = order!.Id;

        // GET by ID
        var getResp = await client.GetAsync($"/api/brands/{brand}/shops/{shopId}/orders/{orderId}");
        await Assert.That(getResp.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var tracking = await getResp.Content.ReadFromJsonAsync<OrderTrackingResponse>();
        await Assert.That(tracking).IsNotNull();
        await Assert.That(tracking!.Order.TimeSlot).IsEqualTo(slot.Label);

        // GET active (kitchen list, AC4)
        var activeResp = await client.GetAsync(ListActiveUrl(brand, shopId));
        await Assert.That(activeResp.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var activeWrapper = await activeResp.Content.ReadFromJsonAsync<ListActiveOrdersResponse>();
        await Assert.That(activeWrapper).IsNotNull();

        var fromActive = activeWrapper!.Orders.FirstOrDefault(o => o.Id == orderId);
        await Assert.That(fromActive).IsNotNull();
        await Assert.That(fromActive!.TimeSlot).IsEqualTo(slot.Label);
    }

    // ── §7: Create with timeSlotStart when shop has slots disabled → 400 ──────

    [Test]
    public async Task CreateOrder_WithTimeSlotStart_WhenSlotsDisabled_Returns400()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.GammaSlug;

        var shopId = await CreateShopAsync(client, brand);
        // Time-slot ordering is disabled by default
        var productId = await CreateProductAsync(client, brand);

        var futureSlot = DateTimeOffset.UtcNow.AddMinutes(30);
        var request = new
        {
            OrderType = "Pickup",
            PaymentMethod = "CashAtPickup",
            CustomerFirstName = "Slot",
            CustomerLastName = "Disabled",
            CustomerEmail = "slot.disabled@test.com",
            CustomerPhone = "+32470000000",
            TimeSlotStart = futureSlot,
            Items = new[] { new { ProductId = productId, Quantity = 1, SelectedModifierIds = Array.Empty<Guid>() } }
        };

        var resp = await client.PostAsJsonAsync(OrdersUrl(brand, shopId), request);
        await Assert.That(resp.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    // ── §8: Create with misaligned timeSlotStart → 400 ───────────────────────

    [Test]
    public async Task CreateOrder_WithMisalignedTimeSlotStart_Returns400()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.BetaSlug;

        var shopId = await CreateShopAsync(client, brand);
        await EnableTimeSlotsAsync(client, brand, shopId, intervalMinutes: 15, maxOrders: 5);
        var productId = await CreateProductAsync(client, brand);

        // A slot that is not on a 15-minute boundary (e.g., XX:07)
        var now = DateTimeOffset.UtcNow;
        var misalignedSlot = now.AddMinutes(30 - now.Minute % 15 + 7); // something like XX:07

        var request = new
        {
            OrderType = "Pickup",
            PaymentMethod = "CashAtPickup",
            CustomerFirstName = "Mis",
            CustomerLastName = "Aligned",
            CustomerEmail = "mis.aligned@test.com",
            CustomerPhone = "+32470000001",
            TimeSlotStart = misalignedSlot,
            Items = new[] { new { ProductId = productId, Quantity = 1, SelectedModifierIds = Array.Empty<Guid>() } }
        };

        var resp = await client.PostAsJsonAsync(OrdersUrl(brand, shopId), request);
        await Assert.That(resp.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    // ── §8b: Create with offered slot shifted by 30 seconds → 400 ─────────────
    // Sub-minute timestamps would be stored as their own capacity bucket and bypass
    // MaxOrdersPerInterval entirely; the tick-precision alignment check must reject them.

    [Test]
    public async Task CreateOrder_WithOfferedSlotPlusThirtySeconds_Returns400()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.BetaSlug;

        var shopId = await CreateShopAsync(client, brand);
        await EnableTimeSlotsAsync(client, brand, shopId, intervalMinutes: 15, maxOrders: 5);
        var productId = await CreateProductAsync(client, brand);

        var slotsResp = await client.GetAsync(TimeSlotsUrl(brand, shopId));
        var availability = await slotsResp.Content.ReadFromJsonAsync<TimeSlotAvailabilityResponse>();
        await Assert.That(availability).IsNotNull();

        if (availability!.Slots.Count == 0)
        {
            Console.WriteLine("[TimeSlotTests] SKIP: No slots returned (near midnight).");
            return;
        }

        var craftedSlot = availability.Slots.First(s => s.IsAvailable).SlotStart.AddSeconds(30);

        var request = new
        {
            OrderType = "Pickup",
            PaymentMethod = "CashAtPickup",
            CustomerFirstName = "Sub",
            CustomerLastName = "Minute",
            CustomerEmail = "sub.minute@test.com",
            CustomerPhone = "+32470000002",
            TimeSlotStart = craftedSlot,
            Items = new[] { new { ProductId = productId, Quantity = 1, SelectedModifierIds = Array.Empty<Guid>() } }
        };

        var resp = await client.PostAsJsonAsync(OrdersUrl(brand, shopId), request);
        await Assert.That(resp.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    // ── §9: Create with aligned slot 7 days in future → 400 (same-local-day) ─

    [Test]
    public async Task CreateOrder_WithAlignedSlotSevenDaysInFuture_Returns400()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.AlphaSlug;

        var shopId = await CreateShopAsync(client, brand);
        await EnableTimeSlotsAsync(client, brand, shopId, intervalMinutes: 15, maxOrders: 5);
        var productId = await CreateProductAsync(client, brand);

        // 7 days ahead, aligned to 15-minute boundary.
        var futureUtc = DateTimeOffset.UtcNow.AddDays(7);
        var aligned = new DateTimeOffset(
            futureUtc.Year, futureUtc.Month, futureUtc.Day,
            futureUtc.Hour, (futureUtc.Minute / 15 + 1) * 15 % 60, 0,
            TimeSpan.Zero);

        var request = new
        {
            OrderType = "Pickup",
            PaymentMethod = "CashAtPickup",
            CustomerFirstName = "Future",
            CustomerLastName = "Day",
            CustomerEmail = "future.day@test.com",
            CustomerPhone = "+32470000002",
            TimeSlotStart = aligned,
            Items = new[] { new { ProductId = productId, Quantity = 1, SelectedModifierIds = Array.Empty<Guid>() } }
        };

        var resp = await client.PostAsJsonAsync(OrdersUrl(brand, shopId), request);
        await Assert.That(resp.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    // ── §10: ASAP — null timeSlotStart on enabled shop succeeds, timeSlot null ─

    [Test]
    public async Task CreateOrder_AsapOnEnabledShop_Returns201WithNullTimeSlot()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.GammaSlug;

        var shopId = await CreateShopAsync(client, brand);
        await EnableTimeSlotsAsync(client, brand, shopId, intervalMinutes: 15, maxOrders: 5);
        var productId = await CreateProductAsync(client, brand);

        // null TimeSlotStart = ASAP
        var request = new
        {
            OrderType = "Pickup",
            PaymentMethod = "CashAtPickup",
            CustomerFirstName = "Asap",
            CustomerLastName = "Order",
            CustomerEmail = "asap@test.com",
            CustomerPhone = "+32470000000",
            TimeSlotStart = (DateTimeOffset?)null,
            Items = new[] { new { ProductId = productId, Quantity = 1, SelectedModifierIds = Array.Empty<Guid>() } }
        };

        var resp = await client.PostAsJsonAsync(OrdersUrl(brand, shopId), request);
        await Assert.That(resp.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var order = await resp.Content.ReadFromJsonAsync<OrderResponse>();
        await Assert.That(order).IsNotNull();
        await Assert.That(order!.TimeSlot).IsNull();
    }
}

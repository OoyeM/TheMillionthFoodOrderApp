using System.Net;
using System.Net.Http.Json;
using TheMillionthFoodOrderApp.Application.Orders.Dtos;
using TheMillionthFoodOrderApp.Application.Products;
using TheMillionthFoodOrderApp.Application.Shops;
using TheMillionthFoodOrderApp.Tests.Integration.Fixtures;

namespace TheMillionthFoodOrderApp.Tests.Integration.Orders;

/// <summary>
/// Integration tests for the order-tracking endpoints:
///   GET /api/brands/{brandSlug}/shops/{shopId}/orders/{orderId}
///   GET /api/brands/{brandSlug}/shops/{shopId}/orders/number/{orderNumber}
///
/// Each test places a real order via POST and then retrieves it, verifying
/// the OrderTrackingResponse shape and lifecycle data.
/// Runs against a real SQL Server via Testcontainers.
/// </summary>
[ClassDataSource<IntegrationTestBase>(Shared = SharedType.PerClass)]
public sealed class GetOrderTests(IntegrationTestBase fixture)
{
    private HttpClient CreateClient() => fixture.Factory.CreateClient();

    private static string OrdersUrl(string brandSlug, Guid shopId) =>
        $"/api/brands/{brandSlug}/shops/{shopId}/orders";

    private static string GetOrderByIdUrl(string brandSlug, Guid shopId, Guid orderId) =>
        $"/api/brands/{brandSlug}/shops/{shopId}/orders/{orderId}";

    private static string GetOrderByNumberUrl(string brandSlug, Guid shopId, string orderNumber) =>
        $"/api/brands/{brandSlug}/shops/{shopId}/orders/number/{orderNumber}";

    private static string TaxConfigUrl(string brandSlug) =>
        $"/api/brands/{brandSlug}/tax-configuration";

    private static string ShopsUrl(string brandSlug) =>
        $"/api/brands/{brandSlug}/shops";

    private static string ProductsUrl(string brandSlug) =>
        $"/api/brands/{brandSlug}/products";

    private static string OrderLifecycleUrl(string brandSlug, Guid shopId) =>
        $"/api/brands/{brandSlug}/shops/{shopId}/order-lifecycle";

    // ── Setup helpers ─────────────────────────────────────────────────────────

    private async Task SeedTaxConfigAsync(HttpClient client, string brandSlug)
    {
        var request = new
        {
            VatRates = new[]
            {
                new { ConsumptionMode = "Takeaway", RatePercentage = 6m },
                new { ConsumptionMode = "EatIn",    RatePercentage = 21m },
            }
        };
        var response = await client.PutAsJsonAsync(TaxConfigUrl(brandSlug), request);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    private async Task<Guid> CreateShopAsync(HttpClient client, string brandSlug)
    {
        var request = new
        {
            Name = $"Tracking Shop {Guid.NewGuid().ToString("N")[..8]}",
            Slug = $"track-{Guid.NewGuid().ToString("N")[..8]}",
            Address = new
            {
                Street = "Frietstraat",
                Number = "42",
                City = "Gent",
                PostalCode = "9000",
                Country = "BE"
            },
            ContactEmail = "track@frietjes.be",
            ContactPhone = (string?)null
        };

        var response = await client.PostAsJsonAsync(ShopsUrl(brandSlug), request);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var shop = await response.Content.ReadFromJsonAsync<ShopResponse>();
        await Assert.That(shop).IsNotNull();

        // Trigger default lifecycle creation (lazy-initialised on first GET)
        await client.GetAsync(OrderLifecycleUrl(brandSlug, shop!.Id));

        // US-FP-071: online orders require an open shop — give it an always-open schedule.
        var openingHours = new
        {
            TimeBlocks = Enumerable.Range(0, 7)
                .Select(day => new { DayOfWeek = day, OpenTime = "00:00", CloseTime = "23:59" })
                .ToArray()
        };
        var hoursResponse = await client.PutAsJsonAsync(
            $"/api/brands/{brandSlug}/shops/{shop.Id}/opening-hours", openingHours);
        await Assert.That(hoursResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        return shop.Id;
    }

    private async Task<Guid> CreateProductAsync(HttpClient client, string brandSlug, decimal price = 3.50m, string name = "Test Product")
    {
        var request = new
        {
            BasePrice = price,
            Translations = new[] { new { LanguageCode = "nl", Name = name, Description = (string?)null } }
        };

        var response = await client.PostAsJsonAsync(ProductsUrl(brandSlug), request);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var product = await response.Content.ReadFromJsonAsync<ProductResponse>();
        await Assert.That(product).IsNotNull();
        return product!.Id;
    }

    private async Task<OrderResponse> PlaceOrderAsync(HttpClient client, string brandSlug, Guid shopId, Guid productId)
    {
        var request = new
        {
            OrderType = "Pickup",
            PaymentMethod = "CashAtPickup",
            CustomerName = "Tracking Tester",
            Items = new[]
            {
                new { ProductId = productId, Quantity = 1, SelectedModifierIds = Array.Empty<Guid>() }
            }
        };

        var response = await client.PostAsJsonAsync(OrdersUrl(brandSlug, shopId), request);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
        await Assert.That(order).IsNotNull();
        return order!;
    }

    // ── Test 1: GET by ID — happy path ────────────────────────────────────────

    [Test]
    public async Task GetOrderById_ExistingOrder_Returns200WithOrderTrackingResponse()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.AlphaSlug;

        await SeedTaxConfigAsync(client, brand);
        var shopId = await CreateShopAsync(client, brand);
        var productId = await CreateProductAsync(client, brand, price: 4.00m, name: "Tracking Frietje");
        var placed = await PlaceOrderAsync(client, brand, shopId, productId);

        var response = await client.GetAsync(GetOrderByIdUrl(brand, shopId, placed.Id));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var tracking = await response.Content.ReadFromJsonAsync<OrderTrackingResponse>();
        await Assert.That(tracking).IsNotNull();

        // Order detail is correct
        await Assert.That(tracking!.Order.Id).IsEqualTo(placed.Id);
        await Assert.That(tracking.Order.OrderNumber).IsEqualTo(placed.OrderNumber);
        await Assert.That(tracking.Order.ShopId).IsEqualTo(shopId);
        await Assert.That(tracking.Order.BrandSlug).IsEqualTo(brand);
        await Assert.That(tracking.Order.StatusName).IsNotEmpty();
        await Assert.That(tracking.Order.Items.Count).IsEqualTo(1);

        // Lifecycle is populated
        await Assert.That(tracking.Lifecycle).IsNotNull();
        await Assert.That(tracking.Lifecycle.ShopId).IsEqualTo(shopId);
        await Assert.That(tracking.Lifecycle.Statuses.Count).IsGreaterThan(0);
    }

    // ── Test 2: GET by ID — not found ─────────────────────────────────────────

    [Test]
    public async Task GetOrderById_NonExistentId_Returns404()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.AlphaSlug;

        await SeedTaxConfigAsync(client, brand);
        var shopId = await CreateShopAsync(client, brand);

        var randomId = Guid.NewGuid();
        var response = await client.GetAsync(GetOrderByIdUrl(brand, shopId, randomId));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    // ── Test 3: GET by order number — happy path ──────────────────────────────

    [Test]
    public async Task GetOrderByNumber_ExistingOrder_Returns200WithOrderTrackingResponse()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.BetaSlug;

        await SeedTaxConfigAsync(client, brand);
        var shopId = await CreateShopAsync(client, brand);
        var productId = await CreateProductAsync(client, brand, price: 2.50m, name: "Tracking Saus");
        var placed = await PlaceOrderAsync(client, brand, shopId, productId);

        var response = await client.GetAsync(GetOrderByNumberUrl(brand, shopId, placed.OrderNumber));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var tracking = await response.Content.ReadFromJsonAsync<OrderTrackingResponse>();
        await Assert.That(tracking).IsNotNull();

        // Order detail matches the placed order
        await Assert.That(tracking!.Order.Id).IsEqualTo(placed.Id);
        await Assert.That(tracking.Order.OrderNumber).IsEqualTo(placed.OrderNumber);
        await Assert.That(tracking.Order.ShopId).IsEqualTo(shopId);
        await Assert.That(tracking.Order.CustomerName).IsEqualTo("Tracking Tester");
        await Assert.That(tracking.Order.Items.Count).IsEqualTo(1);

        // Lifecycle is populated
        await Assert.That(tracking.Lifecycle).IsNotNull();
        await Assert.That(tracking.Lifecycle.ShopId).IsEqualTo(shopId);
        await Assert.That(tracking.Lifecycle.Statuses.Count).IsGreaterThan(0);
    }

    // ── Test 4: GET by order number — not found ───────────────────────────────

    [Test]
    public async Task GetOrderByNumber_BogusOrderNumber_Returns404()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.BetaSlug;

        await SeedTaxConfigAsync(client, brand);
        var shopId = await CreateShopAsync(client, brand);

        var response = await client.GetAsync(GetOrderByNumberUrl(brand, shopId, "DOESNTEXIST"));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    // ── Bonus Test 5: GET by ID — shopId mismatch returns 404 ────────────────

    [Test]
    public async Task GetOrderById_WrongShopId_Returns404()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.AlphaSlug;

        await SeedTaxConfigAsync(client, brand);
        var shopId = await CreateShopAsync(client, brand);
        var wrongShopId = await CreateShopAsync(client, brand);
        var productId = await CreateProductAsync(client, brand, price: 1.80m, name: "Shop Mismatch Product");
        var placed = await PlaceOrderAsync(client, brand, shopId, productId);

        // Request order from the correct brand but a different shop
        var response = await client.GetAsync(GetOrderByIdUrl(brand, wrongShopId, placed.Id));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    // ── Bonus Test 6: Lifecycle statuses are ordered by SortOrder ────────────

    [Test]
    public async Task GetOrderById_LifecycleStatusesAreOrderedBySortOrder()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.GammaSlug;

        await SeedTaxConfigAsync(client, brand);
        var shopId = await CreateShopAsync(client, brand);
        var productId = await CreateProductAsync(client, brand, price: 3.00m, name: "Lifecycle Check");
        var placed = await PlaceOrderAsync(client, brand, shopId, productId);

        var response = await client.GetAsync(GetOrderByIdUrl(brand, shopId, placed.Id));
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var tracking = await response.Content.ReadFromJsonAsync<OrderTrackingResponse>();
        await Assert.That(tracking).IsNotNull();

        var statuses = tracking!.Lifecycle.Statuses;
        await Assert.That(statuses.Count).IsGreaterThan(0);

        // Verify statuses are sorted by SortOrder (ascending)
        var sortOrders = statuses.Select(s => s.SortOrder).ToList();
        var expected = sortOrders.OrderBy(x => x).ToList();
        await Assert.That(sortOrders).IsEquivalentTo(expected);

        // Order's current StatusName must be one of the lifecycle status names
        var statusNames = statuses.Select(s => s.Name).ToList();
        await Assert.That(statusNames).Contains(tracking.Order.StatusName);
    }
}

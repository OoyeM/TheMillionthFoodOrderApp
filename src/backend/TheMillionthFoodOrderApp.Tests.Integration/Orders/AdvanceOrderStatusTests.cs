using System.Net;
using System.Net.Http.Json;
using TheMillionthFoodOrderApp.Application.OrderLifecycle;
using TheMillionthFoodOrderApp.Application.Orders;
using TheMillionthFoodOrderApp.Application.Products;
using TheMillionthFoodOrderApp.Application.Shops;
using TheMillionthFoodOrderApp.Tests.Integration.Fixtures;

namespace TheMillionthFoodOrderApp.Tests.Integration.Orders;

/// <summary>
/// Integration tests for POST /api/brands/{brandSlug}/shops/{shopId}/orders/{orderId}/status —
/// the kitchen "advance order status" action (US-FP-023).
/// </summary>
[ClassDataSource<IntegrationTestBase>(Shared = SharedType.PerClass)]
public sealed class AdvanceOrderStatusTests(IntegrationTestBase fixture)
{
    private HttpClient CreateClient() => fixture.Factory.CreateClient();

    private static string ShopsUrl(string brandSlug) => $"/api/brands/{brandSlug}/shops";
    private static string ProductsUrl(string brandSlug) => $"/api/brands/{brandSlug}/products";
    private static string OrdersUrl(string brandSlug, Guid shopId) =>
        $"/api/brands/{brandSlug}/shops/{shopId}/orders";
    private static string LifecycleUrl(string brandSlug, Guid shopId) =>
        $"/api/brands/{brandSlug}/shops/{shopId}/order-lifecycle";
    private static string AdvanceUrl(string brandSlug, Guid shopId, Guid orderId) =>
        $"/api/brands/{brandSlug}/shops/{shopId}/orders/{orderId}/status";

    private async Task<Guid> CreateShopAsync(HttpClient client, string brandSlug)
    {
        var request = new
        {
            Name = $"Test Shop {Guid.NewGuid().ToString("N")[..8]}",
            Slug = $"shop-{Guid.NewGuid().ToString("N")[..8]}",
            Address = new
            {
                Street = "Frietstraat",
                Number = "1",
                City = "Brussel",
                PostalCode = "1000",
                Country = "BE"
            },
            ContactEmail = "shop@frietjes.be",
            ContactPhone = (string?)null
        };

        var response = await client.PostAsJsonAsync(ShopsUrl(brandSlug), request);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var shop = await response.Content.ReadFromJsonAsync<ShopResponse>();
        await Assert.That(shop).IsNotNull();

        // Trigger default lifecycle creation (lazy-initialised on first GET).
        await client.GetAsync(LifecycleUrl(brandSlug, shop!.Id));

        // Online orders require an open shop — give it an always-open schedule.
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

    private async Task<Guid> CreateProductAsync(HttpClient client, string brandSlug, string name)
    {
        var request = new
        {
            BasePrice = 3.00m,
            Translations = new[] { new { LanguageCode = "nl", Name = name, Description = (string?)null } }
        };

        var response = await client.PostAsJsonAsync(ProductsUrl(brandSlug), request);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var product = await response.Content.ReadFromJsonAsync<ProductResponse>();
        await Assert.That(product).IsNotNull();
        return product!.Id;
    }

    private async Task<OrderResponse> PlaceOrderAsync(
        HttpClient client, string brandSlug, Guid shopId, Guid productId)
    {
        var request = new
        {
            OrderType = "Pickup",
            PaymentMethod = "CashAtPickup",
            CustomerFirstName = "Status",
            CustomerLastName = "Test",
            CustomerEmail = "status@example.com",
            CustomerPhone = "+32470000030",
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

    private async Task<OrderLifecycleResponse> GetLifecycleAsync(
        HttpClient client, string brandSlug, Guid shopId)
    {
        var response = await client.GetAsync(LifecycleUrl(brandSlug, shopId));
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var lifecycle = await response.Content.ReadFromJsonAsync<OrderLifecycleResponse>();
        await Assert.That(lifecycle).IsNotNull();
        return lifecycle!;
    }

    private static Guid StatusId(OrderLifecycleResponse lifecycle, string name) =>
        lifecycle.Statuses.Single(s => s.Name == name).Id;

    // ── Tests ───────────────────────────────────────────────────────────────

    [Test]
    public async Task Advance_AllowedTransition_Returns200AndUpdatesStatus()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.AlphaSlug;
        var shopId = await CreateShopAsync(client, brand);
        var productId = await CreateProductAsync(client, brand, "Frietje");
        var order = await PlaceOrderAsync(client, brand, shopId, productId);
        await Assert.That(order.StatusName).IsEqualTo("Placed");

        var lifecycle = await GetLifecycleAsync(client, brand, shopId);
        var confirmedId = StatusId(lifecycle, "Confirmed");

        var response = await client.PostAsJsonAsync(
            AdvanceUrl(brand, shopId, order.Id), new { ToStatusId = confirmedId });

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<OrderResponse>();
        await Assert.That(updated).IsNotNull();
        await Assert.That(updated!.StatusName).IsEqualTo("Confirmed");
    }

    [Test]
    public async Task Advance_DisallowedTransition_Returns400()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.BetaSlug;
        var shopId = await CreateShopAsync(client, brand);
        var productId = await CreateProductAsync(client, brand, "Frikandel");
        var order = await PlaceOrderAsync(client, brand, shopId, productId);

        var lifecycle = await GetLifecycleAsync(client, brand, shopId);
        // No direct transition exists from "Placed" to "Ready" in the default lifecycle.
        var readyId = StatusId(lifecycle, "Ready");

        var response = await client.PostAsJsonAsync(
            AdvanceUrl(brand, shopId, order.Id), new { ToStatusId = readyId });

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Advance_UnknownStatusId_Returns404()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.GammaSlug;
        var shopId = await CreateShopAsync(client, brand);
        var productId = await CreateProductAsync(client, brand, "Bicky");
        var order = await PlaceOrderAsync(client, brand, shopId, productId);

        var response = await client.PostAsJsonAsync(
            AdvanceUrl(brand, shopId, order.Id), new { ToStatusId = Guid.NewGuid() });

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Advance_NonExistentOrder_Returns404()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.DeltaSlug;
        var shopId = await CreateShopAsync(client, brand);
        var lifecycle = await GetLifecycleAsync(client, brand, shopId);
        var confirmedId = StatusId(lifecycle, "Confirmed");

        var response = await client.PostAsJsonAsync(
            AdvanceUrl(brand, shopId, Guid.NewGuid()), new { ToStatusId = confirmedId });

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }
}

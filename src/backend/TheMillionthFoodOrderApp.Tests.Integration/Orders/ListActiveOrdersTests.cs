using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using TheMillionthFoodOrderApp.Api.Endpoints.Orders;
using TheMillionthFoodOrderApp.Application.Orders;
using TheMillionthFoodOrderApp.Application.Products;
using TheMillionthFoodOrderApp.Application.Shops;
using TheMillionthFoodOrderApp.Infrastructure.Persistence;
using TheMillionthFoodOrderApp.Tests.Integration.Fixtures;

namespace TheMillionthFoodOrderApp.Tests.Integration.Orders;

/// <summary>
/// Integration tests for GET /api/brands/{brandSlug}/shops/{shopId}/orders/active —
/// the kitchen display endpoint (US-FP-027).
/// </summary>
[ClassDataSource<IntegrationTestBase>(Shared = SharedType.PerClass)]
public sealed class ListActiveOrdersTests(IntegrationTestBase fixture)
{
    private HttpClient CreateClient() => fixture.Factory.CreateClient();

    private static string ActiveOrdersUrl(string brandSlug, Guid shopId) =>
        $"/api/brands/{brandSlug}/shops/{shopId}/orders/active";

    private static string OrdersUrl(string brandSlug, Guid shopId) =>
        $"/api/brands/{brandSlug}/shops/{shopId}/orders";

    private static string ShopsUrl(string brandSlug) =>
        $"/api/brands/{brandSlug}/shops";

    private static string ProductsUrl(string brandSlug) =>
        $"/api/brands/{brandSlug}/products";

    private static string OrderLifecycleUrl(string brandSlug, Guid shopId) =>
        $"/api/brands/{brandSlug}/shops/{shopId}/order-lifecycle";

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
        HttpClient client,
        string brandSlug,
        Guid shopId,
        Guid productId,
        string customerName)
    {
        // Split the provided customerName into first/last for the new API contract (US-FP-051).
        var parts = customerName.Trim().Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
        var firstName = parts.Length > 0 ? parts[0] : customerName;
        var lastName = parts.Length > 1 ? parts[1] : customerName;

        var request = new
        {
            OrderType = "Pickup",
            PaymentMethod = "CashAtPickup",
            CustomerFirstName = firstName,
            CustomerLastName = lastName,
            CustomerEmail = $"{customerName.ToLowerInvariant().Replace(' ', '.')}@example.com",
            CustomerPhone = "+32470000040",
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

    /// <summary>
    /// No equivalent API endpoint exists to update an order's status until US-FP-023.
    /// For the test we mutate the order's StatusName directly via a fresh BrandDbContext.
    /// </summary>
    private async Task MarkOrderStatusAsync(string brandSlug, Guid orderId, string statusName)
    {
        var options = new DbContextOptionsBuilder<BrandDbContext>()
            .UseSqlServer(fixture.GetBrandConnectionString(brandSlug))
            .Options;

        await using var ctx = new BrandDbContext(options);
        await ctx.Orders
            .Where(o => o.Id == orderId)
            .ExecuteUpdateAsync(s => s.SetProperty(o => o.StatusName, statusName));
    }

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Test]
    public async Task ListActive_NoOrders_ReturnsEmptyList()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.AlphaSlug;
        var shopId = await CreateShopAsync(client, brand);

        var response = await client.GetAsync(ActiveOrdersUrl(brand, shopId));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<ListActiveOrdersResponse>();
        await Assert.That(body).IsNotNull();
        await Assert.That(body!.Orders.Count).IsEqualTo(0);
    }

    [Test]
    public async Task ListActive_MultipleOrders_ReturnsSortedByCreatedAtAscending()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.BetaSlug;
        var shopId = await CreateShopAsync(client, brand);
        var productId = await CreateProductAsync(client, brand, "Frietje");

        var first = await PlaceOrderAsync(client, brand, shopId, productId, "Alice");
        // Small delay so CreatedAt values are distinguishable at the second precision
        await Task.Delay(10);
        var second = await PlaceOrderAsync(client, brand, shopId, productId, "Bob");
        await Task.Delay(10);
        var third = await PlaceOrderAsync(client, brand, shopId, productId, "Charlie");

        var response = await client.GetAsync(ActiveOrdersUrl(brand, shopId));
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ListActiveOrdersResponse>();
        await Assert.That(body).IsNotNull();
        await Assert.That(body!.Orders.Count).IsEqualTo(3);
        await Assert.That(body.Orders[0].Id).IsEqualTo(first.Id);
        await Assert.That(body.Orders[1].Id).IsEqualTo(second.Id);
        await Assert.That(body.Orders[2].Id).IsEqualTo(third.Id);
    }

    [Test]
    public async Task ListActive_ExcludesOrdersInTerminalStatuses()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.GammaSlug;
        var shopId = await CreateShopAsync(client, brand);
        var productId = await CreateProductAsync(client, brand, "Frikandel");

        var active = await PlaceOrderAsync(client, brand, shopId, productId, "Active");
        var completed = await PlaceOrderAsync(client, brand, shopId, productId, "Completed");
        var delivered = await PlaceOrderAsync(client, brand, shopId, productId, "Delivered");

        // Default lifecycle terminal statuses: "Picked Up" and "Delivered"
        await MarkOrderStatusAsync(brand, completed.Id, "Picked Up");
        await MarkOrderStatusAsync(brand, delivered.Id, "Delivered");

        var response = await client.GetAsync(ActiveOrdersUrl(brand, shopId));
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ListActiveOrdersResponse>();
        await Assert.That(body).IsNotNull();
        await Assert.That(body!.Orders.Count).IsEqualTo(1);
        await Assert.That(body.Orders[0].Id).IsEqualTo(active.Id);
    }

    [Test]
    public async Task ListActive_ExcludesOrdersFromOtherShops()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.DeltaSlug;
        var shopA = await CreateShopAsync(client, brand);
        var shopB = await CreateShopAsync(client, brand);
        var productId = await CreateProductAsync(client, brand, "Bicky");

        var inShopA = await PlaceOrderAsync(client, brand, shopA, productId, "ShopA Customer");
        await PlaceOrderAsync(client, brand, shopB, productId, "ShopB Customer");

        var response = await client.GetAsync(ActiveOrdersUrl(brand, shopA));
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<ListActiveOrdersResponse>();
        await Assert.That(body).IsNotNull();
        await Assert.That(body!.Orders.Count).IsEqualTo(1);
        await Assert.That(body.Orders[0].Id).IsEqualTo(inShopA.Id);
        await Assert.That(body.Orders[0].ShopId).IsEqualTo(shopA);
    }

    [Test]
    public async Task ListActive_IncludesItemDetails()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.AlphaSlug;
        var shopId = await CreateShopAsync(client, brand);
        var productId = await CreateProductAsync(client, brand, "Patatje Oorlog");

        await PlaceOrderAsync(client, brand, shopId, productId, "Detail Test");

        var response = await client.GetAsync(ActiveOrdersUrl(brand, shopId));
        var body = await response.Content.ReadFromJsonAsync<ListActiveOrdersResponse>();

        await Assert.That(body).IsNotNull();
        var order = body!.Orders.Single(o => o.CustomerName == "Detail Test");
        await Assert.That(order.Items.Count).IsEqualTo(1);
        await Assert.That(order.Items[0].ProductName).IsEqualTo("Patatje Oorlog");
        await Assert.That(order.Items[0].Quantity).IsEqualTo(1);
    }

    [Test]
    public async Task ListActive_NonExistentBrand_Returns404()
    {
        var client = CreateClient();
        var response = await client.GetAsync(
            $"/api/brands/non-existent-brand/shops/{Guid.NewGuid()}/orders/active");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }
}

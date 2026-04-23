using System.Net;
using System.Net.Http.Json;
using TheMillionthFoodOrderApp.Application.OrderLifecycle;
using TheMillionthFoodOrderApp.Application.Shops;
using TheMillionthFoodOrderApp.Tests.Integration.Fixtures;

namespace TheMillionthFoodOrderApp.Tests.Integration.OrderLifecycle;

/// <summary>
/// Integration tests for order lifecycle CRUD operations.
/// Runs against a real SQL Server via Testcontainers.
/// </summary>
[ClassDataSource<IntegrationTestBase>(Shared = SharedType.PerClass)]
public sealed class OrderLifecycleCrudTests(IntegrationTestBase fixture)
{
    private HttpClient CreateClient() => fixture.Factory.CreateClient();

    private static string LifecycleUrl(string brandSlug, Guid shopId) =>
        $"/api/brands/{brandSlug}/shops/{shopId}/order-lifecycle";

    private static string ResetUrl(string brandSlug, Guid shopId) =>
        $"/api/brands/{brandSlug}/shops/{shopId}/order-lifecycle/reset";

    private static string ShopsUrl(string brandSlug) =>
        $"/api/brands/{brandSlug}/shops";

    private async Task<Guid> CreateShopAsync(HttpClient client, string brandSlug)
    {
        var uniqueSlug = $"shop-{Guid.NewGuid():N}";
        var request = new
        {
            Name = "Test Shop",
            Slug = uniqueSlug,
            Address = new
            {
                Street = "Teststraat",
                Number = "1",
                City = "Brussel",
                PostalCode = "1000",
                Country = "BE"
            },
            ContactEmail = "shop@test.com",
            ContactPhone = (string?)null
        };

        var response = await client.PostAsJsonAsync(ShopsUrl(brandSlug), request);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var shop = await response.Content.ReadFromJsonAsync<ShopResponse>();
        await Assert.That(shop).IsNotNull();
        return shop!.Id;
    }

    // ── GET — lazy init ───────────────────────────────────────────────────────

    [Test]
    public async Task GetOrderLifecycle_NewShop_ReturnsDefault()
    {
        var client = CreateClient();
        var shopId = await CreateShopAsync(client, IntegrationTestBase.AlphaSlug);

        var response = await client.GetAsync(LifecycleUrl(IntegrationTestBase.AlphaSlug, shopId));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<OrderLifecycleResponse>();
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.ShopId).IsEqualTo(shopId);
        await Assert.That(result.Statuses.Count).IsEqualTo(6);
        await Assert.That(result.Transitions.Count).IsEqualTo(5);

        // Default statuses in order
        await Assert.That(result.Statuses[0].Name).IsEqualTo("Placed");
        await Assert.That(result.Statuses[0].SystemKey).IsEqualTo("placed");
        await Assert.That(result.Statuses[0].IsTerminal).IsFalse();

        await Assert.That(result.Statuses[4].Name).IsEqualTo("Picked Up");
        await Assert.That(result.Statuses[4].SystemKey).IsEqualTo("picked_up");
        await Assert.That(result.Statuses[4].IsTerminal).IsTrue();

        await Assert.That(result.Statuses[5].Name).IsEqualTo("Delivered");
        await Assert.That(result.Statuses[5].SystemKey).IsEqualTo("delivered");
        await Assert.That(result.Statuses[5].IsTerminal).IsTrue();
    }

    // ── PUT — valid config ────────────────────────────────────────────────────

    [Test]
    public async Task ConfigureLifecycle_ValidMinimalConfig_Returns200()
    {
        var client = CreateClient();
        var shopId = await CreateShopAsync(client, IntegrationTestBase.AlphaSlug);

        var request = new
        {
            Statuses = new[]
            {
                new { Name = "Placed", SystemKey = "placed", SortOrder = 0, IsTerminal = false, ColorHex = (string?)null },
                new { Name = "Done", SystemKey = (string?)null, SortOrder = 1, IsTerminal = true, ColorHex = "#16a34a" },
            },
            Transitions = new[]
            {
                new { FromSortOrder = 0, ToSortOrder = 1 },
            }
        };

        var response = await client.PutAsJsonAsync(LifecycleUrl(IntegrationTestBase.AlphaSlug, shopId), request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<OrderLifecycleResponse>();
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Statuses.Count).IsEqualTo(2);
        await Assert.That(result.Transitions.Count).IsEqualTo(1);
        await Assert.That(result.Statuses[0].Name).IsEqualTo("Placed");
        await Assert.That(result.Statuses[1].Name).IsEqualTo("Done");
        await Assert.That(result.Statuses[1].IsTerminal).IsTrue();
        await Assert.That(result.Statuses[1].ColorHex).IsEqualTo("#16a34a");
    }

    [Test]
    public async Task ConfigureLifecycle_ReplaceExisting_Returns200()
    {
        var client = CreateClient();
        var shopId = await CreateShopAsync(client, IntegrationTestBase.AlphaSlug);

        // First: get default
        await client.GetAsync(LifecycleUrl(IntegrationTestBase.AlphaSlug, shopId));

        // Replace with 3 statuses
        var request = new
        {
            Statuses = new[]
            {
                new { Name = "New", SystemKey = (string?)null, SortOrder = 0, IsTerminal = false, ColorHex = (string?)null },
                new { Name = "In Progress", SystemKey = (string?)null, SortOrder = 1, IsTerminal = false, ColorHex = (string?)null },
                new { Name = "Complete", SystemKey = (string?)null, SortOrder = 2, IsTerminal = true, ColorHex = (string?)null },
            },
            Transitions = new[]
            {
                new { FromSortOrder = 0, ToSortOrder = 1 },
                new { FromSortOrder = 1, ToSortOrder = 2 },
            }
        };

        var response = await client.PutAsJsonAsync(LifecycleUrl(IntegrationTestBase.AlphaSlug, shopId), request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<OrderLifecycleResponse>();
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Statuses.Count).IsEqualTo(3);
        await Assert.That(result.Transitions.Count).IsEqualTo(2);
    }

    // ── PUT — validation errors ──────────────────────────────────────────────

    [Test]
    public async Task ConfigureLifecycle_LessThan2Statuses_Returns400()
    {
        var client = CreateClient();
        var shopId = await CreateShopAsync(client, IntegrationTestBase.AlphaSlug);

        var request = new
        {
            Statuses = new[]
            {
                new { Name = "Only One", SystemKey = (string?)null, SortOrder = 0, IsTerminal = true, ColorHex = (string?)null },
            },
            Transitions = Array.Empty<object>()
        };

        var response = await client.PutAsJsonAsync(LifecycleUrl(IntegrationTestBase.AlphaSlug, shopId), request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task ConfigureLifecycle_NoTerminalStatus_Returns400()
    {
        var client = CreateClient();
        var shopId = await CreateShopAsync(client, IntegrationTestBase.AlphaSlug);

        var request = new
        {
            Statuses = new[]
            {
                new { Name = "Placed", SystemKey = (string?)null, SortOrder = 0, IsTerminal = false, ColorHex = (string?)null },
                new { Name = "Processing", SystemKey = (string?)null, SortOrder = 1, IsTerminal = false, ColorHex = (string?)null },
            },
            Transitions = new[]
            {
                new { FromSortOrder = 0, ToSortOrder = 1 },
            }
        };

        var response = await client.PutAsJsonAsync(LifecycleUrl(IntegrationTestBase.AlphaSlug, shopId), request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task ConfigureLifecycle_DuplicateSortOrders_Returns400()
    {
        var client = CreateClient();
        var shopId = await CreateShopAsync(client, IntegrationTestBase.AlphaSlug);

        var request = new
        {
            Statuses = new[]
            {
                new { Name = "Placed", SystemKey = (string?)null, SortOrder = 0, IsTerminal = false, ColorHex = (string?)null },
                new { Name = "Done", SystemKey = (string?)null, SortOrder = 0, IsTerminal = true, ColorHex = (string?)null }, // Duplicate sort order!
            },
            Transitions = Array.Empty<object>()
        };

        var response = await client.PutAsJsonAsync(LifecycleUrl(IntegrationTestBase.AlphaSlug, shopId), request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task ConfigureLifecycle_InvalidTransitionReference_Returns400()
    {
        var client = CreateClient();
        var shopId = await CreateShopAsync(client, IntegrationTestBase.AlphaSlug);

        var request = new
        {
            Statuses = new[]
            {
                new { Name = "Placed", SystemKey = (string?)null, SortOrder = 0, IsTerminal = false, ColorHex = (string?)null },
                new { Name = "Done", SystemKey = (string?)null, SortOrder = 1, IsTerminal = true, ColorHex = (string?)null },
            },
            Transitions = new[]
            {
                new { FromSortOrder = 0, ToSortOrder = 99 }, // 99 doesn't exist
            }
        };

        var response = await client.PutAsJsonAsync(LifecycleUrl(IntegrationTestBase.AlphaSlug, shopId), request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    // ── POST /reset ──────────────────────────────────────────────────────────

    [Test]
    public async Task ResetToDefault_Returns200WithDefaultStatuses()
    {
        var client = CreateClient();
        var shopId = await CreateShopAsync(client, IntegrationTestBase.AlphaSlug);

        // Configure a minimal lifecycle first
        var configRequest = new
        {
            Statuses = new[]
            {
                new { Name = "Open", SystemKey = (string?)null, SortOrder = 0, IsTerminal = false, ColorHex = (string?)null },
                new { Name = "Closed", SystemKey = (string?)null, SortOrder = 1, IsTerminal = true, ColorHex = (string?)null },
            },
            Transitions = new[]
            {
                new { FromSortOrder = 0, ToSortOrder = 1 },
            }
        };
        await client.PutAsJsonAsync(LifecycleUrl(IntegrationTestBase.AlphaSlug, shopId), configRequest);

        // Reset to default
        var response = await client.PostAsync(ResetUrl(IntegrationTestBase.AlphaSlug, shopId), null);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<OrderLifecycleResponse>();
        await Assert.That(result).IsNotNull();
        await Assert.That(result!.Statuses.Count).IsEqualTo(6); // Back to default 6 statuses
        await Assert.That(result.Transitions.Count).IsEqualTo(5);
        await Assert.That(result.Statuses[0].Name).IsEqualTo("Placed");
    }

    // ── 404 cases ─────────────────────────────────────────────────────────────

    [Test]
    public async Task GetOrderLifecycle_NonExistentShop_Returns404()
    {
        var client = CreateClient();
        var nonExistentShopId = Guid.NewGuid();

        var response = await client.GetAsync(LifecycleUrl(IntegrationTestBase.AlphaSlug, nonExistentShopId));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GetOrderLifecycle_NonExistentBrand_Returns404()
    {
        var client = CreateClient();
        var shopId = Guid.NewGuid();

        var response = await client.GetAsync(LifecycleUrl("non-existent-brand", shopId));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }
}

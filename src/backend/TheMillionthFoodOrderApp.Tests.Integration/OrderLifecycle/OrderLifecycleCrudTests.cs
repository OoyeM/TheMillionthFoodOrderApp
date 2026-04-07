using System.Net;
using System.Net.Http.Json;
using Shouldly;
using TheMillionthFoodOrderApp.Application.OrderLifecycle;
using TheMillionthFoodOrderApp.Application.Shops;
using TheMillionthFoodOrderApp.Tests.Integration.Fixtures;

namespace TheMillionthFoodOrderApp.Tests.Integration.OrderLifecycle;

/// <summary>
/// Integration tests for order lifecycle CRUD operations.
/// Runs against a real SQL Server via Testcontainers.
/// </summary>
public sealed class OrderLifecycleCrudTests(IntegrationTestBase fixture)
    : IClassFixture<IntegrationTestBase>
{
    private HttpClient CreateClient() => fixture.Factory.CreateClient();

    private static string LifecycleUrl(string brandSlug, Guid shopId) =>
        $"/api/brands/{brandSlug}/shops/{shopId}/order-lifecycle";

    private static string ResetUrl(string brandSlug, Guid shopId) =>
        $"/api/brands/{brandSlug}/shops/{shopId}/order-lifecycle/reset";

    private static string ShopsUrl(string brandSlug) =>
        $"/api/brands/{brandSlug}/shops";

    private static async Task<Guid> CreateShopAsync(HttpClient client, string brandSlug)
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
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var shop = await response.Content.ReadFromJsonAsync<ShopResponse>();
        shop.ShouldNotBeNull();
        return shop.Id;
    }

    // ── GET — lazy init ───────────────────────────────────────────────────────

    [Fact]
    public async Task GetOrderLifecycle_NewShop_ReturnsDefault()
    {
        var client = CreateClient();
        var shopId = await CreateShopAsync(client, IntegrationTestBase.AlphaSlug);

        var response = await client.GetAsync(LifecycleUrl(IntegrationTestBase.AlphaSlug, shopId));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<OrderLifecycleResponse>();
        result.ShouldNotBeNull();
        result.ShopId.ShouldBe(shopId);
        result.Statuses.Count.ShouldBe(6);
        result.Transitions.Count.ShouldBe(5);

        // Default statuses in order
        result.Statuses[0].Name.ShouldBe("Placed");
        result.Statuses[0].SystemKey.ShouldBe("placed");
        result.Statuses[0].IsTerminal.ShouldBeFalse();

        result.Statuses[4].Name.ShouldBe("Picked Up");
        result.Statuses[4].SystemKey.ShouldBe("picked_up");
        result.Statuses[4].IsTerminal.ShouldBeTrue();

        result.Statuses[5].Name.ShouldBe("Delivered");
        result.Statuses[5].SystemKey.ShouldBe("delivered");
        result.Statuses[5].IsTerminal.ShouldBeTrue();
    }

    // ── PUT — valid config ────────────────────────────────────────────────────

    [Fact]
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

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<OrderLifecycleResponse>();
        result.ShouldNotBeNull();
        result.Statuses.Count.ShouldBe(2);
        result.Transitions.Count.ShouldBe(1);
        result.Statuses[0].Name.ShouldBe("Placed");
        result.Statuses[1].Name.ShouldBe("Done");
        result.Statuses[1].IsTerminal.ShouldBeTrue();
        result.Statuses[1].ColorHex.ShouldBe("#16a34a");
    }

    [Fact]
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

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<OrderLifecycleResponse>();
        result.ShouldNotBeNull();
        result.Statuses.Count.ShouldBe(3);
        result.Transitions.Count.ShouldBe(2);
    }

    // ── PUT — validation errors ──────────────────────────────────────────────

    [Fact]
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

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
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

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
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

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
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

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // ── POST /reset ──────────────────────────────────────────────────────────

    [Fact]
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

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<OrderLifecycleResponse>();
        result.ShouldNotBeNull();
        result.Statuses.Count.ShouldBe(6); // Back to default 6 statuses
        result.Transitions.Count.ShouldBe(5);
        result.Statuses[0].Name.ShouldBe("Placed");
    }

    // ── 404 cases ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetOrderLifecycle_NonExistentShop_Returns404()
    {
        var client = CreateClient();
        var nonExistentShopId = Guid.NewGuid();

        var response = await client.GetAsync(LifecycleUrl(IntegrationTestBase.AlphaSlug, nonExistentShopId));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetOrderLifecycle_NonExistentBrand_Returns404()
    {
        var client = CreateClient();
        var shopId = Guid.NewGuid();

        var response = await client.GetAsync(LifecycleUrl("non-existent-brand", shopId));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}

using System.Net;
using System.Net.Http.Json;
using Shouldly;
using TheMillionthFoodOrderApp.Application.Shops;
using TheMillionthFoodOrderApp.Tests.Integration.Fixtures;

namespace TheMillionthFoodOrderApp.Tests.Integration.Shops;

/// <summary>
/// Integration tests for the shop open/closed status endpoint.
/// Runs against a real SQL Server via Testcontainers.
/// </summary>
public sealed class ShopStatusTests(IntegrationTestBase fixture)
    : IClassFixture<IntegrationTestBase>
{
    private HttpClient CreateClient() => fixture.Factory.CreateClient();

    private static string ShopsUrl(string brandSlug) =>
        $"/api/brands/{brandSlug}/shops";

    private static string OpeningHoursUrl(string brandSlug, Guid shopId) =>
        $"/api/brands/{brandSlug}/shops/{shopId}/opening-hours";

    private static string StatusUrl(string brandSlug, Guid shopId) =>
        $"/api/brands/{brandSlug}/shops/{shopId}/status";

    /// <summary>
    /// Creates a test shop via the API and returns its id.
    /// Each call uses a unique slug to prevent conflicts across tests sharing the same brand database.
    /// </summary>
    private static async Task<Guid> CreateShopAsync(HttpClient client, string brandSlug)
    {
        var uniqueSlug = $"status-shop-{Guid.NewGuid():N}";
        var request = new
        {
            Name = "Status Test Shop",
            Slug = uniqueSlug,
            Address = new
            {
                Street = "Statusstraat",
                Number = "1",
                City = "Gent",
                PostalCode = "9000",
                Country = "BE"
            },
            ContactEmail = "status@test.com",
            ContactPhone = (string?)null
        };

        var response = await client.PostAsJsonAsync(ShopsUrl(brandSlug), request);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var shop = await response.Content.ReadFromJsonAsync<ShopResponse>();
        shop.ShouldNotBeNull();
        return shop.Id;
    }

    // ── Status tests ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetShopStatus_NoHours_ReturnsClosed_WithNullNextOpeningTime()
    {
        // Arrange
        var client = CreateClient();
        var shopId = await CreateShopAsync(client, IntegrationTestBase.BetaSlug);
        // No opening hours set — shop has empty schedule

        // Act
        var response = await client.GetAsync(StatusUrl(IntegrationTestBase.BetaSlug, shopId));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var status = await response.Content.ReadFromJsonAsync<ShopStatusResponse>();
        status.ShouldNotBeNull();
        status.IsOpen.ShouldBeFalse();
        status.NextOpeningTime.ShouldBeNull();
    }

    [Fact]
    public async Task GetShopStatus_AllDayEveryDay_ReturnsOpen()
    {
        // Arrange
        var client = CreateClient();
        var shopId = await CreateShopAsync(client, IntegrationTestBase.BetaSlug);

        // Set 00:00-23:59 for all 7 days to guarantee the shop is always open
        var setHoursRequest = new
        {
            TimeBlocks = new[]
            {
                new { DayOfWeek = 0, OpenTime = "00:00", CloseTime = "23:59" }, // Sunday
                new { DayOfWeek = 1, OpenTime = "00:00", CloseTime = "23:59" }, // Monday
                new { DayOfWeek = 2, OpenTime = "00:00", CloseTime = "23:59" }, // Tuesday
                new { DayOfWeek = 3, OpenTime = "00:00", CloseTime = "23:59" }, // Wednesday
                new { DayOfWeek = 4, OpenTime = "00:00", CloseTime = "23:59" }, // Thursday
                new { DayOfWeek = 5, OpenTime = "00:00", CloseTime = "23:59" }, // Friday
                new { DayOfWeek = 6, OpenTime = "00:00", CloseTime = "23:59" }, // Saturday
            }
        };
        var setResponse = await client.PutAsJsonAsync(
            OpeningHoursUrl(IntegrationTestBase.BetaSlug, shopId), setHoursRequest);
        setResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Act
        var response = await client.GetAsync(StatusUrl(IntegrationTestBase.BetaSlug, shopId));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var status = await response.Content.ReadFromJsonAsync<ShopStatusResponse>();
        status.ShouldNotBeNull();
        status.IsOpen.ShouldBeTrue();
    }

    [Fact]
    public async Task GetShopStatus_Returns404_ForNonExistentShop()
    {
        // Arrange
        var client = CreateClient();
        var nonExistentShopId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync(StatusUrl(IntegrationTestBase.BetaSlug, nonExistentShopId));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetShopStatus_ResponseIncludesTimeZoneId()
    {
        // Arrange
        var client = CreateClient();
        var shopId = await CreateShopAsync(client, IntegrationTestBase.BetaSlug);

        // Act
        var response = await client.GetAsync(StatusUrl(IntegrationTestBase.BetaSlug, shopId));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var status = await response.Content.ReadFromJsonAsync<ShopStatusResponse>();
        status.ShouldNotBeNull();
        status.TimeZoneId.ShouldNotBeNullOrEmpty();
    }
}

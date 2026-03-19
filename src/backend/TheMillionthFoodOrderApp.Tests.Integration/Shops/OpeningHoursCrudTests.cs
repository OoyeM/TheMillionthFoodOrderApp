using System.Net;
using System.Net.Http.Json;
using Shouldly;
using TheMillionthFoodOrderApp.Application.Shops;
using TheMillionthFoodOrderApp.Tests.Integration.Fixtures;

namespace TheMillionthFoodOrderApp.Tests.Integration.Shops;

/// <summary>
/// Integration tests for shop opening hours CRUD operations.
/// Runs against a real SQL Server via Testcontainers.
/// </summary>
public sealed class OpeningHoursCrudTests(IntegrationTestBase fixture)
    : IClassFixture<IntegrationTestBase>
{
    private HttpClient CreateClient() => fixture.Factory.CreateClient();

    private static string OpeningHoursUrl(string brandSlug, Guid shopId) =>
        $"/api/brands/{brandSlug}/shops/{shopId}/opening-hours";

    private static string ShopsUrl(string brandSlug) =>
        $"/api/brands/{brandSlug}/shops";

    /// <summary>
    /// Creates a test shop via the API and returns its id.
    /// Each call uses a unique slug to prevent conflicts across tests sharing the same brand database.
    /// </summary>
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

    // ── Set opening hours ──────────────────────────────────────────────────────

    [Fact]
    public async Task SetOpeningHours_SingleBlockPerDay_Returns200()
    {
        // Arrange
        var client = CreateClient();
        var shopId = await CreateShopAsync(client, IntegrationTestBase.AlphaSlug);

        var request = new
        {
            TimeBlocks = new[]
            {
                new { DayOfWeek = 1, OpenTime = "09:00", CloseTime = "17:00" }, // Monday
                new { DayOfWeek = 2, OpenTime = "09:00", CloseTime = "17:00" }, // Tuesday
                new { DayOfWeek = 5, OpenTime = "10:00", CloseTime = "22:00" }, // Friday
            }
        };

        // Act
        var response = await client.PutAsJsonAsync(
            OpeningHoursUrl(IntegrationTestBase.AlphaSlug, shopId), request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<OpeningHoursResponse>();
        result.ShouldNotBeNull();
        result.TimeBlocks.Count.ShouldBe(3);
        result.TimeBlocks.ShouldContain(b => b.DayOfWeek == DayOfWeek.Monday && b.OpenTime == "09:00" && b.CloseTime == "17:00");
        result.TimeBlocks.ShouldContain(b => b.DayOfWeek == DayOfWeek.Tuesday && b.OpenTime == "09:00" && b.CloseTime == "17:00");
        result.TimeBlocks.ShouldContain(b => b.DayOfWeek == DayOfWeek.Friday && b.OpenTime == "10:00" && b.CloseTime == "22:00");
    }

    [Fact]
    public async Task SetOpeningHours_MultipleBlocksPerDay_Returns200()
    {
        // Arrange
        var client = CreateClient();
        var shopId = await CreateShopAsync(client, IntegrationTestBase.AlphaSlug);

        // Lunch block + dinner block on the same day
        var request = new
        {
            TimeBlocks = new[]
            {
                new { DayOfWeek = 1, OpenTime = "11:30", CloseTime = "14:00" }, // Monday lunch
                new { DayOfWeek = 1, OpenTime = "17:00", CloseTime = "21:30" }, // Monday dinner
            }
        };

        // Act
        var response = await client.PutAsJsonAsync(
            OpeningHoursUrl(IntegrationTestBase.AlphaSlug, shopId), request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<OpeningHoursResponse>();
        result.ShouldNotBeNull();
        result.TimeBlocks.Count.ShouldBe(2);
        result.TimeBlocks.ShouldContain(b => b.DayOfWeek == DayOfWeek.Monday && b.OpenTime == "11:30" && b.CloseTime == "14:00");
        result.TimeBlocks.ShouldContain(b => b.DayOfWeek == DayOfWeek.Monday && b.OpenTime == "17:00" && b.CloseTime == "21:30");
    }

    [Fact]
    public async Task SetOpeningHours_ReplaceExistingSchedule_Returns200WithNewSchedule()
    {
        // Arrange
        var client = CreateClient();
        var shopId = await CreateShopAsync(client, IntegrationTestBase.AlphaSlug);

        // Set initial schedule
        var initial = new
        {
            TimeBlocks = new[]
            {
                new { DayOfWeek = 1, OpenTime = "09:00", CloseTime = "17:00" },
                new { DayOfWeek = 2, OpenTime = "09:00", CloseTime = "17:00" },
            }
        };
        await client.PutAsJsonAsync(OpeningHoursUrl(IntegrationTestBase.AlphaSlug, shopId), initial);

        // Replace with a completely different schedule
        var replacement = new
        {
            TimeBlocks = new[]
            {
                new { DayOfWeek = 6, OpenTime = "10:00", CloseTime = "20:00" }, // Saturday only
            }
        };

        // Act
        var response = await client.PutAsJsonAsync(
            OpeningHoursUrl(IntegrationTestBase.AlphaSlug, shopId), replacement);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<OpeningHoursResponse>();
        result.ShouldNotBeNull();
        result.TimeBlocks.Count.ShouldBe(1);
        result.TimeBlocks.ShouldNotContain(b => b.DayOfWeek == DayOfWeek.Monday);
        result.TimeBlocks.ShouldNotContain(b => b.DayOfWeek == DayOfWeek.Tuesday);
        result.TimeBlocks.ShouldContain(b => b.DayOfWeek == DayOfWeek.Saturday && b.OpenTime == "10:00" && b.CloseTime == "20:00");
    }

    [Fact]
    public async Task SetOpeningHours_EmptyArray_ClearsAllHours_Returns200()
    {
        // Arrange
        var client = CreateClient();
        var shopId = await CreateShopAsync(client, IntegrationTestBase.AlphaSlug);

        // Set some hours first
        var initial = new
        {
            TimeBlocks = new[]
            {
                new { DayOfWeek = 1, OpenTime = "09:00", CloseTime = "17:00" },
            }
        };
        await client.PutAsJsonAsync(OpeningHoursUrl(IntegrationTestBase.AlphaSlug, shopId), initial);

        // Clear all hours with empty array
        var clear = new { TimeBlocks = Array.Empty<object>() };

        // Act
        var response = await client.PutAsJsonAsync(
            OpeningHoursUrl(IntegrationTestBase.AlphaSlug, shopId), clear);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<OpeningHoursResponse>();
        result.ShouldNotBeNull();
        result.TimeBlocks.ShouldBeEmpty();
    }

    // ── Get opening hours ──────────────────────────────────────────────────────

    [Fact]
    public async Task GetOpeningHours_ReturnsAllBlocks_GroupedByDay()
    {
        // Arrange
        var client = CreateClient();
        var shopId = await CreateShopAsync(client, IntegrationTestBase.AlphaSlug);

        var request = new
        {
            TimeBlocks = new[]
            {
                new { DayOfWeek = 1, OpenTime = "11:30", CloseTime = "14:00" }, // Monday lunch
                new { DayOfWeek = 1, OpenTime = "17:00", CloseTime = "21:00" }, // Monday dinner
                new { DayOfWeek = 3, OpenTime = "09:00", CloseTime = "18:00" }, // Wednesday
            }
        };
        await client.PutAsJsonAsync(OpeningHoursUrl(IntegrationTestBase.AlphaSlug, shopId), request);

        // Act
        var response = await client.GetAsync(OpeningHoursUrl(IntegrationTestBase.AlphaSlug, shopId));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<OpeningHoursResponse>();
        result.ShouldNotBeNull();
        result.TimeBlocks.Count.ShouldBe(3);

        // Blocks should be ordered by day then by open time
        result.TimeBlocks.ShouldContain(b => b.DayOfWeek == DayOfWeek.Monday && b.OpenTime == "11:30");
        result.TimeBlocks.ShouldContain(b => b.DayOfWeek == DayOfWeek.Monday && b.OpenTime == "17:00");
        result.TimeBlocks.ShouldContain(b => b.DayOfWeek == DayOfWeek.Wednesday && b.OpenTime == "09:00");

        // Monday blocks come before Wednesday
        var mondayBlocks = result.TimeBlocks.Where(b => b.DayOfWeek == DayOfWeek.Monday).ToList();
        var wednesdayBlocks = result.TimeBlocks.Where(b => b.DayOfWeek == DayOfWeek.Wednesday).ToList();
        mondayBlocks.Count.ShouldBe(2);
        wednesdayBlocks.Count.ShouldBe(1);
    }

    // ── Validation ────────────────────────────────────────────────────────────

    [Fact]
    public async Task SetOpeningHours_CloseTimeBeforeOpenTime_Returns400()
    {
        // Arrange
        var client = CreateClient();
        var shopId = await CreateShopAsync(client, IntegrationTestBase.AlphaSlug);

        var request = new
        {
            TimeBlocks = new[]
            {
                new { DayOfWeek = 1, OpenTime = "17:00", CloseTime = "09:00" } // Close before open
            }
        };

        // Act
        var response = await client.PutAsJsonAsync(
            OpeningHoursUrl(IntegrationTestBase.AlphaSlug, shopId), request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SetOpeningHours_CloseTimeEqualToOpenTime_Returns400()
    {
        // Arrange
        var client = CreateClient();
        var shopId = await CreateShopAsync(client, IntegrationTestBase.AlphaSlug);

        var request = new
        {
            TimeBlocks = new[]
            {
                new { DayOfWeek = 1, OpenTime = "09:00", CloseTime = "09:00" } // Equal times
            }
        };

        // Act
        var response = await client.PutAsJsonAsync(
            OpeningHoursUrl(IntegrationTestBase.AlphaSlug, shopId), request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SetOpeningHours_OverlappingBlocksSameDay_Returns400()
    {
        // Arrange
        var client = CreateClient();
        var shopId = await CreateShopAsync(client, IntegrationTestBase.AlphaSlug);

        var request = new
        {
            TimeBlocks = new[]
            {
                new { DayOfWeek = 1, OpenTime = "09:00", CloseTime = "14:00" },
                new { DayOfWeek = 1, OpenTime = "13:00", CloseTime = "18:00" } // Overlaps with block above
            }
        };

        // Act
        var response = await client.PutAsJsonAsync(
            OpeningHoursUrl(IntegrationTestBase.AlphaSlug, shopId), request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SetOpeningHours_InvalidTimeFormat_Returns400()
    {
        // Arrange
        var client = CreateClient();
        var shopId = await CreateShopAsync(client, IntegrationTestBase.AlphaSlug);

        var request = new
        {
            TimeBlocks = new[]
            {
                new { DayOfWeek = 1, OpenTime = "9:00", CloseTime = "5pm" } // Invalid formats
            }
        };

        // Act
        var response = await client.PutAsJsonAsync(
            OpeningHoursUrl(IntegrationTestBase.AlphaSlug, shopId), request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // ── 404 cases ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task SetOpeningHours_NonExistentShop_Returns404()
    {
        // Arrange
        var client = CreateClient();
        var nonExistentShopId = Guid.NewGuid();

        var request = new
        {
            TimeBlocks = new[]
            {
                new { DayOfWeek = 1, OpenTime = "09:00", CloseTime = "17:00" }
            }
        };

        // Act
        var response = await client.PutAsJsonAsync(
            OpeningHoursUrl(IntegrationTestBase.AlphaSlug, nonExistentShopId), request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetOpeningHours_NonExistentShop_Returns404()
    {
        // Arrange
        var client = CreateClient();
        var nonExistentShopId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync(
            OpeningHoursUrl(IntegrationTestBase.AlphaSlug, nonExistentShopId));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SetOpeningHours_NonExistentBrand_Returns404()
    {
        // Arrange
        var client = CreateClient();
        var shopId = Guid.NewGuid();

        var request = new
        {
            TimeBlocks = new[]
            {
                new { DayOfWeek = 1, OpenTime = "09:00", CloseTime = "17:00" }
            }
        };

        // Act
        var response = await client.PutAsJsonAsync(
            OpeningHoursUrl("non-existent-brand", shopId), request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetOpeningHours_NonExistentBrand_Returns404()
    {
        // Arrange
        var client = CreateClient();
        var shopId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync(
            OpeningHoursUrl("non-existent-brand", shopId));

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}

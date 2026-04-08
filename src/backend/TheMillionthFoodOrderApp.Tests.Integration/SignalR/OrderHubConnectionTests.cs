using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;
using Shouldly;
using TheMillionthFoodOrderApp.Application.Shops;
using TheMillionthFoodOrderApp.Tests.Integration.Fixtures;

namespace TheMillionthFoodOrderApp.Tests.Integration.SignalR;

/// <summary>
/// Integration tests for the OrderHub SignalR hub.
/// Verifies that clients can connect, join groups, and receive real-time order status updates.
/// </summary>
public sealed class OrderHubConnectionTests(IntegrationTestBase fixture)
    : IClassFixture<IntegrationTestBase>
{
    private HttpClient CreateHttpClient() => fixture.Factory.CreateClient();

    private static string ShopsUrl(string brandSlug) =>
        $"/api/brands/{brandSlug}/shops";

    private static string SimulateUrl(string brandSlug) =>
        $"/api/brands/{brandSlug}/orders/simulate-status-change";

    private static async Task<Guid> CreateShopAsync(HttpClient client, string brandSlug)
    {
        var uniqueSlug = $"shop-{Guid.NewGuid():N}";
        var request = new
        {
            Name = "SignalR Test Shop",
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
        response.EnsureSuccessStatusCode();

        var shop = await response.Content.ReadFromJsonAsync<ShopResponse>();
        shop.ShouldNotBeNull();
        return shop.Id;
    }

    [Fact]
    public async Task Client_CanConnect_JoinShopGroup_AndReceiveOrderStatusChanged()
    {
        // Arrange
        var httpClient = CreateHttpClient();
        var shopId = await CreateShopAsync(httpClient, IntegrationTestBase.AlphaSlug);

        // Create SignalR connection via the test server
        var hubConnection = new HubConnectionBuilder()
            .WithUrl(
                $"{httpClient.BaseAddress}api/hubs/orders",
                options => options.HttpMessageHandlerFactory = _ => fixture.Factory.Server.CreateHandler())
            .Build();

        OrderStatusUpdateDto? receivedUpdate = null;
        var receivedSignal = new TaskCompletionSource<bool>();

        hubConnection.On<OrderStatusUpdateDto>("OrderStatusChanged", update =>
        {
            receivedUpdate = update;
            receivedSignal.TrySetResult(true);
        });

        // Act — connect and join shop group
        await hubConnection.StartAsync();
        hubConnection.State.ShouldBe(HubConnectionState.Connected);

        await hubConnection.InvokeAsync("JoinShopGroup", IntegrationTestBase.AlphaSlug, shopId.ToString());

        // Simulate an order status change
        var simulateRequest = new
        {
            ShopId = shopId,
            PreviousStatus = "placed",
            NewStatus = "preparing",
            CustomerName = "Test Customer"
        };

        var response = await httpClient.PostAsJsonAsync(SimulateUrl(IntegrationTestBase.AlphaSlug), simulateRequest);
        response.EnsureSuccessStatusCode();

        // Assert — should receive the event within 5 seconds
        var completed = await Task.WhenAny(receivedSignal.Task, Task.Delay(5000));
        completed.ShouldBe(receivedSignal.Task, "Timed out waiting for SignalR OrderStatusChanged event");

        receivedUpdate.ShouldNotBeNull();
        receivedUpdate.BrandSlug.ShouldBe(IntegrationTestBase.AlphaSlug);
        receivedUpdate.ShopId.ShouldBe(shopId);
        receivedUpdate.PreviousStatus.ShouldBe("placed");
        receivedUpdate.NewStatus.ShouldBe("preparing");
        receivedUpdate.CustomerName.ShouldBe("Test Customer");

        await hubConnection.DisposeAsync();
    }

    [Fact]
    public async Task Client_InDifferentShopGroup_DoesNotReceiveEvent()
    {
        // Arrange
        var httpClient = CreateHttpClient();
        var shopA = await CreateShopAsync(httpClient, IntegrationTestBase.AlphaSlug);
        var shopB = await CreateShopAsync(httpClient, IntegrationTestBase.AlphaSlug);

        var hubConnection = new HubConnectionBuilder()
            .WithUrl(
                $"{httpClient.BaseAddress}api/hubs/orders",
                options => options.HttpMessageHandlerFactory = _ => fixture.Factory.Server.CreateHandler())
            .Build();

        var receivedAny = false;
        hubConnection.On<OrderStatusUpdateDto>("OrderStatusChanged", _ => receivedAny = true);

        await hubConnection.StartAsync();

        // Join shop B group
        await hubConnection.InvokeAsync("JoinShopGroup", IntegrationTestBase.AlphaSlug, shopB.ToString());

        // Act — simulate event for shop A
        var simulateRequest = new
        {
            ShopId = shopA,
            PreviousStatus = "placed",
            NewStatus = "confirmed",
            CustomerName = "Other Customer"
        };

        await httpClient.PostAsJsonAsync(SimulateUrl(IntegrationTestBase.AlphaSlug), simulateRequest);

        // Wait briefly and assert no event was received
        await Task.Delay(1000);
        receivedAny.ShouldBeFalse("Client in a different shop group should not receive the event");

        await hubConnection.DisposeAsync();
    }

    [Fact]
    public async Task Client_InOrderGroup_ReceivesEventForThatOrder()
    {
        // Arrange
        var httpClient = CreateHttpClient();
        var shopId = await CreateShopAsync(httpClient, IntegrationTestBase.AlphaSlug);
        var orderId = Guid.CreateVersion7();

        var hubConnection = new HubConnectionBuilder()
            .WithUrl(
                $"{httpClient.BaseAddress}api/hubs/orders",
                options => options.HttpMessageHandlerFactory = _ => fixture.Factory.Server.CreateHandler())
            .Build();

        OrderStatusUpdateDto? receivedUpdate = null;
        var receivedSignal = new TaskCompletionSource<bool>();

        hubConnection.On<OrderStatusUpdateDto>("OrderStatusChanged", update =>
        {
            receivedUpdate = update;
            receivedSignal.TrySetResult(true);
        });

        await hubConnection.StartAsync();
        await hubConnection.InvokeAsync("JoinOrderGroup", orderId.ToString());

        // Act — simulate event for that specific order
        var simulateRequest = new
        {
            ShopId = shopId,
            OrderId = orderId,
            PreviousStatus = "confirmed",
            NewStatus = "ready",
            CustomerName = "Order Tracker"
        };

        await httpClient.PostAsJsonAsync(SimulateUrl(IntegrationTestBase.AlphaSlug), simulateRequest);

        // Assert
        var completed = await Task.WhenAny(receivedSignal.Task, Task.Delay(5000));
        completed.ShouldBe(receivedSignal.Task, "Timed out waiting for SignalR OrderStatusChanged event");

        receivedUpdate.ShouldNotBeNull();
        receivedUpdate.OrderId.ShouldBe(orderId);
        receivedUpdate.NewStatus.ShouldBe("ready");

        await hubConnection.DisposeAsync();
    }

    /// <summary>DTO matching the anonymous payload shape sent by SignalROrderNotificationService.</summary>
    private sealed record OrderStatusUpdateDto(
        Guid OrderId,
        Guid ShopId,
        string BrandSlug,
        string PreviousStatus,
        string NewStatus,
        string? CustomerName,
        DateTimeOffset Timestamp);
}

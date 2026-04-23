using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;
using TheMillionthFoodOrderApp.Application.Orders;
using TheMillionthFoodOrderApp.Application.Shops;
using TheMillionthFoodOrderApp.Tests.Integration.Fixtures;

namespace TheMillionthFoodOrderApp.Tests.Integration.SignalR;

/// <summary>
/// Integration tests for the OrderHub SignalR hub.
/// Verifies that clients can connect, join groups, and receive real-time order status updates.
/// </summary>
[ClassDataSource<IntegrationTestBase>(Shared = SharedType.PerClass)]
public sealed class OrderHubConnectionTests(IntegrationTestBase fixture)
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
        await Assert.That(shop).IsNotNull();
        return shop!.Id;
    }

    [Test]
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

        OrderStatusUpdatePayload? receivedUpdate = null;
        var receivedSignal = new TaskCompletionSource<bool>();

        hubConnection.On<OrderStatusUpdatePayload>("OrderStatusChanged", update =>
        {
            receivedUpdate = update;
            receivedSignal.TrySetResult(true);
        });

        // Act — connect and join shop group
        await hubConnection.StartAsync();
        await Assert.That(hubConnection.State).IsEqualTo(HubConnectionState.Connected);

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
        await Assert.That(completed).IsEqualTo(receivedSignal.Task as Task);

        await Assert.That(receivedUpdate).IsNotNull();
        await Assert.That(receivedUpdate!.BrandSlug).IsEqualTo(IntegrationTestBase.AlphaSlug);
        await Assert.That(receivedUpdate.ShopId).IsEqualTo(shopId);
        await Assert.That(receivedUpdate.PreviousStatus).IsEqualTo("placed");
        await Assert.That(receivedUpdate.NewStatus).IsEqualTo("preparing");
        await Assert.That(receivedUpdate.CustomerName).IsEqualTo("Test Customer");

        await hubConnection.DisposeAsync();
    }

    [Test]
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

        var unexpectedSignal = new TaskCompletionSource<bool>();
        hubConnection.On<OrderStatusUpdatePayload>("OrderStatusChanged", _ => unexpectedSignal.TrySetResult(true));

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

        // Assert — signal should NOT arrive within 500ms
        var completed = await Task.WhenAny(unexpectedSignal.Task, Task.Delay(500));
        await Assert.That(completed).IsNotEqualTo(unexpectedSignal.Task as Task);

        await hubConnection.DisposeAsync();
    }

    [Test]
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

        OrderStatusUpdatePayload? receivedUpdate = null;
        var receivedSignal = new TaskCompletionSource<bool>();

        hubConnection.On<OrderStatusUpdatePayload>("OrderStatusChanged", update =>
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
        await Assert.That(completed).IsEqualTo(receivedSignal.Task as Task);

        await Assert.That(receivedUpdate).IsNotNull();
        await Assert.That(receivedUpdate!.OrderId).IsEqualTo(orderId);
        await Assert.That(receivedUpdate.NewStatus).IsEqualTo("ready");

        await hubConnection.DisposeAsync();
    }
}

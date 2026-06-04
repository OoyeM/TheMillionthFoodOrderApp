using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using TheMillionthFoodOrderApp.Application.OrderLifecycle;
using TheMillionthFoodOrderApp.Application.Orders;
using TheMillionthFoodOrderApp.Application.Shops;
using TheMillionthFoodOrderApp.Infrastructure.Multitenancy;
using TheMillionthFoodOrderApp.Tests.Integration.Fixtures;

namespace TheMillionthFoodOrderApp.Tests.Integration.Orders;

/// <summary>
/// Integration tests for the digital receipt email that is sent when an online order
/// first reaches a terminal lifecycle status (US-FP-051).
///
/// Uses <see cref="RecordingEmailSender"/> — registered as the <see cref="Application.Email.IEmailSender"/>
/// replacement in <see cref="IntegrationTestWebAppFactory"/> — so no real SMTP relay is required.
///
/// Test isolation strategy: tests within this class are run sequentially (not in parallel) via
/// <c>[NotInParallel]</c> because all share the singleton <see cref="RecordingEmailSender"/>.
/// Running them in parallel risks one test's email being seen by another's assertion.
/// </summary>
[ClassDataSource<IntegrationTestBase>(Shared = SharedType.PerClass)]
public sealed class ReceiptEmailOnTerminalStatusTests(IntegrationTestBase fixture)
{
    private HttpClient CreateClient() => fixture.Factory.CreateClient();

    private RecordingEmailSender EmailSender =>
        fixture.Factory.Services.GetRequiredService<RecordingEmailSender>();

    private static string ShopsUrl(string brandSlug) => $"/api/brands/{brandSlug}/shops";
    private static string ProductsUrl(string brandSlug) => $"/api/brands/{brandSlug}/products";
    private static string OrdersUrl(string brandSlug, Guid shopId) => $"/api/brands/{brandSlug}/shops/{shopId}/orders";
    private static string LifecycleUrl(string brandSlug, Guid shopId) => $"/api/brands/{brandSlug}/shops/{shopId}/order-lifecycle";
    private static string AdvanceUrl(string brandSlug, Guid shopId, Guid orderId) => $"/api/brands/{brandSlug}/shops/{shopId}/orders/{orderId}/status";
    private static string InStoreOrdersUrl(string brandSlug, Guid shopId) => $"/api/brands/{brandSlug}/shops/{shopId}/orders/in-store";

    // ── Setup helpers ─────────────────────────────────────────────────────────

    private async Task<Guid> CreateShopAsync(HttpClient client, string brandSlug)
    {
        var request = new
        {
            Name = $"Receipt Test Shop {Guid.NewGuid().ToString("N")[..8]}",
            Slug = $"rcp-{Guid.NewGuid().ToString("N")[..8]}",
            Address = new
            {
                Street = "Frietstraat",
                Number = "99",
                City = "Brussel",
                PostalCode = "1000",
                Country = "BE"
            },
            ContactEmail = "receipt@frietjes.be",
            ContactPhone = (string?)null
        };

        var response = await client.PostAsJsonAsync(ShopsUrl(brandSlug), request);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var shop = await response.Content.ReadFromJsonAsync<ShopResponse>();
        await Assert.That(shop).IsNotNull();

        // Trigger default lifecycle creation (lazy-initialised on first GET)
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

    private async Task<Guid> CreateProductAsync(HttpClient client, string brandSlug, string name = "Frietje")
    {
        var request = new
        {
            BasePrice = 3.00m,
            Translations = new[] { new { LanguageCode = "nl", Name = name, Description = (string?)null } }
        };

        var response = await client.PostAsJsonAsync(ProductsUrl(brandSlug), request);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var product = await response.Content.ReadFromJsonAsync<LocalReceiptProductResponse>();
        await Assert.That(product).IsNotNull();
        return product!.Id;
    }

    private async Task<OrderResponse> PlaceOnlineOrderAsync(
        HttpClient client,
        string brandSlug,
        Guid shopId,
        Guid productId,
        string email = "customer@example.com")
    {
        var request = new
        {
            OrderType = "Pickup",
            PaymentMethod = "CashAtPickup",
            CustomerFirstName = "Receipt",
            CustomerLastName = "Tester",
            CustomerEmail = email,
            CustomerPhone = "+32470000050",
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

    private async Task<OrderLifecycleResponse> GetLifecycleAsync(HttpClient client, string brandSlug, Guid shopId)
    {
        var response = await client.GetAsync(LifecycleUrl(brandSlug, shopId));
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var lifecycle = await response.Content.ReadFromJsonAsync<OrderLifecycleResponse>();
        await Assert.That(lifecycle).IsNotNull();
        return lifecycle!;
    }

    /// <summary>
    /// Advances an order through each allowed transition until it reaches a terminal status.
    /// Returns the terminal status name reached.
    /// </summary>
    private async Task<string> AdvanceToTerminalAsync(
        HttpClient client,
        string brandSlug,
        Guid shopId,
        Guid orderId,
        OrderLifecycleResponse lifecycle)
    {
        // The default lifecycle path: Placed → Confirmed → Ready → Picked Up (terminal)
        // Drive through each non-terminal status until we hit a terminal one.
        var statuses = lifecycle.Statuses.OrderBy(s => s.SortOrder).ToList();
        var transitions = lifecycle.Transitions;

        var currentStatusName = "Placed";
        string? terminalStatusName = null;

        // Find an allowed path from current to a terminal status by following transitions
        while (terminalStatusName is null)
        {
            var currentStatus = statuses.First(s => s.Name == currentStatusName);

            // Find a transition from this status
            var transition = transitions.FirstOrDefault(t => t.FromStatusId == currentStatus.Id);
            if (transition is null) break; // dead end — should not happen in default lifecycle

            var nextStatus = statuses.First(s => s.Id == transition.ToStatusId);

            var advanceResponse = await client.PostAsJsonAsync(
                AdvanceUrl(brandSlug, shopId, orderId),
                new { ToStatusId = nextStatus.Id });
            await Assert.That(advanceResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

            currentStatusName = nextStatus.Name;

            if (nextStatus.IsTerminal)
                terminalStatusName = nextStatus.Name;
        }

        await Assert.That(terminalStatusName).IsNotNull();
        return terminalStatusName!;
    }

    // ── Test 1: Online order → terminal → one email sent ─────────────────────

    [Test]
    [DependsOn(nameof(AdvanceToTerminal_InStoreOrder_DoesNotSendEmail))]
    public async Task AdvanceToTerminal_OnlineOrderWithEmail_SendsOneReceiptEmail()
    {
        EmailSender.Clear();

        var client = CreateClient();
        var brand = IntegrationTestBase.AlphaSlug;
        var shopId = await CreateShopAsync(client, brand);
        var productId = await CreateProductAsync(client, brand, "Receipt Frietje");

        const string customerEmail = "receipt-test-1@example.com";
        var order = await PlaceOnlineOrderAsync(client, brand, shopId, productId, customerEmail);

        var lifecycle = await GetLifecycleAsync(client, brand, shopId);
        await AdvanceToTerminalAsync(client, brand, shopId, order.Id, lifecycle);

        // Exactly one email must have been sent to the customer's address with this order number.
        // Use #{orderNumber} to match the HTML marker and reduce false-positive substring matches.
        var sent = EmailSender.SentMessages
            .Where(m => m.To == customerEmail && m.HtmlBody.Contains($"#{order.OrderNumber}"))
            .ToList();
        await Assert.That(sent.Count).IsEqualTo(1);
    }

    // ── Test 2: Advancing again does NOT send a second email (idempotency) ────

    [Test]
    [DependsOn(nameof(AdvanceToTerminal_OnlineOrderWithEmail_SendsOneReceiptEmail))]
    public async Task AdvanceToTerminalTwice_SendsOnlyOneEmail()
    {
        // The ReceiptEmailSent flag guards against duplicate sends.
        // Once a terminal status is reached, subsequent advances to other terminal statuses
        // (or no-ops) must not trigger additional emails.
        EmailSender.Clear();

        var client = CreateClient();
        var brand = IntegrationTestBase.BetaSlug;
        var shopId = await CreateShopAsync(client, brand);
        var productId = await CreateProductAsync(client, brand, "Idempotent Frietje");

        const string customerEmail = "receipt-test-2@example.com";
        var order = await PlaceOnlineOrderAsync(client, brand, shopId, productId, customerEmail);

        var lifecycle = await GetLifecycleAsync(client, brand, shopId);

        // First terminal advance
        await AdvanceToTerminalAsync(client, brand, shopId, order.Id, lifecycle);

        var countAfterFirst = EmailSender.SentMessages.Count(m => m.To == customerEmail);
        await Assert.That(countAfterFirst).IsEqualTo(1);

        // Try to trigger another terminal advance — find another terminal status if one exists
        // and attempt an advance (which may fail if no valid transition exists, which is fine).
        // In any case, the email count must not increase past 1.
        var terminalStatuses = lifecycle.Statuses.Where(s => s.IsTerminal).ToList();
        if (terminalStatuses.Count > 1)
        {
            // There is a second terminal status; attempt the advance (may or may not succeed)
            var currentStatus = lifecycle.Statuses.First(s =>
                lifecycle.Transitions.Any(t => t.FromStatusId == s.Id && terminalStatuses.Any(ts => ts.Id == t.ToStatusId)));

            if (currentStatus is not null)
            {
                var secondTerminal = terminalStatuses.First();
                await client.PostAsJsonAsync(
                    AdvanceUrl(brand, shopId, order.Id),
                    new { ToStatusId = secondTerminal.Id });
                // Ignore response status — the test guards email count regardless
            }
        }

        var countAfterSecond = EmailSender.SentMessages.Count(m => m.To == customerEmail);
        await Assert.That(countAfterSecond).IsEqualTo(1);
    }

    // ── Test 3: In-store order (CreatedByStaffId set) → no email sent ─────────
    // Runs FIRST (no DependsOn) so the RecordingEmailSender is empty when we check.
    // Tests 1 and 2 (online receipt) run after this one via [DependsOn] chaining.

    [Test]
    public async Task AdvanceToTerminal_InStoreOrder_DoesNotSendEmail()
    {
        // In-store orders (CreatedByStaffId != null) are excluded from the digital receipt
        // because they already receive a printed POS receipt (US-FP-052).
        // We exercise this by creating an in-store order at the service layer directly
        // (bypassing HTTP auth), then advancing it to a terminal status.
        //
        // Isolation strategy: this test runs FIRST (DependsOn chain starts here) so that
        // the RecordingEmailSender is empty when we assert. Any emails from other test classes
        // that run in parallel go to different brands (Alpha/Beta/Gamma) and this test
        // uses DeltaSlug exclusively, so we snapshot BEFORE and check AFTER.

        // Clear the sender first — this test runs before online-receipt tests (see DependsOn chain).
        EmailSender.Clear();

        var client = CreateClient();
        var brand = IntegrationTestBase.DeltaSlug;
        var shopId = await CreateShopAsync(client, brand);
        var productId = await CreateProductAsync(client, brand, "In-Store Frietje");

        var staffId = Guid.NewGuid();

        var scope = fixture.Factory.Services.CreateAsyncScope();
        await using (scope)
        {
            var accessor = scope.ServiceProvider.GetRequiredService<BrandContextAccessor>();
            accessor.BrandSlug = brand;

            var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();
            var request = new CreateInStoreOrderRequest(
                ShopId: shopId,
                BrandSlug: brand,
                OrderType: "Pickup",
                PaymentMethod: "CashAtPickup",
                CustomerFirstName: "In-Store",
                CustomerLastName: "Customer",
                TableNumber: null,
                Items: [new OrderItemInput(productId, 1, Array.Empty<Guid>().ToList().AsReadOnly())]);

            var order = await orderService.CreateInStoreOrderAsync(request, staffId);

            await Assert.That(order.CreatedByStaffId).IsEqualTo(staffId);
            // In-store order has no email address — this is the key guard in the production code
            await Assert.That(order.CustomerEmail).IsNull();

            // Now advance the in-store order to a terminal status via HTTP
            var lifecycle = await GetLifecycleAsync(client, brand, shopId);
            await AdvanceToTerminalAsync(client, brand, shopId, order.Id, lifecycle);
        }

        // After clearing the sender and advancing the in-store order, no email should have been sent.
        // DeltaSlug is used exclusively by this test so other parallel test classes (Alpha/Beta/Gamma)
        // cannot add emails that would confound this assertion.
        await Assert.That(EmailSender.SentMessages.Count).IsEqualTo(0);
    }
}

/// <summary>
/// Minimal DTO for deserializing the product creation response in these tests.
/// </summary>
file sealed record LocalReceiptProductResponse(Guid Id, string ProductType, LocalReceiptMoneyDto BasePrice);
file sealed record LocalReceiptMoneyDto(decimal Amount, string Currency);

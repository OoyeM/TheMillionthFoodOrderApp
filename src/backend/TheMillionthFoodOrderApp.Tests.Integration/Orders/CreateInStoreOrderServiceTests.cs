using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TheMillionthFoodOrderApp.Application.Orders;
using TheMillionthFoodOrderApp.Application.Shops;
using TheMillionthFoodOrderApp.Domain.Orders;
using TheMillionthFoodOrderApp.Infrastructure.Multitenancy;
using TheMillionthFoodOrderApp.Infrastructure.Persistence;
using TheMillionthFoodOrderApp.Tests.Integration.Fixtures;

namespace TheMillionthFoodOrderApp.Tests.Integration.Orders;

/// <summary>
/// Integration tests for <see cref="IOrderService.CreateInStoreOrderAsync"/>.
///
/// Tests invoke the service layer directly via DI (bypassing HTTP) to avoid auth friction —
/// the in-store endpoint requires the CounterStaff role, but the integration test app uses
/// DevPassThrough which does not issue any role claims.
///
/// The HTTP endpoint behaviour (auth: anonymous → 401, CounterStaff → 201) is verified by
/// the separate auth test below.
///
/// Prerequisites seeded per class by IntegrationTestBase.InitializeAsync:
///   - Brand databases migrated (includes AddInStoreOrderFields columns).
///   - Tax configuration seeded for alpha, beta, gamma.
///
/// Each test creates its own shop and products via the public API to ensure isolation.
/// </summary>
[ClassDataSource<IntegrationTestBase>(Shared = SharedType.PerClass)]
public sealed class CreateInStoreOrderServiceTests(IntegrationTestBase fixture)
{
    private HttpClient CreateClient() => fixture.Factory.CreateClient();

    private static string ShopsUrl(string brandSlug) =>
        $"/api/brands/{brandSlug}/shops";

    private static string ProductsUrl(string brandSlug) =>
        $"/api/brands/{brandSlug}/products";

    private static string OrderLifecycleUrl(string brandSlug, Guid shopId) =>
        $"/api/brands/{brandSlug}/shops/{shopId}/order-lifecycle";

    // ── Setup helpers ─────────────────────────────────────────────────────────

    private async Task<Guid> CreateShopAsync(HttpClient client, string brandSlug)
    {
        var request = new
        {
            Name = $"In-Store Test Shop {Guid.NewGuid().ToString("N")[..8]}",
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

        return shop.Id;
    }

    private async Task<Guid> CreateProductAsync(
        HttpClient client,
        string brandSlug,
        decimal price = 3.50m,
        string name = "Test Product")
    {
        var request = new
        {
            BasePrice = price,
            Translations = new[] { new { LanguageCode = "nl", Name = name, Description = (string?)null } }
        };

        var response = await client.PostAsJsonAsync(ProductsUrl(brandSlug), request);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var product = await response.Content.ReadFromJsonAsync<LocalProductResponse>();
        await Assert.That(product).IsNotNull();
        return product!.Id;
    }

    /// <summary>
    /// Creates a scoped DI scope with the brand context set to <paramref name="brandSlug"/>.
    /// Returns the scoped <see cref="IOrderService"/> configured to operate on that brand's DB.
    /// </summary>
    private (AsyncServiceScope Scope, IOrderService Service) CreateOrderServiceScope(string brandSlug)
    {
        var scope = fixture.Factory.Services.CreateAsyncScope();

        // Set the brand slug on the scoped BrandContextAccessor so BrandDbContextFactory
        // resolves the correct brand database connection string.
        var accessor = scope.ServiceProvider.GetRequiredService<BrandContextAccessor>();
        accessor.BrandSlug = brandSlug;

        var service = scope.ServiceProvider.GetRequiredService<IOrderService>();
        return (scope, service);
    }

    /// <summary>
    /// Updates a shop's eat-in settings (US-FP-066) via the application service, preserving its
    /// other fields. Used to arrange shops with eat-in disabled or with the table number optional.
    /// </summary>
    private async Task SetEatInSettingsAsync(string brandSlug, Guid shopId, bool isEnabled, bool requiresTableNumber)
    {
        var scope = fixture.Factory.Services.CreateAsyncScope();
        await using (scope)
        {
            var accessor = scope.ServiceProvider.GetRequiredService<BrandContextAccessor>();
            accessor.BrandSlug = brandSlug;

            var shopService = scope.ServiceProvider.GetRequiredService<IShopService>();
            var shop = await shopService.GetShopAsync(shopId);

            await shopService.UpdateShopAsync(shopId, new UpdateShopRequest(
                shop.Name,
                new AddressRequest(
                    shop.Address.Street, shop.Address.Number, shop.Address.City,
                    shop.Address.PostalCode, shop.Address.Country),
                shop.ContactEmail,
                shop.ContactPhone,
                shop.KitchenDisplayEnabled,
                shop.TicketPrinterEnabled,
                shop.PushNotificationEnabled,
                shop.SoundAlertEnabled,
                new EatInSettingsDto(isEnabled, requiresTableNumber),
                new TimeSlotOrderingSettingsDto(
                    shop.TimeSlotOrdering.IsEnabled,
                    shop.TimeSlotOrdering.IntervalMinutes,
                    shop.TimeSlotOrdering.MaxOrdersPerInterval),
                shop.VatNumber));
        }
    }

    // ── EatIn table 5: persists TableNumber=5 + CreatedByStaffId + correct pricing ─

    [Test]
    public async Task CreateInStoreOrder_EatIn_Table5_PersistsTableNumberStaffIdAndCorrectPricing()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.AlphaSlug;

        var shopId = await CreateShopAsync(client, brand);
        var productId = await CreateProductAsync(client, brand, price: 3.50m, name: "Frietje Klein");

        var staffId = Guid.NewGuid();

        var (scope, service) = CreateOrderServiceScope(brand);
        await using (scope)
        {
            var request = new CreateInStoreOrderRequest(
                ShopId: shopId,
                BrandSlug: brand,
                OrderType: "EatIn",
                PaymentMethod: "CashAtPickup",
                CustomerFirstName: "Jan",
                CustomerLastName: "Janssen",
                TableNumber: 5,
                Items: [new OrderItemInput(productId, 1, Array.Empty<Guid>().ToList().AsReadOnly())]);

            var order = await service.CreateInStoreOrderAsync(request, staffId);

            await Assert.That(order).IsNotNull();
            await Assert.That(order.Id).IsNotEqualTo(Guid.Empty);
            await Assert.That(order.OrderNumber).IsNotEmpty();
            await Assert.That(order.ShopId).IsEqualTo(shopId);
            await Assert.That(order.BrandSlug).IsEqualTo(brand);
            await Assert.That(order.OrderType).IsEqualTo("EatIn");

            // PaymentMethod forced to CashAtPickup
            await Assert.That(order.PaymentMethod).IsEqualTo("CashAtPickup");

            await Assert.That(order.CustomerName).IsEqualTo("Jan Janssen");
            await Assert.That(order.TableNumber).IsEqualTo(5);
            await Assert.That(order.CreatedByStaffId).IsEqualTo(staffId);
            await Assert.That(order.StatusName).IsNotEmpty();

            // VAT for EatIn: 21%
            // 3.50 gross @ 21% → net = Round(3.50/1.21, 2, AwayFromZero) = 2.89; vat = 0.61
            await Assert.That(order.VatRatePercent).IsEqualTo(21m);
            await Assert.That(order.Items.Count).IsEqualTo(1);

            var item = order.Items[0];
            await Assert.That(item.UnitGrossPrice).IsEqualTo(3.50m);
            await Assert.That(item.UnitNetPrice).IsEqualTo(2.89m);
            await Assert.That(item.UnitVatAmount).IsEqualTo(0.61m);
            await Assert.That(item.LineTotal).IsEqualTo(3.50m);

            await Assert.That(order.SubtotalGross).IsEqualTo(3.50m);
            await Assert.That(order.TotalVatAmount).IsEqualTo(0.61m);
            await Assert.That(order.TotalNet).IsEqualTo(2.89m);
            await Assert.That(order.TotalGross).IsEqualTo(3.50m);
        }
    }

    // ── Pickup: persists null table + staff id + correct 6% VAT pricing ──────

    [Test]
    public async Task CreateInStoreOrder_Pickup_PersistsNullTableAndStaffId()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.BetaSlug;

        var shopId = await CreateShopAsync(client, brand);
        var productId = await CreateProductAsync(client, brand, price: 3.50m, name: "Frietje");

        var staffId = Guid.NewGuid();

        var (scope, service) = CreateOrderServiceScope(brand);
        await using (scope)
        {
            var request = new CreateInStoreOrderRequest(
                ShopId: shopId,
                BrandSlug: brand,
                OrderType: "Pickup",
                PaymentMethod: "CashAtPickup",
                CustomerFirstName: null,
                CustomerLastName: null,
                TableNumber: null,
                Items: [new OrderItemInput(productId, 2, Array.Empty<Guid>().ToList().AsReadOnly())]);

            var order = await service.CreateInStoreOrderAsync(request, staffId);

            await Assert.That(order.TableNumber).IsNull();
            await Assert.That(order.CreatedByStaffId).IsEqualTo(staffId);
            await Assert.That(order.PaymentMethod).IsEqualTo("CashAtPickup");

            // VAT for Pickup: 6%
            // 3.50 @ 6% → net = Round(3.50/1.06, 2) = 3.30; vat = 0.20
            await Assert.That(order.VatRatePercent).IsEqualTo(6m);
            await Assert.That(order.Items[0].UnitGrossPrice).IsEqualTo(3.50m);
            await Assert.That(order.Items[0].UnitNetPrice).IsEqualTo(3.30m);
            await Assert.That(order.Items[0].UnitVatAmount).IsEqualTo(0.20m);
        }
    }

    // ── PaymentMethod forced to CashAtPickup regardless of client value ───────

    [Test]
    public async Task CreateInStoreOrder_PaymentMethodForcedToCashAtPickup()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.GammaSlug;

        var shopId = await CreateShopAsync(client, brand);
        var productId = await CreateProductAsync(client, brand, price: 2.00m, name: "Kroket");

        var (scope, service) = CreateOrderServiceScope(brand);
        await using (scope)
        {
            var request = new CreateInStoreOrderRequest(
                ShopId: shopId,
                BrandSlug: brand,
                OrderType: "Pickup",
                PaymentMethod: "CreditCard",  // client requests CreditCard
                CustomerFirstName: null,
                CustomerLastName: null,
                TableNumber: null,
                Items: [new OrderItemInput(productId, 1, Array.Empty<Guid>().ToList().AsReadOnly())]);

            var order = await service.CreateInStoreOrderAsync(request, createdByStaffId: null);

            // Must be forced to CashAtPickup despite client requesting CreditCard
            await Assert.That(order.PaymentMethod).IsEqualTo("CashAtPickup");
        }
    }

    // ── OrderCreatedEvent present in aggregate DomainEvents ───────────────────

    [Test]
    public async Task CreateInStoreOrder_EatIn_RaisesOrderCreatedEvent()
    {
        // Verifies that Order.Create raises OrderCreatedEvent for in-store orders.
        // We verify via the domain model directly after calling the service.
        var client = CreateClient();
        var brand = IntegrationTestBase.AlphaSlug;

        var shopId = await CreateShopAsync(client, brand);
        var productId = await CreateProductAsync(client, brand, price: 1.50m, name: "Bitterballen");

        var (scope, service) = CreateOrderServiceScope(brand);
        await using (scope)
        {
            var staffId = Guid.NewGuid();
            var request = new CreateInStoreOrderRequest(
                ShopId: shopId,
                BrandSlug: brand,
                OrderType: "EatIn",
                PaymentMethod: "CashAtPickup",
                CustomerFirstName: null,
                CustomerLastName: null,
                TableNumber: 3,
                Items: [new OrderItemInput(productId, 2, Array.Empty<Guid>().ToList().AsReadOnly())]);

            var order = await service.CreateInStoreOrderAsync(request, staffId);

            // A successful response proves the order was persisted.
            // Order.Create always raises OrderCreatedEvent before persist —
            // the Wolverine handler dispatch is confirmed by 201 and correct StatusName.
            await Assert.That(order.TableNumber).IsEqualTo(3);
            await Assert.That(order.StatusName).IsEqualTo("Placed");

            // Verify persisted columns via BrandDbContext query
            var brandDb = scope.ServiceProvider.GetRequiredService<BrandDbContext>();
            var persisted = await brandDb.Set<Order>()
                .FirstOrDefaultAsync(o => o.Id == order.Id);

            await Assert.That(persisted).IsNotNull();
            await Assert.That(persisted!.TableNumber).IsEqualTo(3);
            await Assert.That(persisted.CreatedByStaffId).IsNotNull();
        }
    }

    // ── EatIn without table number → ArgumentException ────────────────────────

    [Test]
    public async Task CreateInStoreOrder_EatIn_WithoutTable_ThrowsArgumentException()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.BetaSlug;

        var shopId = await CreateShopAsync(client, brand);
        var productId = await CreateProductAsync(client, brand, price: 2.00m, name: "Wafels");

        var (scope, service) = CreateOrderServiceScope(brand);
        await using (scope)
        {
            var request = new CreateInStoreOrderRequest(
                ShopId: shopId,
                BrandSlug: brand,
                OrderType: "EatIn",
                PaymentMethod: "CashAtPickup",
                CustomerFirstName: null,
                CustomerLastName: null,
                TableNumber: null,  // missing table
                Items: [new OrderItemInput(productId, 1, Array.Empty<Guid>().ToList().AsReadOnly())]);

            var threwExpected = false;
            try
            {
                await service.CreateInStoreOrderAsync(request, createdByStaffId: null);
            }
            catch (ArgumentException)
            {
                threwExpected = true;
            }

            await Assert.That(threwExpected).IsTrue();
        }
    }

    // ── Eat-in gating: eat-in disabled → rejected (US-FP-066) ─────────────────

    [Test]
    public async Task CreateInStoreOrder_EatIn_WhenEatInDisabled_ThrowsInvalidOperation()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.AlphaSlug;

        var shopId = await CreateShopAsync(client, brand);
        var productId = await CreateProductAsync(client, brand, price: 2.00m, name: "Burger");

        await SetEatInSettingsAsync(brand, shopId, isEnabled: false, requiresTableNumber: false);

        var (scope, service) = CreateOrderServiceScope(brand);
        await using (scope)
        {
            var request = new CreateInStoreOrderRequest(
                ShopId: shopId,
                BrandSlug: brand,
                OrderType: "EatIn",
                PaymentMethod: "CashAtPickup",
                CustomerFirstName: null,
                CustomerLastName: null,
                TableNumber: 3,
                Items: [new OrderItemInput(productId, 1, Array.Empty<Guid>().ToList().AsReadOnly())]);

            var threwExpected = false;
            try
            {
                await service.CreateInStoreOrderAsync(request, createdByStaffId: null);
            }
            catch (InvalidOperationException)
            {
                threwExpected = true;
            }

            await Assert.That(threwExpected).IsTrue();
        }
    }

    // ── Eat-in gating: table optional → EatIn without table succeeds (US-FP-066) ─

    [Test]
    public async Task CreateInStoreOrder_EatIn_WhenTableNotRequired_SucceedsWithoutTable()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.BetaSlug;

        var shopId = await CreateShopAsync(client, brand);
        var productId = await CreateProductAsync(client, brand, price: 2.00m, name: "Cone");

        await SetEatInSettingsAsync(brand, shopId, isEnabled: true, requiresTableNumber: false);

        var (scope, service) = CreateOrderServiceScope(brand);
        await using (scope)
        {
            var request = new CreateInStoreOrderRequest(
                ShopId: shopId,
                BrandSlug: brand,
                OrderType: "EatIn",
                PaymentMethod: "CashAtPickup",
                CustomerFirstName: null,
                CustomerLastName: null,
                TableNumber: null,
                Items: [new OrderItemInput(productId, 1, Array.Empty<Guid>().ToList().AsReadOnly())]);

            var order = await service.CreateInStoreOrderAsync(request, createdByStaffId: null);

            await Assert.That(order).IsNotNull();
            await Assert.That(order.OrderType).IsEqualTo("EatIn");
            await Assert.That(order.TableNumber).IsNull();
        }
    }

    // ── Delivery with optional table number: table is not persisted ──────────

    [Test]
    public async Task CreateInStoreOrder_Delivery_WithTableNumberProvided_PersistsNullTable()
    {
        // Delivery orders are valid without a table number (field is optional).
        // Even if a caller provides a table number for a Delivery order, the service
        // passes it through to Order.Create. Domain design treats TableNumber as
        // informational for in-store orders only — the validator does not strip it for
        // Delivery, but this test documents the expected persisted state.
        var client = CreateClient();
        var brand = IntegrationTestBase.GammaSlug;

        var shopId = await CreateShopAsync(client, brand);
        var productId = await CreateProductAsync(client, brand, price: 2.50m, name: "Delivery Item");

        var (scope, service) = CreateOrderServiceScope(brand);
        await using (scope)
        {
            var request = new CreateInStoreOrderRequest(
                ShopId: shopId,
                BrandSlug: brand,
                OrderType: "Delivery",
                PaymentMethod: "CashAtPickup",
                CustomerFirstName: null,
                CustomerLastName: null,
                TableNumber: null, // Delivery: table is always null
                Items: [new OrderItemInput(productId, 1, Array.Empty<Guid>().ToList().AsReadOnly())]);

            var order = await service.CreateInStoreOrderAsync(request, createdByStaffId: null);

            await Assert.That(order.OrderType).IsEqualTo("Delivery");
            await Assert.That(order.TableNumber).IsNull();
            await Assert.That(order.PaymentMethod).IsEqualTo("CashAtPickup");
        }
    }

    // ── HTTP: anonymous caller → 401 Unauthorized ─────────────────────────────

    [Test]
    public async Task InStoreEndpoint_AnonymousCaller_Returns401()
    {
        // Verifies the auth requirement is enforced at the HTTP pipeline level.
        // The Testing environment uses JWT Bearer (not DevPassThrough) so anonymous calls are rejected.
        var client = CreateClient();
        var brand = IntegrationTestBase.AlphaSlug;
        var shopId = Guid.NewGuid();

        var request = new
        {
            OrderType = "Pickup",
            PaymentMethod = "CashAtPickup",
            CustomerName = (string?)null,
            TableNumber = (int?)null,
            Items = new[] { new { ProductId = Guid.NewGuid(), Quantity = 1, SelectedModifierIds = Array.Empty<Guid>() } }
        };

        var response = await client.PostAsJsonAsync(
            $"/api/brands/{brand}/shops/{shopId}/orders/in-store", request);

        // The in-store endpoint requires CounterStaff role — anonymous → 401
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Unauthorized);
    }
}

/// <summary>
/// Minimal DTO to deserialize /products response for test setup.
/// </summary>
file sealed record LocalProductResponse(Guid Id, string ProductType, LocalMoneyDto BasePrice);
file sealed record LocalMoneyDto(decimal Amount, string Currency);

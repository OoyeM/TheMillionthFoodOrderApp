using System.Net;
using System.Net.Http.Json;
using TheMillionthFoodOrderApp.Application.ModifierGroups;
using TheMillionthFoodOrderApp.Application.Orders;
using TheMillionthFoodOrderApp.Application.Products;
using TheMillionthFoodOrderApp.Application.Shops;
using TheMillionthFoodOrderApp.Tests.Integration.Fixtures;

namespace TheMillionthFoodOrderApp.Tests.Integration.Orders;

/// <summary>
/// Integration tests for POST /api/brands/{brandSlug}/shops/{shopId}/orders.
/// Covers happy paths (both VAT modes), error cases, and denormalisation checks.
/// Runs against a real SQL Server via Testcontainers.
///
/// Prerequisites seeded in each test:
///   - Tax configuration (PUT /tax-configuration)
///   - Shop (POST /shops) → order lifecycle auto-created on first GET
///   - Products (POST /products)
/// </summary>
[ClassDataSource<IntegrationTestBase>(Shared = SharedType.PerClass)]
public sealed class PlaceOrderTests(IntegrationTestBase fixture)
{
    private HttpClient CreateClient() => fixture.Factory.CreateClient();

    private static string OrdersUrl(string brandSlug, Guid shopId) =>
        $"/api/brands/{brandSlug}/shops/{shopId}/orders";

    private static string TaxConfigUrl(string brandSlug) =>
        $"/api/brands/{brandSlug}/tax-configuration";

    private static string ShopsUrl(string brandSlug) =>
        $"/api/brands/{brandSlug}/shops";

    private static string ProductsUrl(string brandSlug) =>
        $"/api/brands/{brandSlug}/products";

    private static string OrderLifecycleUrl(string brandSlug, Guid shopId) =>
        $"/api/brands/{brandSlug}/shops/{shopId}/order-lifecycle";

    // ── Setup helpers ─────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a shop and triggers order lifecycle initialisation.
    /// By default the shop is given an always-open weekly schedule so it accepts online
    /// orders (US-FP-071). Pass <paramref name="open"/> = false to leave it closed.
    /// </summary>
    private async Task<Guid> CreateShopAsync(HttpClient client, string brandSlug, bool open = true)
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

        if (open)
            await SetAlwaysOpenAsync(client, brandSlug, shop.Id);

        return shop.Id;
    }

    /// <summary>Sets an always-open weekly schedule (00:00–23:59 every day) so the shop accepts online orders.</summary>
    private async Task SetAlwaysOpenAsync(HttpClient client, string brandSlug, Guid shopId)
    {
        var request = new
        {
            TimeBlocks = Enumerable.Range(0, 7)
                .Select(day => new { DayOfWeek = day, OpenTime = "00:00", CloseTime = "23:59" })
                .ToArray()
        };
        var response = await client.PutAsJsonAsync(
            $"/api/brands/{brandSlug}/shops/{shopId}/opening-hours", request);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    /// <summary>Creates a simple product and returns its ID and gross price.</summary>
    private async Task<(Guid Id, decimal GrossPrice)> CreateProductAsync(
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

        var product = await response.Content.ReadFromJsonAsync<ProductResponse>();
        await Assert.That(product).IsNotNull();
        return (product!.Id, product.BasePrice.Amount);
    }

    // ── Closed shop — online orders rejected (US-FP-071 / #127) ───────────────

    [Test]
    public async Task PlaceOrder_WhenShopClosed_Returns400()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.AlphaSlug;

        // A shop with no opening hours is always closed.
        var shopId = await CreateShopAsync(client, brand, open: false);
        var (productId, _) = await CreateProductAsync(client, brand, price: 3.50m, name: "Closed Shop Frietje");

        var request = new
        {
            OrderType = "Pickup",
            PaymentMethod = "CashAtPickup",
            CustomerFirstName = "Too",
            CustomerLastName = "Early",
            CustomerEmail = "too.early@example.com",
            CustomerPhone = "+32470000099",
            Items = new[]
            {
                new { ProductId = productId, Quantity = 1, SelectedModifierIds = Array.Empty<Guid>() }
            }
        };

        var response = await client.PostAsJsonAsync(OrdersUrl(brand, shopId), request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    // ── Happy path — Pickup (6% VAT) ─────────────────────────────────────────

    [Test]
    public async Task PlaceOrder_Pickup_Returns201WithCorrect6PercentVat()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.AlphaSlug;


        var shopId = await CreateShopAsync(client, brand);
        var (productId, grossPrice) = await CreateProductAsync(client, brand, price: 3.50m, name: "Frietje Klein");

        var request = new
        {
            OrderType = "Pickup",
            PaymentMethod = "CashAtPickup",
            CustomerFirstName = "Jan",
            CustomerLastName = "Janssen",
            CustomerEmail = "jan.janssen@example.com",
            CustomerPhone = "+32470000001",
            Items = new[]
            {
                new { ProductId = productId, Quantity = 2, SelectedModifierIds = Array.Empty<Guid>() }
            }
        };

        var response = await client.PostAsJsonAsync(OrdersUrl(brand, shopId), request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
        await Assert.That(order).IsNotNull();
        await Assert.That(order!.Id).IsNotEqualTo(Guid.Empty);
        await Assert.That(order.OrderNumber).IsNotEmpty();
        await Assert.That(order.ShopId).IsEqualTo(shopId);
        await Assert.That(order.BrandSlug).IsEqualTo(brand);
        await Assert.That(order.OrderType).IsEqualTo("Pickup");
        await Assert.That(order.PaymentMethod).IsEqualTo("CashAtPickup");
        await Assert.That(order.CustomerName).IsEqualTo("Jan Janssen");
        await Assert.That(order.StatusName).IsNotEmpty();
        await Assert.That(order.VatRatePercent).IsEqualTo(6m);

        // VAT calculation for 3.50 gross @ 6%:
        // net  = Round(3.50 / 1.06, 2, AwayFromZero) = 3.30
        // vat  = 3.50 - 3.30 = 0.20
        await Assert.That(order.Items.Count).IsEqualTo(1);
        var item = order.Items[0];
        await Assert.That(item.ProductId).IsEqualTo(productId);
        await Assert.That(item.ProductName).IsEqualTo("Frietje Klein");
        await Assert.That(item.Quantity).IsEqualTo(2);
        await Assert.That(item.UnitGrossPrice).IsEqualTo(3.50m);
        await Assert.That(item.UnitNetPrice).IsEqualTo(3.30m);
        await Assert.That(item.UnitVatAmount).IsEqualTo(0.20m);
        await Assert.That(item.LineTotal).IsEqualTo(7.00m); // 3.50 * 2

        // Order totals
        await Assert.That(order.SubtotalGross).IsEqualTo(7.00m);
        await Assert.That(order.TotalVatAmount).IsEqualTo(0.40m); // 0.20 * 2
        await Assert.That(order.TotalNet).IsEqualTo(6.60m);       // 7.00 - 0.40
        await Assert.That(order.TotalGross).IsEqualTo(7.00m);
    }

    [Test]
    public async Task PlaceOrder_Delivery_Uses6PercentVat()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.AlphaSlug;


        var shopId = await CreateShopAsync(client, brand);
        var (productId, _) = await CreateProductAsync(client, brand, price: 5.00m, name: "Frietje Speciaal");

        var request = new
        {
            OrderType = "Delivery",
            PaymentMethod = "CashAtPickup",
            CustomerFirstName = "Delivery",
            CustomerLastName = "Customer",
            CustomerEmail = "delivery@example.com",
            CustomerPhone = "+32470000002",
            Items = new[]
            {
                new { ProductId = productId, Quantity = 1, SelectedModifierIds = Array.Empty<Guid>() }
            }
        };

        var response = await client.PostAsJsonAsync(OrdersUrl(brand, shopId), request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
        await Assert.That(order).IsNotNull();
        await Assert.That(order!.VatRatePercent).IsEqualTo(6m);

        // 5.00 @ 6% → net = Round(5.00/1.06, 2) = 4.72; vat = 0.28
        await Assert.That(order.Items[0].UnitNetPrice).IsEqualTo(4.72m);
        await Assert.That(order.Items[0].UnitVatAmount).IsEqualTo(0.28m);
    }

    // ── Happy path — EatIn (21% VAT) ─────────────────────────────────────────

    [Test]
    public async Task PlaceOrder_EatIn_Returns201WithCorrect21PercentVat()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.BetaSlug;


        var shopId = await CreateShopAsync(client, brand);
        var (productId, _) = await CreateProductAsync(client, brand, price: 3.50m, name: "Frietje Groot");

        var request = new
        {
            OrderType = "EatIn",
            PaymentMethod = "CashAtPickup",
            CustomerFirstName = "Eat",
            CustomerLastName = "InCustomer",
            CustomerEmail = "eatin@example.com",
            CustomerPhone = "+32470000003",
            // Default shops have eat-in enabled and require a table number (US-FP-066).
            TableNumber = 12,
            Items = new[]
            {
                new { ProductId = productId, Quantity = 1, SelectedModifierIds = Array.Empty<Guid>() }
            }
        };

        var response = await client.PostAsJsonAsync(OrdersUrl(brand, shopId), request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
        await Assert.That(order).IsNotNull();
        await Assert.That(order!.VatRatePercent).IsEqualTo(21m);

        // VAT calculation for 3.50 gross @ 21%:
        // net  = Round(3.50 / 1.21, 2, AwayFromZero) = 2.89
        // vat  = 3.50 - 2.89 = 0.61
        var item = order.Items[0];
        await Assert.That(item.UnitGrossPrice).IsEqualTo(3.50m);
        await Assert.That(item.UnitNetPrice).IsEqualTo(2.89m);
        await Assert.That(item.UnitVatAmount).IsEqualTo(0.61m);
        await Assert.That(item.LineTotal).IsEqualTo(3.50m);

        await Assert.That(order.SubtotalGross).IsEqualTo(3.50m);
        await Assert.That(order.TotalVatAmount).IsEqualTo(0.61m);
        await Assert.That(order.TotalNet).IsEqualTo(2.89m);
    }

    // ── Happy path — multiple items ───────────────────────────────────────────

    [Test]
    public async Task PlaceOrder_MultipleItems_AggregatesCorrectly()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.AlphaSlug;


        var shopId = await CreateShopAsync(client, brand);
        var (productId1, _) = await CreateProductAsync(client, brand, price: 3.50m, name: "Frietje");
        var (productId2, _) = await CreateProductAsync(client, brand, price: 1.20m, name: "Saus");

        var request = new
        {
            OrderType = "Pickup",
            PaymentMethod = "CashAtPickup",
            CustomerFirstName = "Multi",
            CustomerLastName = "Items",
            CustomerEmail = "multi@example.com",
            CustomerPhone = "+32470000005",
            Items = new[]
            {
                new { ProductId = productId1, Quantity = 1, SelectedModifierIds = Array.Empty<Guid>() },
                new { ProductId = productId2, Quantity = 2, SelectedModifierIds = Array.Empty<Guid>() }
            }
        };

        var response = await client.PostAsJsonAsync(OrdersUrl(brand, shopId), request);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
        await Assert.That(order).IsNotNull();
        await Assert.That(order!.Items.Count).IsEqualTo(2);

        // Total = 3.50 * 1 + 1.20 * 2 = 5.90
        await Assert.That(order.SubtotalGross).IsEqualTo(5.90m);
        await Assert.That(order.TotalGross).IsEqualTo(5.90m);
    }

    // ── OrderNumber uniqueness ────────────────────────────────────────────────

    [Test]
    public async Task PlaceOrder_TwoOrders_HaveDifferentOrderNumbers()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.AlphaSlug;


        var shopId = await CreateShopAsync(client, brand);
        var (productId, _) = await CreateProductAsync(client, brand, price: 2.00m, name: "Knakworst");

        var requestBody = new
        {
            OrderType = "Pickup",
            PaymentMethod = "CashAtPickup",
            CustomerFirstName = "Order",
            CustomerLastName = "Uniqueness",
            CustomerEmail = "unique@example.com",
            CustomerPhone = "+32470000006",
            Items = new[]
            {
                new { ProductId = productId, Quantity = 1, SelectedModifierIds = Array.Empty<Guid>() }
            }
        };

        var response1 = await client.PostAsJsonAsync(OrdersUrl(brand, shopId), requestBody);
        var response2 = await client.PostAsJsonAsync(OrdersUrl(brand, shopId), requestBody);

        await Assert.That(response1.StatusCode).IsEqualTo(HttpStatusCode.Created);
        await Assert.That(response2.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var order1 = await response1.Content.ReadFromJsonAsync<OrderResponse>();
        var order2 = await response2.Content.ReadFromJsonAsync<OrderResponse>();

        await Assert.That(order1).IsNotNull();
        await Assert.That(order2).IsNotNull();
        await Assert.That(order1!.OrderNumber).IsNotEqualTo(order2!.OrderNumber);
        await Assert.That(order1.Id).IsNotEqualTo(order2.Id);
    }

    // ── Error cases ───────────────────────────────────────────────────────────

    [Test]
    public async Task PlaceOrder_UnknownProductId_Returns400()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.AlphaSlug;


        var shopId = await CreateShopAsync(client, brand);

        var request = new
        {
            OrderType = "Pickup",
            PaymentMethod = "CashAtPickup",
            CustomerFirstName = "Unknown",
            CustomerLastName = "Product",
            CustomerEmail = "unknown@example.com",
            CustomerPhone = "+32470000007",
            Items = new[]
            {
                new { ProductId = Guid.NewGuid(), Quantity = 1, SelectedModifierIds = Array.Empty<Guid>() }
            }
        };

        var response = await client.PostAsJsonAsync(OrdersUrl(brand, shopId), request);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task PlaceOrder_EmptyItems_Returns400()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.AlphaSlug;


        var shopId = await CreateShopAsync(client, brand);

        var request = new
        {
            OrderType = "Pickup",
            PaymentMethod = "CashAtPickup",
            CustomerFirstName = "Empty",
            CustomerLastName = "Items",
            CustomerEmail = "empty@example.com",
            CustomerPhone = "+32470000008",
            Items = Array.Empty<object>()
        };

        var response = await client.PostAsJsonAsync(OrdersUrl(brand, shopId), request);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task PlaceOrder_InvalidOrderType_Returns400()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.AlphaSlug;


        var shopId = await CreateShopAsync(client, brand);
        var (productId, _) = await CreateProductAsync(client, brand, price: 2.00m, name: "Wafels");

        var request = new
        {
            OrderType = "DineIn",  // Not a valid OrderType value
            PaymentMethod = "CashAtPickup",
            CustomerFirstName = "Invalid",
            CustomerLastName = "Type",
            CustomerEmail = "invalid@example.com",
            CustomerPhone = "+32470000009",
            Items = new[]
            {
                new { ProductId = productId, Quantity = 1, SelectedModifierIds = Array.Empty<Guid>() }
            }
        };

        var response = await client.PostAsJsonAsync(OrdersUrl(brand, shopId), request);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task PlaceOrder_ZeroQuantity_Returns400()
    {
        var client = CreateClient();
        var brand = IntegrationTestBase.AlphaSlug;


        var shopId = await CreateShopAsync(client, brand);
        var (productId, _) = await CreateProductAsync(client, brand, price: 2.00m, name: "Kroket");

        var request = new
        {
            OrderType = "Pickup",
            PaymentMethod = "CashAtPickup",
            CustomerFirstName = "Zero",
            CustomerLastName = "Qty",
            CustomerEmail = "zero@example.com",
            CustomerPhone = "+32470000010",
            Items = new[]
            {
                new { ProductId = productId, Quantity = 0, SelectedModifierIds = Array.Empty<Guid>() }
            }
        };

        var response = await client.PostAsJsonAsync(OrdersUrl(brand, shopId), request);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task PlaceOrder_NonExistentBrand_Returns404()
    {
        var client = CreateClient();

        var request = new
        {
            OrderType = "Pickup",
            PaymentMethod = "CashAtPickup",
            CustomerFirstName = "Non",
            CustomerLastName = "Existent",
            CustomerEmail = "nonexistent@example.com",
            CustomerPhone = "+32470000011",
            Items = new[]
            {
                new { ProductId = Guid.NewGuid(), Quantity = 1, SelectedModifierIds = Array.Empty<Guid>() }
            }
        };

        // BrandContextMiddleware returns 404 for unknown brands
        var response = await client.PostAsJsonAsync(
            $"/api/brands/non-existent-brand/shops/{Guid.NewGuid()}/orders",
            request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    // ── Denormalisation check ─────────────────────────────────────────────────

    [Test]
    public async Task PlaceOrder_DenormalisesProductNameAtOrderTime()
    {
        // Verifies that the product name is captured on the order item at creation time.
        var client = CreateClient();
        var brand = IntegrationTestBase.AlphaSlug;


        var shopId = await CreateShopAsync(client, brand);
        var (productId, _) = await CreateProductAsync(client, brand, price: 2.50m, name: "Oorspronkelijke Naam");

        var request = new
        {
            OrderType = "Pickup",
            PaymentMethod = "CashAtPickup",
            CustomerFirstName = "Denorm",
            CustomerLastName = "Test",
            CustomerEmail = "denorm@example.com",
            CustomerPhone = "+32470000012",
            Items = new[]
            {
                new { ProductId = productId, Quantity = 1, SelectedModifierIds = Array.Empty<Guid>() }
            }
        };

        var response = await client.PostAsJsonAsync(OrdersUrl(brand, shopId), request);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
        await Assert.That(order).IsNotNull();
        await Assert.That(order!.Items[0].ProductName).IsEqualTo("Oorspronkelijke Naam");
    }

    // ── Opening lifecycle status ──────────────────────────────────────────────

    [Test]
    public async Task PlaceOrder_StatusIsOpeningLifecycleStatus()
    {
        // The order should be in the "Placed" status (opening status = lowest SortOrder).
        var client = CreateClient();
        var brand = IntegrationTestBase.BetaSlug;


        var shopId = await CreateShopAsync(client, brand);
        var (productId, _) = await CreateProductAsync(client, brand, price: 2.00m, name: "Bitterballen");

        var request = new
        {
            OrderType = "Pickup",
            PaymentMethod = "CashAtPickup",
            CustomerFirstName = "Status",
            CustomerLastName = "Check",
            CustomerEmail = "status@example.com",
            CustomerPhone = "+32470000013",
            Items = new[]
            {
                new { ProductId = productId, Quantity = 1, SelectedModifierIds = Array.Empty<Guid>() }
            }
        };

        var response = await client.PostAsJsonAsync(OrdersUrl(brand, shopId), request);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
        await Assert.That(order).IsNotNull();
        // Default lifecycle's opening status is "Placed" (SortOrder = 0)
        await Assert.That(order!.StatusName).IsEqualTo("Placed");
    }

    // ── VAT on modifier price adjustments ────────────────────────────────────

    /// <summary>
    /// Creates a modifier group with a single modifier and returns its modifier ID.
    /// </summary>
    private async Task<Guid> CreateModifierWithPriceAsync(
        HttpClient client,
        string brandSlug,
        decimal priceAdjustment,
        string modifierName = "Extra saus")
    {
        var request = new
        {
            Translations = new[] { new { LanguageCode = "nl", Name = "Extras" } },
            Modifiers = new[]
            {
                new
                {
                    PriceAdjustment = priceAdjustment,
                    SortOrder = 0,
                    Translations = new[] { new { LanguageCode = "nl", Name = modifierName } }
                }
            }
        };

        var response = await client.PostAsJsonAsync(
            $"/api/brands/{brandSlug}/modifier-groups", request);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var group = await response.Content.ReadFromJsonAsync<ModifierGroupResponse>();
        await Assert.That(group).IsNotNull();
        return group!.Modifiers[0].Id;
    }

    [Test]
    public async Task PlaceOrder_WithModifier_VatAppliedToCombinedPrice()
    {
        // Verifies that modifier price adjustments are included in the VAT decomposition.
        // Product base price = 3.00, modifier price adjustment = 0.50, combined gross = 3.50.
        // At 6% VAT (Pickup): net = Round(3.50 / 1.06, 2) = 3.30, vat = 0.20.
        // The UnitGrossPrice stored on the item must be 3.50, NOT 3.00.
        var client = CreateClient();
        var brand = IntegrationTestBase.GammaSlug;


        var shopId = await CreateShopAsync(client, brand);
        var (productId, _) = await CreateProductAsync(client, brand, price: 3.00m, name: "Frietje");
        var modifierId = await CreateModifierWithPriceAsync(client, brand, priceAdjustment: 0.50m, modifierName: "Extra groot");

        var request = new
        {
            OrderType = "Pickup",
            PaymentMethod = "CashAtPickup",
            CustomerFirstName = "Modifier",
            CustomerLastName = "Test",
            CustomerEmail = "modifier@example.com",
            CustomerPhone = "+32470000014",
            Items = new[]
            {
                new { ProductId = productId, Quantity = 2, SelectedModifierIds = new[] { modifierId } }
            }
        };

        var response = await client.PostAsJsonAsync(OrdersUrl(brand, shopId), request);
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
        await Assert.That(order).IsNotNull();
        await Assert.That(order!.VatRatePercent).IsEqualTo(6m);

        var item = order.Items[0];
        await Assert.That(item.SelectedModifiers.Count).IsEqualTo(1);

        // Combined gross = 3.00 (base) + 0.50 (modifier) = 3.50
        await Assert.That(item.UnitGrossPrice).IsEqualTo(3.50m);

        // Net = Round(3.50 / 1.06, 2, AwayFromZero) = 3.30, VAT = 3.50 - 3.30 = 0.20
        await Assert.That(item.UnitNetPrice).IsEqualTo(3.30m);
        await Assert.That(item.UnitVatAmount).IsEqualTo(0.20m);

        // LineTotal = combined unit gross × quantity = 3.50 × 2 = 7.00
        await Assert.That(item.LineTotal).IsEqualTo(7.00m);

        // Order totals
        await Assert.That(order.SubtotalGross).IsEqualTo(7.00m);
        await Assert.That(order.TotalVatAmount).IsEqualTo(0.40m); // 0.20 * 2
        await Assert.That(order.TotalNet).IsEqualTo(6.60m);       // 7.00 - 0.40
        await Assert.That(order.TotalGross).IsEqualTo(7.00m);

        // Modifier PriceAdjustment is still recorded individually for display
        await Assert.That(item.SelectedModifiers[0].PriceAdjustment).IsEqualTo(0.50m);
    }

    // ── PaymentMethod ─────────────────────────────────────────────────────────

    [Test]
    public async Task PlaceOrder_WithCreditCard_StoresPaymentMethod()
    {
        // Verifies that a non-default payment method is captured on the order and returned in the response.
        var client = CreateClient();
        var brand = IntegrationTestBase.AlphaSlug;


        var shopId = await CreateShopAsync(client, brand);
        var (productId, _) = await CreateProductAsync(client, brand, price: 4.00m, name: "Frietje XL");

        var request = new
        {
            OrderType = "Pickup",
            PaymentMethod = "CreditCard",
            CustomerFirstName = "Credit",
            CustomerLastName = "Card",
            CustomerEmail = "credit@example.com",
            CustomerPhone = "+32470000015",
            Items = new[]
            {
                new { ProductId = productId, Quantity = 1, SelectedModifierIds = Array.Empty<Guid>() }
            }
        };

        var response = await client.PostAsJsonAsync(OrdersUrl(brand, shopId), request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
        await Assert.That(order).IsNotNull();
        await Assert.That(order!.PaymentMethod).IsEqualTo("CreditCard");
    }

    // ── Guest contact fields (US-FP-017) ─────────────────────────────────────

    [Test]
    public async Task PlaceOrder_WithEmailAndPhone_PersistsAndReturnsContactFields()
    {
        // Verifies that optional CustomerEmail and CustomerPhone are stored and returned.
        var client = CreateClient();
        var brand = IntegrationTestBase.AlphaSlug;

        var shopId = await CreateShopAsync(client, brand);
        var (productId, _) = await CreateProductAsync(client, brand, price: 2.50m, name: "Friet met email");

        var request = new
        {
            OrderType = "Pickup",
            PaymentMethod = "CashAtPickup",
            CustomerFirstName = "Lieselot",
            CustomerLastName = "Pieters",
            CustomerEmail = "lieselot@example.com",
            CustomerPhone = "+32 478 12 34 56",
            Items = new[]
            {
                new { ProductId = productId, Quantity = 1, SelectedModifierIds = Array.Empty<Guid>() }
            }
        };

        var response = await client.PostAsJsonAsync(OrdersUrl(brand, shopId), request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var order = await response.Content.ReadFromJsonAsync<OrderResponse>();
        await Assert.That(order).IsNotNull();
        await Assert.That(order!.CustomerName).IsEqualTo("Lieselot Pieters");
        await Assert.That(order.CustomerEmail).IsEqualTo("lieselot@example.com");
        await Assert.That(order.CustomerPhone).IsEqualTo("+32 478 12 34 56");
    }

    [Test]
    public async Task PlaceOrder_WithoutContactFields_Returns400()
    {
        // US-FP-051: every online order must supply all four contact fields (first, last, email, phone)
        // so the digital receipt can be delivered. Omitting them returns 400.
        var client = CreateClient();
        var brand = IntegrationTestBase.BetaSlug;

        var shopId = await CreateShopAsync(client, brand);
        var (productId, _) = await CreateProductAsync(client, brand, price: 3.00m, name: "Friet zonder contact");

        var request = new
        {
            OrderType = "Pickup",
            PaymentMethod = "CashAtPickup",
            // All four contact fields intentionally omitted
            Items = new[]
            {
                new { ProductId = productId, Quantity = 1, SelectedModifierIds = Array.Empty<Guid>() }
            }
        };

        var response = await client.PostAsJsonAsync(OrdersUrl(brand, shopId), request);

        // Contact fields are required for online orders since US-FP-051
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task PlaceOrder_WithInvalidEmail_Returns400()
    {
        // Verifies that a malformed email address is rejected by the FluentValidation rule.
        var client = CreateClient();
        var brand = IntegrationTestBase.GammaSlug;

        var shopId = await CreateShopAsync(client, brand);
        var (productId, _) = await CreateProductAsync(client, brand, price: 2.00m, name: "Friet ongeldig email");

        var request = new
        {
            OrderType = "Pickup",
            PaymentMethod = "CashAtPickup",
            CustomerFirstName = "Invalid",
            CustomerLastName = "Email",
            CustomerEmail = "not-a-valid-email",
            CustomerPhone = "+32470000016",
            Items = new[]
            {
                new { ProductId = productId, Quantity = 1, SelectedModifierIds = Array.Empty<Guid>() }
            }
        };

        var response = await client.PostAsJsonAsync(OrdersUrl(brand, shopId), request);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }
}

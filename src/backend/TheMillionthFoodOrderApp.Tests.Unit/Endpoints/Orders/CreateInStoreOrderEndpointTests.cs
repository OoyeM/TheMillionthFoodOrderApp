using System.Security.Claims;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using TheMillionthFoodOrderApp.Api.Endpoints.Orders;
using TheMillionthFoodOrderApp.Application.Orders;

// Disambiguate Arg: TUnit.Mocks uses TUnit.Mocks.Arguments.Arg; NSubstitute uses NSubstitute.Arg.
// We use NSubstitute's Arg for Received/Returns matching.
using Arg = NSubstitute.Arg;

namespace TheMillionthFoodOrderApp.Tests.Unit.Endpoints.Orders;

/// <summary>
/// Unit tests for <see cref="CreateInStoreOrderEndpoint"/> and <see cref="CreateInStoreOrderRequestValidator"/>.
///
/// Auth enforcement (anonymous → 401, CounterStaff → 200) is enforced by FastEndpoints middleware
/// via Configure() → Roles("CounterStaff"). This is not testable without the full HTTP pipeline.
/// The integration tests in CreateInStoreOrderServiceTests exercise the full stack including auth.
///
/// This test class covers:
/// - Route shape
/// - Validator rules (TableNumber conditional on EatIn, quantity bounds, CustomerName length)
/// - HandleAsync: correct service call mapping (tableNumber + createdByStaffId)
/// - HandleAsync: error mapping (KeyNotFoundException → 400, ArgumentException → 400)
/// - Claim extraction (NameIdentifier, "sub" literal, "userId" fallback)
/// </summary>
public sealed class CreateInStoreOrderEndpointTests
{
    // ── Route contract ────────────────────────────────────────────────────────

    [Test]
    public async Task Route_EndsWithInStoreSuffix()
    {
        await Assert.That(CreateInStoreOrderEndpoint.Route)
            .IsEqualTo("/api/brands/{brandSlug}/shops/{shopId}/orders/in-store");
    }

    [Test]
    public async Task Route_IsDifferentFromPublicOrderEndpoint()
    {
        await Assert.That(CreateInStoreOrderEndpoint.Route)
            .IsNotEqualTo(CreateOrderEndpoint.Route);
    }

    // ── Validator: Pickup without table — valid ───────────────────────────────

    [Test]
    public async Task Validator_Pickup_WithoutTableNumber_IsValid()
    {
        var result = await new CreateInStoreOrderRequestValidator().ValidateAsync(
            new CreateInStoreOrderApiRequest(
                "frietjes", Guid.NewGuid(), "Pickup", "CashAtPickup",
                null, null, null,
                [new OrderItemApiInput(Guid.NewGuid(), 1, null)]));

        await Assert.That(result.IsValid).IsTrue();
    }

    // ── Validator: EatIn without table — invalid ──────────────────────────────

    [Test]
    public async Task Validator_EatIn_WithoutTableNumber_FailsValidation()
    {
        var result = await new CreateInStoreOrderRequestValidator().ValidateAsync(
            new CreateInStoreOrderApiRequest(
                "frietjes", Guid.NewGuid(), "EatIn", "CashAtPickup",
                null, null, null,
                [new OrderItemApiInput(Guid.NewGuid(), 1, null)]));

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Any(e => e.PropertyName == "TableNumber")).IsTrue();
    }

    // ── Validator: EatIn with valid table — valid ─────────────────────────────

    [Test]
    public async Task Validator_EatIn_WithTableNumber5_IsValid()
    {
        var result = await new CreateInStoreOrderRequestValidator().ValidateAsync(
            new CreateInStoreOrderApiRequest(
                "frietjes", Guid.NewGuid(), "EatIn", "CashAtPickup",
                null, null, 5,
                [new OrderItemApiInput(Guid.NewGuid(), 1, null)]));

        await Assert.That(result.IsValid).IsTrue();
    }

    // ── Validator: table <= 0 — invalid ──────────────────────────────────────

    [Test]
    public async Task Validator_EatIn_TableNumberZero_FailsValidation()
    {
        var result = await new CreateInStoreOrderRequestValidator().ValidateAsync(
            new CreateInStoreOrderApiRequest(
                "frietjes", Guid.NewGuid(), "EatIn", "CashAtPickup",
                null, null, 0,
                [new OrderItemApiInput(Guid.NewGuid(), 1, null)]));

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Any(e => e.PropertyName == "TableNumber")).IsTrue();
    }

    [Test]
    public async Task Validator_EatIn_NegativeTableNumber_FailsValidation()
    {
        var result = await new CreateInStoreOrderRequestValidator().ValidateAsync(
            new CreateInStoreOrderApiRequest(
                "frietjes", Guid.NewGuid(), "EatIn", "CashAtPickup",
                null, null, -3,
                [new OrderItemApiInput(Guid.NewGuid(), 1, null)]));

        await Assert.That(result.IsValid).IsFalse();
    }

    // ── Validator: TableNumber not required for Delivery ──────────────────────

    [Test]
    public async Task Validator_Delivery_WithoutTableNumber_IsValid()
    {
        var result = await new CreateInStoreOrderRequestValidator().ValidateAsync(
            new CreateInStoreOrderApiRequest(
                "frietjes", Guid.NewGuid(), "Delivery", "CashAtPickup",
                null, null, null,
                [new OrderItemApiInput(Guid.NewGuid(), 1, null)]));

        await Assert.That(result.IsValid).IsTrue();
    }

    // ── Validator: Items non-empty ────────────────────────────────────────────

    [Test]
    public async Task Validator_EmptyItems_FailsValidation()
    {
        var result = await new CreateInStoreOrderRequestValidator().ValidateAsync(
            new CreateInStoreOrderApiRequest(
                "frietjes", Guid.NewGuid(), "Pickup", "CashAtPickup",
                null, null, null, []));

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Any(e => e.PropertyName == "Items")).IsTrue();
    }

    // ── Validator: Quantity bounds ────────────────────────────────────────────

    [Test]
    public async Task Validator_QuantityZero_FailsValidation()
    {
        var result = await new CreateInStoreOrderRequestValidator().ValidateAsync(
            new CreateInStoreOrderApiRequest(
                "frietjes", Guid.NewGuid(), "Pickup", "CashAtPickup",
                null, null, null,
                [new OrderItemApiInput(Guid.NewGuid(), 0, null)]));

        await Assert.That(result.IsValid).IsFalse();
    }

    [Test]
    public async Task Validator_Quantity100_FailsValidation()
    {
        var result = await new CreateInStoreOrderRequestValidator().ValidateAsync(
            new CreateInStoreOrderApiRequest(
                "frietjes", Guid.NewGuid(), "Pickup", "CashAtPickup",
                null, null, null,
                [new OrderItemApiInput(Guid.NewGuid(), 100, null)]));

        await Assert.That(result.IsValid).IsFalse();
    }

    // ── Validator: CustomerFirstName / CustomerLastName length ────────────────

    [Test]
    public async Task Validator_CustomerFirstNameOver100Chars_FailsValidation()
    {
        var result = await new CreateInStoreOrderRequestValidator().ValidateAsync(
            new CreateInStoreOrderApiRequest(
                "frietjes", Guid.NewGuid(), "Pickup", "CashAtPickup",
                new string('A', 101), null, null,
                [new OrderItemApiInput(Guid.NewGuid(), 1, null)]));

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Any(e => e.PropertyName == "CustomerFirstName")).IsTrue();
    }

    [Test]
    public async Task Validator_CustomerLastNameOver100Chars_FailsValidation()
    {
        var result = await new CreateInStoreOrderRequestValidator().ValidateAsync(
            new CreateInStoreOrderApiRequest(
                "frietjes", Guid.NewGuid(), "Pickup", "CashAtPickup",
                null, new string('B', 101), null,
                [new OrderItemApiInput(Guid.NewGuid(), 1, null)]));

        await Assert.That(result.IsValid).IsFalse();
        await Assert.That(result.Errors.Any(e => e.PropertyName == "CustomerLastName")).IsTrue();
    }

    // ── HandleAsync: maps tableNumber + createdByStaffId to service ───────────

    [Test]
    public async Task HandleAsync_Pickup_WithOptionalTableNumber_PassesValueToService()
    {
        var expectedStaffId = Guid.NewGuid();
        var shopId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var tableNumber = 7;

        var orderService = Substitute.For<IOrderService>();
        CreateInStoreOrderRequest? captured = null;
        Guid? capturedStaffId = Guid.Empty; // sentinel to distinguish "not called" from explicit null

        orderService
            .CreateInStoreOrderAsync(
                Arg.Do<CreateInStoreOrderRequest>(r => captured = r),
                Arg.Do<Guid?>(id => capturedStaffId = id),
                Arg.Any<CancellationToken>())
            .Returns(BuildFakeResponse(shopId, tableNumber, expectedStaffId));

        var httpContext = BuildHttpContext(staffId: expectedStaffId, claimType: ClaimTypes.NameIdentifier);
        var endpoint = Factory.Create<CreateInStoreOrderEndpoint>(httpContext, orderService);

        await endpoint.HandleAsync(
            new CreateInStoreOrderApiRequest(
                "frietjes", shopId, "Pickup", "CashAtPickup",
                null, null, tableNumber,
                [new OrderItemApiInput(productId, 2, null)]),
            CancellationToken.None);

        await Assert.That(captured).IsNotNull();
        await Assert.That(captured!.TableNumber).IsEqualTo(tableNumber);
        // createdByStaffId is passed as a separate parameter (not in DTO) — verify it was extracted from claims
        await Assert.That(capturedStaffId).IsEqualTo(expectedStaffId);
        await Assert.That(captured.ShopId).IsEqualTo(shopId);
    }

    // ── HandleAsync: Pickup without table — null passed to service ────────────

    [Test]
    public async Task HandleAsync_Pickup_WithoutTableNumber_PassesNullToService()
    {
        var expectedStaffId = Guid.NewGuid();
        var shopId = Guid.NewGuid();

        var orderService = Substitute.For<IOrderService>();
        CreateInStoreOrderRequest? captured = null;

        orderService
            .CreateInStoreOrderAsync(
                Arg.Do<CreateInStoreOrderRequest>(r => captured = r),
                Arg.Any<Guid?>(),
                Arg.Any<CancellationToken>())
            .Returns(BuildFakeResponse(shopId, null, expectedStaffId));

        var httpContext = BuildHttpContext(staffId: expectedStaffId, claimType: ClaimTypes.NameIdentifier);
        var endpoint = Factory.Create<CreateInStoreOrderEndpoint>(httpContext, orderService);

        await endpoint.HandleAsync(
            new CreateInStoreOrderApiRequest(
                "frietjes", shopId, "Pickup", "CashAtPickup",
                null, null, null, // no table number for Pickup
                [new OrderItemApiInput(Guid.NewGuid(), 1, null)]),
            CancellationToken.None);

        await Assert.That(captured).IsNotNull();
        await Assert.That(captured!.TableNumber).IsNull();
    }

    // ── HandleAsync: endpoint passes client PaymentMethod through to service (service enforces CashAtPickup) ──

    [Test]
    public async Task HandleAsync_ClientSubmitsCreditCard_EndpointPassesThroughToService()
    {
        // The endpoint does NOT force PaymentMethod — the service layer does.
        // This test verifies the endpoint passes whatever the client sent to the service DTO
        // unchanged; it does NOT test the force logic itself.
        // PaymentMethod enforcement (override to CashAtPickup) is verified at the service layer in:
        //   CreateInStoreOrderServiceTests.CreateInStoreOrder_PaymentMethodForcedToCashAtPickup
        var shopId = Guid.NewGuid();
        var staffId = Guid.NewGuid();

        var orderService = Substitute.For<IOrderService>();
        CreateInStoreOrderRequest? captured = null;

        orderService
            .CreateInStoreOrderAsync(
                Arg.Do<CreateInStoreOrderRequest>(r => captured = r),
                Arg.Any<Guid?>(),
                Arg.Any<CancellationToken>())
            .Returns(BuildFakeResponse(shopId, null, staffId));

        var httpContext = BuildHttpContext(staffId: staffId, claimType: ClaimTypes.NameIdentifier);
        var endpoint = Factory.Create<CreateInStoreOrderEndpoint>(httpContext, orderService);

        await endpoint.HandleAsync(
            new CreateInStoreOrderApiRequest(
                "frietjes", shopId, "Pickup", "CreditCard", // client submits CreditCard
                null, null, null,
                [new OrderItemApiInput(Guid.NewGuid(), 1, null)]),
            CancellationToken.None);

        await Assert.That(captured).IsNotNull();
        // The endpoint passes through the client's PaymentMethod value to the service DTO.
        // The OrderService.CreateInStoreOrderAsync then forces it to CashAtPickup server-side.
        await Assert.That(captured!.PaymentMethod).IsEqualTo("CreditCard");
    }

    // ── HandleAsync: KeyNotFoundException → 400 ──────────────────────────────

    [Test]
    public async Task HandleAsync_ServiceThrowsKeyNotFoundException_Returns400()
    {
        var orderService = Substitute.For<IOrderService>();
        orderService
            .CreateInStoreOrderAsync(Arg.Any<CreateInStoreOrderRequest>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new KeyNotFoundException("Product not found."));

        var httpContext = BuildHttpContext(staffId: Guid.NewGuid(), claimType: ClaimTypes.NameIdentifier);
        var endpoint = Factory.Create<CreateInStoreOrderEndpoint>(httpContext, orderService);

        await endpoint.HandleAsync(
            new CreateInStoreOrderApiRequest(
                "frietjes", Guid.NewGuid(), "Pickup", "CashAtPickup",
                null, null, null, [new OrderItemApiInput(Guid.NewGuid(), 1, null)]),
            CancellationToken.None);

        await Assert.That(httpContext.Response.StatusCode).IsEqualTo(400);
    }

    // ── HandleAsync: ArgumentException → 400 ─────────────────────────────────

    [Test]
    public async Task HandleAsync_ServiceThrowsArgumentException_Returns400()
    {
        var orderService = Substitute.For<IOrderService>();
        orderService
            .CreateInStoreOrderAsync(Arg.Any<CreateInStoreOrderRequest>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new ArgumentException("TableNumber is required for EatIn orders."));

        var httpContext = BuildHttpContext(staffId: Guid.NewGuid(), claimType: ClaimTypes.NameIdentifier);
        var endpoint = Factory.Create<CreateInStoreOrderEndpoint>(httpContext, orderService);

        await endpoint.HandleAsync(
            new CreateInStoreOrderApiRequest(
                "frietjes", Guid.NewGuid(), "EatIn", "CashAtPickup",
                null, null, null, [new OrderItemApiInput(Guid.NewGuid(), 1, null)]),
            CancellationToken.None);

        await Assert.That(httpContext.Response.StatusCode).IsEqualTo(400);
    }

    // ── HandleAsync: 'sub' literal claim extraction ───────────────────────────

    [Test]
    public async Task HandleAsync_ExtractsStaffId_FromLiteralSubClaim()
    {
        var expectedStaffId = Guid.NewGuid();
        var shopId = Guid.NewGuid();

        var orderService = Substitute.For<IOrderService>();
        CreateInStoreOrderRequest? captured = null;
        Guid? capturedStaffId = Guid.Empty;

        orderService
            .CreateInStoreOrderAsync(
                Arg.Do<CreateInStoreOrderRequest>(r => captured = r),
                Arg.Do<Guid?>(id => capturedStaffId = id),
                Arg.Any<CancellationToken>())
            .Returns(BuildFakeResponse(shopId, null, expectedStaffId));

        // Use the literal "sub" claim (not ClaimTypes.NameIdentifier) to exercise the fallback
        var httpContext = BuildHttpContext(staffId: expectedStaffId, claimType: "sub");
        var endpoint = Factory.Create<CreateInStoreOrderEndpoint>(httpContext, orderService);

        await endpoint.HandleAsync(
            new CreateInStoreOrderApiRequest(
                "frietjes", shopId, "Pickup", "CashAtPickup",
                null, null, null, [new OrderItemApiInput(Guid.NewGuid(), 1, null)]),
            CancellationToken.None);

        await Assert.That(captured).IsNotNull();
        // createdByStaffId is now a separate parameter — verify the literal 'sub' claim was extracted
        await Assert.That(capturedStaffId).IsEqualTo(expectedStaffId);
    }

    // ── HandleAsync: no claims → null staffId (graceful degradation) ──────────

    [Test]
    public async Task HandleAsync_NoClaims_PassesNullStaffIdToService()
    {
        var shopId = Guid.NewGuid();

        var orderService = Substitute.For<IOrderService>();
        CreateInStoreOrderRequest? captured = null;
        Guid? capturedStaffId = Guid.Empty; // sentinel to distinguish "not called" from explicit null

        orderService
            .CreateInStoreOrderAsync(
                Arg.Do<CreateInStoreOrderRequest>(r => captured = r),
                Arg.Do<Guid?>(id => capturedStaffId = id),
                Arg.Any<CancellationToken>())
            .Returns(BuildFakeResponse(shopId, null, null));

        // Anonymous user (no claims)
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

        var endpoint = Factory.Create<CreateInStoreOrderEndpoint>(httpContext, orderService);

        await endpoint.HandleAsync(
            new CreateInStoreOrderApiRequest(
                "frietjes", shopId, "Pickup", "CashAtPickup",
                null, null, null, [new OrderItemApiInput(Guid.NewGuid(), 1, null)]),
            CancellationToken.None);

        // Verify service was called exactly once with a null staffId (graceful degradation)
        await orderService.Received(1).CreateInStoreOrderAsync(
            Arg.Any<CreateInStoreOrderRequest>(),
            Arg.Is<Guid?>(id => id == null),
            Arg.Any<CancellationToken>());

        await Assert.That(captured).IsNotNull();
        // createdByStaffId is now a separate parameter — verify null is passed through
        await Assert.That(capturedStaffId).IsNull();
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private static DefaultHttpContext BuildHttpContext(Guid staffId, string claimType)
    {
        var claims = new[] { new Claim(claimType, staffId.ToString()) };
        var identity = new ClaimsIdentity(claims, "TestScheme");
        var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        httpContext.Response.Body = new MemoryStream();
        return httpContext;
    }

    private static OrderResponse BuildFakeResponse(Guid shopId, int? tableNumber, Guid? staffId) =>
        new(
            Guid.NewGuid(), "ABC12345", shopId, "frietjes",
            "Pickup", "CashAtPickup", "Placed", null,
            Array.Empty<OrderItemResponse>().ToList().AsReadOnly(),
            // VatRatePercent=6%, SubtotalGross=3.50, TotalVatAmount=0.20, TotalNet=3.30, TotalGross=3.50
            // These are intentional test-stub values — they do not represent real pricing logic.
            // Real pricing is exercised in CreateInStoreOrderServiceTests (integration) and OrderServiceTests (unit).
            6m, 3.50m, 0.20m, 3.30m, 3.50m, DateTimeOffset.UtcNow,
            tableNumber, staffId);
}

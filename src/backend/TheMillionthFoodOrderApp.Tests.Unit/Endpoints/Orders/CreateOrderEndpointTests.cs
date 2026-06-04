using System.Security.Claims;
using FastEndpoints;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using TheMillionthFoodOrderApp.Api.Endpoints.Orders;
using TheMillionthFoodOrderApp.Application.Orders;

// Disambiguate NSubstitute.Arg from TUnit.Mocks.Arguments.Arg
using Arg = NSubstitute.Arg;

namespace TheMillionthFoodOrderApp.Tests.Unit.Endpoints.Orders;

/// <summary>
/// Unit tests for <see cref="CreateOrderEndpoint"/> (US-FP-051).
///
/// Covers:
/// (a) Anonymous request missing a required contact field (phone) → 400, service NOT called.
/// (b) Anonymous request with all four contact fields → service Received(1) with correct values.
/// (c) Authenticated principal with OIDC claims → service Received(1) with claim values
///     (body contact fields are ignored when claims are present).
/// </summary>
public sealed class CreateOrderEndpointTests
{
    // ── Helper: build a minimal valid request body ────────────────────────────

    private static CreateOrderApiRequest BuildRequest(
        string? firstName = "Jan",
        string? lastName = "Janssen",
        string? email = "jan@example.com",
        string? phone = "+32470000001") =>
        new(
            BrandSlug: "frietjes",
            ShopId: Guid.NewGuid(),
            OrderType: "Pickup",
            PaymentMethod: "CashAtPickup",
            CustomerFirstName: firstName,
            CustomerLastName: lastName,
            Items: [new OrderItemApiInput(Guid.NewGuid(), 1, null)],
            CustomerEmail: email,
            CustomerPhone: phone);

    private static DefaultHttpContext BuildAnonymousContext()
    {
        var ctx = new DefaultHttpContext();
        ctx.Response.Body = new MemoryStream();
        ctx.User = new ClaimsPrincipal(new ClaimsIdentity()); // unauthenticated
        return ctx;
    }

    private static DefaultHttpContext BuildAuthenticatedContext(
        string givenName,
        string familyName,
        string email,
        string phone)
    {
        var claims = new[]
        {
            new Claim("given_name",    givenName),
            new Claim("family_name",   familyName),
            new Claim("email",         email),
            new Claim("phone_number",  phone),
        };
        var identity = new ClaimsIdentity(claims, "TestScheme");
        var ctx = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        ctx.Response.Body = new MemoryStream();
        return ctx;
    }

    private static OrderResponse BuildFakeResponse(Guid shopId) =>
        new(
            Id: Guid.NewGuid(),
            OrderNumber: "ORD-0001",
            ShopId: shopId,
            BrandSlug: "frietjes",
            OrderType: "Pickup",
            PaymentMethod: "CashAtPickup",
            StatusName: "Placed",
            CustomerName: "Jan Janssen",
            Items: new List<OrderItemResponse>().AsReadOnly(),
            VatRatePercent: 6m,
            SubtotalGross: 3.50m,
            TotalVatAmount: 0.20m,
            TotalNet: 3.30m,
            TotalGross: 3.50m,
            CreatedAt: DateTimeOffset.UtcNow);

    // ── (a) Missing phone → 400, service not called ───────────────────────────

    [Test]
    public async Task HandleAsync_AnonymousMissingPhone_Returns400WithPhoneFailure()
    {
        var orderService = Substitute.For<IOrderService>();
        var shopId = Guid.NewGuid();

        var ctx = BuildAnonymousContext();
        var endpoint = Factory.Create<CreateOrderEndpoint>(ctx, orderService);

        await endpoint.HandleAsync(
            BuildRequest(phone: null),    // phone omitted
            CancellationToken.None);

        // Must return 400
        await Assert.That(ctx.Response.StatusCode).IsEqualTo(400);

        // Service must NOT have been called
        await orderService.DidNotReceive().CreateOrderAsync(
            Arg.Any<CreateOrderRequest>(),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandleAsync_AnonymousMissingFirstName_Returns400()
    {
        var orderService = Substitute.For<IOrderService>();

        var ctx = BuildAnonymousContext();
        var endpoint = Factory.Create<CreateOrderEndpoint>(ctx, orderService);

        await endpoint.HandleAsync(
            BuildRequest(firstName: null),
            CancellationToken.None);

        await Assert.That(ctx.Response.StatusCode).IsEqualTo(400);
        await orderService.DidNotReceive().CreateOrderAsync(
            Arg.Any<CreateOrderRequest>(),
            Arg.Any<CancellationToken>());
    }

    // ── (b) Anonymous with all four fields → service called once ─────────────

    [Test]
    public async Task HandleAsync_AnonymousWithAllContactFields_CallsServiceOnce()
    {
        var shopId = Guid.NewGuid();
        var orderService = Substitute.For<IOrderService>();
        CreateOrderRequest? captured = null;

        orderService
            .CreateOrderAsync(
                Arg.Do<CreateOrderRequest>(r => captured = r),
                Arg.Any<CancellationToken>())
            .Returns(BuildFakeResponse(shopId));

        var ctx = BuildAnonymousContext();
        var endpoint = Factory.Create<CreateOrderEndpoint>(ctx, orderService);

        await endpoint.HandleAsync(
            new CreateOrderApiRequest(
                BrandSlug: "frietjes",
                ShopId: shopId,
                OrderType: "Pickup",
                PaymentMethod: "CashAtPickup",
                CustomerFirstName: "Jan",
                CustomerLastName: "Janssen",
                Items: [new OrderItemApiInput(Guid.NewGuid(), 1, null)],
                CustomerEmail: "jan@example.com",
                CustomerPhone: "+32470000001"),
            CancellationToken.None);

        await orderService.Received(1).CreateOrderAsync(
            Arg.Any<CreateOrderRequest>(),
            Arg.Any<CancellationToken>());

        await Assert.That(captured).IsNotNull();
        await Assert.That(captured!.CustomerFirstName).IsEqualTo("Jan");
        await Assert.That(captured.CustomerLastName).IsEqualTo("Janssen");
        await Assert.That(captured.CustomerEmail).IsEqualTo("jan@example.com");
        await Assert.That(captured.CustomerPhone).IsEqualTo("+32470000001");
    }

    // ── (c) Authenticated user: claim values forwarded, body ignored ──────────

    [Test]
    public async Task HandleAsync_AuthenticatedWithClaims_UsesClaimValues()
    {
        // The authenticated user carries OIDC claims: given_name, family_name, email, phone_number.
        // The body has different values — the endpoint must prefer the claims.
        var shopId = Guid.NewGuid();
        var orderService = Substitute.For<IOrderService>();
        CreateOrderRequest? captured = null;

        orderService
            .CreateOrderAsync(
                Arg.Do<CreateOrderRequest>(r => captured = r),
                Arg.Any<CancellationToken>())
            .Returns(BuildFakeResponse(shopId));

        var ctx = BuildAuthenticatedContext(
            givenName: "Test",
            familyName: "Customer",
            email: "customer@mock.local",
            phone: "+32470000004");

        var endpoint = Factory.Create<CreateOrderEndpoint>(ctx, orderService);

        // Body has different values — claims must win
        await endpoint.HandleAsync(
            new CreateOrderApiRequest(
                BrandSlug: "frietjes",
                ShopId: shopId,
                OrderType: "Pickup",
                PaymentMethod: "CashAtPickup",
                CustomerFirstName: "Body",
                CustomerLastName: "Override",
                Items: [new OrderItemApiInput(Guid.NewGuid(), 1, null)],
                CustomerEmail: "body@example.com",
                CustomerPhone: "+32000000000"),
            CancellationToken.None);

        await orderService.Received(1).CreateOrderAsync(
            Arg.Any<CreateOrderRequest>(),
            Arg.Any<CancellationToken>());

        await Assert.That(captured).IsNotNull();
        // Claim values must override body values
        await Assert.That(captured!.CustomerFirstName).IsEqualTo("Test");
        await Assert.That(captured.CustomerLastName).IsEqualTo("Customer");
        await Assert.That(captured.CustomerEmail).IsEqualTo("customer@mock.local");
        await Assert.That(captured.CustomerPhone).IsEqualTo("+32470000004");
    }

    // ── (d) Authenticated user with empty body — claims satisfy requirements ──

    [Test]
    public async Task HandleAsync_AuthenticatedWithEmptyBodyContactFields_UsesClaimsSuccessfully()
    {
        // An authenticated customer with an empty body (all contact fields null) should succeed
        // because the OIDC claims provide all four required values.
        var shopId = Guid.NewGuid();
        var orderService = Substitute.For<IOrderService>();

        orderService
            .CreateOrderAsync(Arg.Any<CreateOrderRequest>(), Arg.Any<CancellationToken>())
            .Returns(BuildFakeResponse(shopId));

        var ctx = BuildAuthenticatedContext(
            givenName: "Test",
            familyName: "Customer",
            email: "customer@mock.local",
            phone: "+32470000004");

        var endpoint = Factory.Create<CreateOrderEndpoint>(ctx, orderService);

        await endpoint.HandleAsync(
            new CreateOrderApiRequest(
                BrandSlug: "frietjes",
                ShopId: shopId,
                OrderType: "Pickup",
                PaymentMethod: "CashAtPickup",
                CustomerFirstName: null,
                CustomerLastName: null,
                Items: [new OrderItemApiInput(Guid.NewGuid(), 1, null)],
                CustomerEmail: null,
                CustomerPhone: null),
            CancellationToken.None);

        // Service must have been called — all four fields resolved from claims
        await orderService.Received(1).CreateOrderAsync(
            Arg.Any<CreateOrderRequest>(),
            Arg.Any<CancellationToken>());
    }
}

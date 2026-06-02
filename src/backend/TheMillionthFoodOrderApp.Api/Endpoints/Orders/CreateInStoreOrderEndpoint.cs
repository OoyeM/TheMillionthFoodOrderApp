using System.Security.Claims;
using FastEndpoints;
using FluentValidation;
using FluentValidation.Results;
using TheMillionthFoodOrderApp.Application.Orders;
using TheMillionthFoodOrderApp.Application.Orders.Dtos;
using TheMillionthFoodOrderApp.Domain.Orders;

namespace TheMillionthFoodOrderApp.Api.Endpoints.Orders;

public sealed record CreateInStoreOrderApiRequest(
    [property: RouteParam] string BrandSlug,
    [property: RouteParam] Guid ShopId,
    string OrderType,
    string PaymentMethod,
    string? CustomerName,
    int? TableNumber,
    List<OrderItemApiInput> Items);

public sealed class CreateInStoreOrderRequestValidator : Validator<CreateInStoreOrderApiRequest>
{
    public CreateInStoreOrderRequestValidator()
    {
        RuleFor(x => x.OrderType)
            .NotEmpty().WithMessage("OrderType is required.")
            .Must(v => Enum.TryParse<OrderType>(v, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed))
            .WithMessage($"OrderType must be one of: {string.Join(", ", Enum.GetNames<OrderType>())}.");

        RuleFor(x => x.PaymentMethod)
            .NotEmpty().WithMessage("PaymentMethod is required.")
            .Must(v => Enum.TryParse<Domain.Orders.PaymentMethod>(v, ignoreCase: true, out var parsed) && Enum.IsDefined(parsed))
            .WithMessage($"PaymentMethod must be one of: {string.Join(", ", Enum.GetNames<Domain.Orders.PaymentMethod>())}.");

        RuleFor(x => x.Items)
            .NotEmpty().WithMessage("At least one item is required.");

        RuleForEach(x => x.Items).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId)
                .NotEmpty().WithMessage("ProductId is required.");

            item.RuleFor(i => i.Quantity)
                .GreaterThan(0).WithMessage("Quantity must be greater than zero.")
                .LessThanOrEqualTo(99).WithMessage("Quantity cannot exceed 99.");
        });

        RuleFor(x => x.CustomerName)
            .MaximumLength(200)
            .When(x => x.CustomerName is not null);

        // TableNumber is required and must be > 0 only when OrderType is EatIn
        RuleFor(x => x.TableNumber)
            .NotNull().WithMessage("TableNumber is required for EatIn orders.")
            .GreaterThan(0).WithMessage("TableNumber must be greater than zero.")
            .When(x => string.Equals(x.OrderType, "EatIn", StringComparison.OrdinalIgnoreCase));
    }
}

/// <summary>
/// Creates a new in-store order on behalf of authenticated counter staff.
/// Route: POST /api/brands/{brandSlug}/shops/{shopId}/orders/in-store
///
/// Differences from the public <see cref="CreateOrderEndpoint"/>:
/// <list type="bullet">
///   <item><description>Requires the <c>CounterStaff</c> role — anonymous access is not permitted.</description></item>
///   <item><description>PaymentMethod is forced to <c>CashAtPickup</c> server-side.</description></item>
///   <item><description>TableNumber is required for EatIn orders.</description></item>
///   <item><description>CreatedByStaffId is extracted from the authenticated user's claims (never trusted from the client).</description></item>
/// </list>
/// </summary>
public sealed class CreateInStoreOrderEndpoint(IOrderService orderService)
    : Endpoint<CreateInStoreOrderApiRequest, OrderResponse>
{
    public const string Route = "/api/brands/{brandSlug}/shops/{shopId}/orders/in-store";

    public override void Configure()
    {
        Post(Route);
        Roles("CounterStaff");
        PreProcessor<BrandScopedPreProcessor<CreateInStoreOrderApiRequest>>();
        Summary(s =>
        {
            s.Summary = "Place a new in-store order (counter staff)";
            s.Description =
                "Creates a new in-store order for the specified shop by authenticated counter staff. " +
                "PaymentMethod is forced to CashAtPickup. " +
                "TableNumber is required for EatIn orders. " +
                "Staff identity is captured server-side from the authenticated user.";
            s.Response<OrderResponse>(201, "In-store order placed successfully.");
            s.Response(400, "Validation error or unknown product/modifier.");
            s.Response(401, "Authentication required.");
            s.Response(403, "Caller does not have the CounterStaff role.");
            s.Response(404, "Shop or brand not found.");
        });
    }

    public override async Task HandleAsync(CreateInStoreOrderApiRequest req, CancellationToken ct)
    {
        // Extract staff ID from the authenticated user.
        // Prefer the standard OIDC 'sub' claim (ClaimTypes.NameIdentifier maps to 'sub' when
        // MapInboundClaims=false, but some issuers send it under the literal "sub" key).
        // Fall back to a 'userId' claim as a secondary convention used by some identity providers.
        var staffIdString =
            HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier)  // maps to 'sub' via JwtBearer
            ?? HttpContext.User.FindFirstValue("sub")                    // literal 'sub' claim
            ?? HttpContext.User.FindFirstValue("userId");                // fallback convention

        Guid? createdByStaffId = null;
        if (staffIdString is not null && Guid.TryParse(staffIdString, out var parsedStaffId))
            createdByStaffId = parsedStaffId;

        try
        {
            var appRequest = new CreateInStoreOrderRequest(
                req.ShopId,
                req.BrandSlug,
                req.OrderType,
                req.PaymentMethod,
                req.CustomerName,
                req.TableNumber,
                req.Items
                    .Select(i => new OrderItemInput(
                        i.ProductId,
                        i.Quantity,
                        (i.SelectedModifierIds ?? []).AsReadOnly()))
                    .ToList()
                    .AsReadOnly());

            // createdByStaffId is passed as a separate explicit parameter so the service
            // never reads it from the DTO — it is always the server-extracted claim value.
            var response = await orderService.CreateInStoreOrderAsync(appRequest, createdByStaffId, ct);

            await HttpContext.Response.SendAsync(response, statusCode: 201, cancellation: ct);
        }
        catch (KeyNotFoundException ex)
        {
            var failures = new List<ValidationFailure> { new("items", ex.Message) };
            await HttpContext.Response.SendErrorsAsync(failures, statusCode: 400, cancellation: ct);
        }
        catch (ArgumentException ex)
        {
            var failures = new List<ValidationFailure> { new("request", ex.Message) };
            await HttpContext.Response.SendErrorsAsync(failures, statusCode: 400, cancellation: ct);
        }
        catch (InvalidOperationException ex)
        {
            var failures = new List<ValidationFailure> { new("request", ex.Message) };
            await HttpContext.Response.SendErrorsAsync(failures, statusCode: 400, cancellation: ct);
        }
    }
}

using FastEndpoints;
using FluentValidation;
using FluentValidation.Results;
using TheMillionthFoodOrderApp.Application.Orders;
using TheMillionthFoodOrderApp.Application.Orders.Dtos;
using TheMillionthFoodOrderApp.Domain.Orders;

namespace TheMillionthFoodOrderApp.Api.Endpoints.Orders;

public sealed record OrderItemApiInput(Guid ProductId, int Quantity, List<Guid>? SelectedModifierIds);

public sealed record CreateOrderApiRequest(
    [property: RouteParam] string BrandSlug,
    [property: RouteParam] Guid ShopId,
    string OrderType,
    string PaymentMethod,
    string? CustomerName,
    List<OrderItemApiInput> Items,
    string? TableNumber = null);

public sealed class CreateOrderRequestValidator : Validator<CreateOrderApiRequest>
{
    public CreateOrderRequestValidator()
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

        // TableNumber: optional globally, but required when order type is EatIn.
        // Alphanumeric labels (e.g. "T-12") are valid — max 20 chars.
        RuleFor(x => x.TableNumber)
            .MaximumLength(20)
            .When(x => x.TableNumber is not null);

        RuleFor(x => x.TableNumber)
            .NotEmpty()
            .When(x => string.Equals(x.OrderType, "EatIn", StringComparison.OrdinalIgnoreCase))
            .WithMessage("TableNumber is required for eat-in orders.");
    }
}

public sealed class CreateOrderEndpoint(IOrderService orderService)
    : Endpoint<CreateOrderApiRequest, OrderResponse>
{
    public const string Route = "/api/brands/{brandSlug}/shops/{shopId}/orders";

    public override void Configure()
    {
        Post(Route);
        AllowAnonymous();
        PreProcessor<BrandScopedPreProcessor<CreateOrderApiRequest>>();
        Summary(s =>
        {
            s.Summary = "Place a new order";
            s.Description =
                "Creates a new order for the specified shop. " +
                "Server resolves current product prices — client-submitted prices are ignored. " +
                "VAT is applied at 6% for Pickup/Delivery and 21% for EatIn.";
            s.Response<OrderResponse>(201, "Order placed successfully.");
            s.Response(400, "Validation error or unknown product/modifier.");
            s.Response(404, "Shop or brand not found.");
        });
    }

    public override async Task HandleAsync(CreateOrderApiRequest req, CancellationToken ct)
    {
        try
        {
            var appRequest = new CreateOrderRequest(
                req.ShopId,
                req.BrandSlug,
                req.OrderType,
                req.PaymentMethod,
                req.CustomerName,
                req.Items
                    .Select(i => new OrderItemInput(
                        i.ProductId,
                        i.Quantity,
                        (i.SelectedModifierIds ?? []).AsReadOnly()))
                    .ToList()
                    .AsReadOnly(),
                req.TableNumber);

            var response = await orderService.CreateOrderAsync(appRequest, ct);

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

using System.Security.Claims;
using FastEndpoints;
using FluentValidation;
using FluentValidation.Results;
using TheMillionthFoodOrderApp.Application.Orders;
using TheMillionthFoodOrderApp.Domain.Orders;

namespace TheMillionthFoodOrderApp.Api.Endpoints.Orders;

public sealed record OrderItemApiInput(Guid ProductId, int Quantity, List<Guid>? SelectedModifierIds);

public sealed record CreateOrderApiRequest(
    [property: RouteParam] string BrandSlug,
    [property: RouteParam] Guid ShopId,
    string OrderType,
    string PaymentMethod,
    string? CustomerFirstName,
    string? CustomerLastName,
    List<OrderItemApiInput> Items,
    string? CustomerEmail = null,
    string? CustomerPhone = null,
    string? LanguageCode = null,
    int? TableNumber = null,
    /// <summary>
    /// UTC start of the chosen time slot (US-FP-019). Null or omitted = ASAP.
    /// Must be returned verbatim from GET time-slots; the server re-validates alignment,
    /// same-local-day, opening block, and capacity at create time.
    /// </summary>
    DateTimeOffset? TimeSlotStart = null);

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

        // Contact-field SHAPE only (max length / email format). Presence ("required for guest
        // checkout") is enforced in the handler after merging claim-or-body values, because a
        // FluentValidation validator cannot read the authenticated user's claims (US-FP-051).
        RuleFor(x => x.CustomerFirstName)
            .MaximumLength(100)
            .When(x => x.CustomerFirstName is not null);

        RuleFor(x => x.CustomerLastName)
            .MaximumLength(100)
            .When(x => x.CustomerLastName is not null);

        RuleFor(x => x.CustomerEmail)
            .EmailAddress().WithMessage("CustomerEmail must be a valid email address.")
            .MaximumLength(320).WithMessage("CustomerEmail must not exceed 320 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.CustomerEmail));

        RuleFor(x => x.CustomerPhone)
            .MaximumLength(32).WithMessage("CustomerPhone must not exceed 32 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.CustomerPhone));

        RuleFor(x => x.LanguageCode)
            .Must(v => v is "nl" or "fr" or "de")
            .WithMessage("LanguageCode must be one of: nl, fr, de.")
            .When(x => !string.IsNullOrWhiteSpace(x.LanguageCode));

        // A supplied table number must be positive. Whether it is *required* for eat-in depends on
        // the shop's eat-in settings (US-FP-066) and is enforced server-side in OrderService.
        RuleFor(x => x.TableNumber)
            .GreaterThan(0).WithMessage("TableNumber must be greater than zero.")
            .When(x => x.TableNumber is not null);
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
        // Merge contact details: prefer the authenticated customer's profile claims (populated
        // only in real OIDC mode), falling back to the request body for guests (US-FP-051).
        var user = HttpContext.User;

        string? ClaimOrNull(params string[] keys) =>
            keys.Select(k => user.FindFirstValue(k))
                .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

        var givenName = ClaimOrNull("given_name");
        var familyName = ClaimOrNull("family_name");
        if (givenName is null && familyName is null)
        {
            // No discrete name claims — fall back to splitting a combined "name" claim.
            var combined = ClaimOrNull("name", ClaimTypes.Name);
            if (!string.IsNullOrWhiteSpace(combined))
            {
                var parts = combined.Trim().Split(' ', 2,
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                givenName = parts.Length > 0 ? parts[0] : null;
                familyName = parts.Length > 1 ? parts[1] : null;
            }
        }

        var firstName = !string.IsNullOrWhiteSpace(givenName) ? givenName : req.CustomerFirstName;
        var lastName = !string.IsNullOrWhiteSpace(familyName) ? familyName : req.CustomerLastName;
        var email = ClaimOrNull("email", ClaimTypes.Email) ?? req.CustomerEmail;
        var phone = ClaimOrNull("phone_number") ?? req.CustomerPhone;

        // Every online order must carry a complete contact record (guests type it; logged-in
        // customers get it from their profile) so the digital receipt can be delivered.
        var missing = new List<ValidationFailure>();
        if (string.IsNullOrWhiteSpace(firstName))
            missing.Add(new(nameof(req.CustomerFirstName), "First name is required."));
        if (string.IsNullOrWhiteSpace(lastName))
            missing.Add(new(nameof(req.CustomerLastName), "Last name is required."));
        if (string.IsNullOrWhiteSpace(email))
            missing.Add(new(nameof(req.CustomerEmail), "Email is required."));
        if (string.IsNullOrWhiteSpace(phone))
            missing.Add(new(nameof(req.CustomerPhone), "Phone number is required."));
        if (missing.Count > 0)
        {
            await HttpContext.Response.SendErrorsAsync(missing, statusCode: 400, cancellation: ct);
            return;
        }

        try
        {
            var appRequest = new CreateOrderRequest(
                req.ShopId,
                req.BrandSlug,
                req.OrderType,
                req.PaymentMethod,
                firstName,
                lastName,
                req.Items
                    .Select(i => new OrderItemInput(
                        i.ProductId,
                        i.Quantity,
                        (i.SelectedModifierIds ?? []).AsReadOnly()))
                    .ToList()
                    .AsReadOnly(),
                email,
                phone,
                req.LanguageCode,
                req.TableNumber,
                req.TimeSlotStart);

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
        catch (InvalidOperationException ex) when (ex.Message == "TIME_SLOT_FULL")
        {
            // The chosen slot has reached its capacity — send a field-level error so the frontend
            // can show the slot-specific message and refetch (the field key is camelCased by FastEndpoints).
            var failures = new List<ValidationFailure>
                { new(nameof(req.TimeSlotStart), "The selected time slot is full. Please pick another slot.") };
            await HttpContext.Response.SendErrorsAsync(failures, statusCode: 400, cancellation: ct);
        }
        catch (InvalidOperationException ex)
        {
            var failures = new List<ValidationFailure> { new("request", ex.Message) };
            await HttpContext.Response.SendErrorsAsync(failures, statusCode: 400, cancellation: ct);
        }
    }
}

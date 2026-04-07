using FastEndpoints;
using FluentValidation;
using FluentValidation.Results;
using TheMillionthFoodOrderApp.Application.OrderLifecycle;

namespace TheMillionthFoodOrderApp.Api.Endpoints.OrderLifecycle;

public sealed record ConfigureOrderLifecycleApiRequest(
    [property: RouteParam] string BrandSlug,
    [property: RouteParam] Guid ShopId,
    List<OrderStatusRequest> Statuses,
    List<OrderStatusTransitionRequest> Transitions);

public sealed class ConfigureOrderLifecycleValidator : Validator<ConfigureOrderLifecycleApiRequest>
{
    private static readonly System.Text.RegularExpressions.Regex HexColorPattern =
        new(@"^#[0-9a-fA-F]{6}$", System.Text.RegularExpressions.RegexOptions.Compiled);

    public ConfigureOrderLifecycleValidator()
    {
        RuleFor(x => x.ShopId)
            .NotEmpty().WithMessage("Shop id is required.");

        RuleFor(x => x.Statuses)
            .NotNull().WithMessage("Statuses must not be null.")
            .Must(s => s is null || s.Count >= 2)
            .WithMessage("At least two statuses are required.");

        RuleFor(x => x.Statuses)
            .Must(s => s is null || s.Any(st => st.IsTerminal))
            .WithMessage("At least one status must be terminal.");

        RuleFor(x => x.Statuses)
            .Must(s =>
            {
                if (s is null) return true;
                var sortOrders = s.Select(st => st.SortOrder).OrderBy(o => o).ToList();
                for (var i = 0; i < sortOrders.Count; i++)
                {
                    if (sortOrders[i] != i) return false;
                }
                return true;
            })
            .WithMessage("Sort orders must be sequential starting from 0 with no gaps or duplicates.");

        RuleForEach(x => x.Statuses).ChildRules(status =>
        {
            status.RuleFor(s => s.Name)
                .NotEmpty().WithMessage("Status name is required.")
                .MaximumLength(100).WithMessage("Status name must not exceed 100 characters.");

            status.RuleFor(s => s.ColorHex)
                .Matches(HexColorPattern)
                .When(s => s.ColorHex is not null)
                .WithMessage("ColorHex must be in #RRGGBB format (e.g. '#FF5733').");
        });

        RuleFor(x => x.Transitions)
            .NotNull().WithMessage("Transitions must not be null.");

        RuleFor(x => x)
            .Must(req =>
            {
                if (req.Statuses is null || req.Transitions is null) return true;
                var validSortOrders = new HashSet<int>(req.Statuses.Select(s => s.SortOrder));
                return req.Transitions.All(t =>
                    validSortOrders.Contains(t.FromSortOrder) &&
                    validSortOrders.Contains(t.ToSortOrder));
            })
            .WithMessage("All transitions must reference valid status sort orders.")
            .WithName("Transitions");
    }
}

public sealed class ConfigureOrderLifecycleEndpoint(IOrderLifecycleService service)
    : Endpoint<ConfigureOrderLifecycleApiRequest, OrderLifecycleResponse>
{
    public override void Configure()
    {
        Put("/api/brands/{brandSlug}/shops/{shopId}/order-lifecycle");
        // TODO: Require ShopManager role when auth is implemented (US-FP-039)
        AllowAnonymous();
        PreProcessor<BrandScopedPreProcessor<ConfigureOrderLifecycleApiRequest>>();
        Summary(s =>
        {
            s.Summary = "Configure order lifecycle for a shop";
            s.Description = "Replaces the complete order lifecycle configuration (statuses and transitions) for the given shop.";
            s.Response<OrderLifecycleResponse>(200, "Order lifecycle configured successfully.");
            s.Response(400, "Validation error.");
            s.Response(404, "Shop not found.");
        });
    }

    public override async Task HandleAsync(ConfigureOrderLifecycleApiRequest req, CancellationToken ct)
    {
        try
        {
            var appRequest = new ConfigureOrderLifecycleRequest(req.Statuses, req.Transitions);
            var response = await service.ConfigureLifecycleAsync(req.ShopId, appRequest, ct);
            await HttpContext.Response.SendAsync(response, statusCode: 200, cancellation: ct);
        }
        catch (KeyNotFoundException)
        {
            await HttpContext.Response.SendNotFoundAsync(ct);
        }
        catch (ArgumentException ex)
        {
            var failures = new List<ValidationFailure>
            {
                new("orderLifecycle", ex.Message)
            };
            await HttpContext.Response.SendErrorsAsync(failures, statusCode: 400, cancellation: ct);
        }
    }
}

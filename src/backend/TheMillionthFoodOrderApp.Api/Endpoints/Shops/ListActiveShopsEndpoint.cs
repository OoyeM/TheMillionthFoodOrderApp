using FastEndpoints;
using TheMillionthFoodOrderApp.Application.Shops;

namespace TheMillionthFoodOrderApp.Api.Endpoints.Shops;

public sealed record ListActiveShopsRequest([property: RouteParam] string BrandSlug);

/// <summary>
/// Public storefront endpoint — lists only active shops for a brand,
/// enriched with real-time open/closed status.
/// The frontend uses the returned <c>slug</c> field to resolve a shop URL
/// and the <c>id</c> field when placing orders.
/// </summary>
public sealed class ListActiveShopsEndpoint(IShopService shopService)
    : Endpoint<ListActiveShopsRequest, IReadOnlyList<StorefrontShopResponse>>
{
    public const string Route = "/api/brands/{brandSlug}/shops/active";

    public override void Configure()
    {
        Get(Route);
        AllowAnonymous();
        PreProcessor<BrandScopedPreProcessor<ListActiveShopsRequest>>();
        Summary(s =>
        {
            s.Summary = "List active shops for the storefront";
            s.Description =
                "Returns all active shops belonging to the brand, ordered by name, " +
                "with real-time open/closed status. " +
                "Use the shop slug for customer-facing URLs; use the shop id when placing orders.";
            s.Response<IReadOnlyList<StorefrontShopResponse>>(200, "List of active shops with open/closed status.");
        });
    }

    public override async Task HandleAsync(ListActiveShopsRequest req, CancellationToken ct)
    {
        var response = await shopService.GetActiveShopsAsync(ct);
        await HttpContext.Response.SendAsync(response, statusCode: 200, cancellation: ct);
    }
}

using FastEndpoints;
using TheMillionthFoodOrderApp.Application.Shops;

namespace TheMillionthFoodOrderApp.Api.Endpoints.Shops;

public sealed record ListShopsRequest([property: RouteParam] string BrandSlug);

public sealed class ListShopsEndpoint(IShopService shopService)
    : Endpoint<ListShopsRequest, IReadOnlyList<ShopResponse>>
{
    public override void Configure()
    {
        Get("/api/brands/{brandSlug}/shops");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "List all shops for a brand";
            s.Description = "Returns all shops belonging to the brand, ordered by name.";
            s.Response<IReadOnlyList<ShopResponse>>(200, "List of shops.");
        });
    }

    public override async Task HandleAsync(ListShopsRequest req, CancellationToken ct)
    {
        var response = await shopService.GetShopsAsync(ct);
        await HttpContext.Response.SendAsync(response, statusCode: 200, cancellation: ct);
    }
}

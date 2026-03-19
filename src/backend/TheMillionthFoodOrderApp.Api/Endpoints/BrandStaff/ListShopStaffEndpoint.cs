using FastEndpoints;
using TheMillionthFoodOrderApp.Application.Identity;

namespace TheMillionthFoodOrderApp.Api.Endpoints.BrandStaff;

public sealed record ListShopStaffRequest(
    [property: RouteParam] string BrandSlug,
    [property: RouteParam] Guid ShopId);

public sealed class ListShopStaffEndpoint(IBrandStaffService brandStaffService)
    : Endpoint<ListShopStaffRequest, IReadOnlyList<StaffMemberResponse>>
{
    public override void Configure()
    {
        Get("/api/brands/{brandSlug}/shops/{shopId}/staff");
        AllowAnonymous();
        PreProcessor<BrandScopedPreProcessor<ListShopStaffRequest>>();
        Summary(s =>
        {
            s.Summary = "List all staff for a specific shop";
            s.Description = "Returns all staff role assignments scoped to the given shop within a brand.";
            s.Response<IReadOnlyList<StaffMemberResponse>>(200, "List of staff members for the shop.");
        });
    }

    public override async Task HandleAsync(ListShopStaffRequest req, CancellationToken ct)
    {
        var response = await brandStaffService.ListByShopAsync(req.BrandSlug, req.ShopId, ct);
        await HttpContext.Response.SendAsync(response, statusCode: 200, cancellation: ct);
    }
}

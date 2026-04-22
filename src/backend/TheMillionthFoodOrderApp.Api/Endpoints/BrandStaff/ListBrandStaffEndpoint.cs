using FastEndpoints;
using TheMillionthFoodOrderApp.Application.Identity;

namespace TheMillionthFoodOrderApp.Api.Endpoints.BrandStaff;

public sealed record ListBrandStaffRequest([property: RouteParam] string BrandSlug);

public sealed class ListBrandStaffEndpoint(IBrandStaffService brandStaffService)
    : Endpoint<ListBrandStaffRequest, IReadOnlyList<StaffMemberResponse>>
{
    public const string Route = "/api/brands/{brandSlug}/staff";

    public override void Configure()
    {
        Get(Route);
        AllowAnonymous();
        PreProcessor<BrandScopedPreProcessor<ListBrandStaffRequest>>();
        Summary(s =>
        {
            s.Summary = "List all staff for a brand";
            s.Description = "Returns all staff role assignments for the brand, one row per role.";
            s.Response<IReadOnlyList<StaffMemberResponse>>(200, "List of staff members.");
        });
    }

    public override async Task HandleAsync(ListBrandStaffRequest req, CancellationToken ct)
    {
        var response = await brandStaffService.ListAsync(req.BrandSlug, ct);
        await HttpContext.Response.SendAsync(response, statusCode: 200, cancellation: ct);
    }
}

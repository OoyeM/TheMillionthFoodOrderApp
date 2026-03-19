using FastEndpoints;
using TheMillionthFoodOrderApp.Application.ModifierGroups;

namespace TheMillionthFoodOrderApp.Api.Endpoints.ModifierGroups;

public sealed record ListModifierGroupsRequest([property: RouteParam] string BrandSlug);

public sealed class ListModifierGroupsEndpoint(IModifierGroupService modifierGroupService)
    : Endpoint<ListModifierGroupsRequest, IReadOnlyList<ModifierGroupListItemResponse>>
{
    public override void Configure()
    {
        Get("/api/brands/{brandSlug}/modifier-groups");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "List all modifier groups for a brand";
            s.Description = "Returns all non-deleted modifier groups belonging to the brand, ordered by creation date.";
            s.Response<IReadOnlyList<ModifierGroupListItemResponse>>(200, "List of modifier groups.");
        });
    }

    public override async Task HandleAsync(ListModifierGroupsRequest req, CancellationToken ct)
    {
        var response = await modifierGroupService.GetModifierGroupsAsync(ct);
        await HttpContext.Response.SendAsync(response, statusCode: 200, cancellation: ct);
    }
}

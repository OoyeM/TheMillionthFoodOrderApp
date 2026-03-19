using FastEndpoints;
using TheMillionthFoodOrderApp.Application.ModifierGroups;

namespace TheMillionthFoodOrderApp.Api.Endpoints.ModifierGroups;

public sealed record GetProductModifierGroupsRequest(
    [property: RouteParam] string BrandSlug,
    [property: RouteParam] Guid ProductId);

public sealed class GetProductModifierGroupsEndpoint(IModifierGroupService modifierGroupService)
    : Endpoint<GetProductModifierGroupsRequest, IReadOnlyList<ProductModifierGroupResponse>>
{
    public override void Configure()
    {
        Get("/api/brands/{brandSlug}/products/{productId}/modifier-groups");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get modifier groups assigned to a product";
            s.Description = "Returns all modifier group assignments for the given product, ordered by sort order.";
            s.Response<IReadOnlyList<ProductModifierGroupResponse>>(200, "List of modifier group assignments.");
        });
    }

    public override async Task HandleAsync(GetProductModifierGroupsRequest req, CancellationToken ct)
    {
        var response = await modifierGroupService.GetProductModifierGroupsAsync(req.ProductId, ct);
        await HttpContext.Response.SendAsync(response, statusCode: 200, cancellation: ct);
    }
}

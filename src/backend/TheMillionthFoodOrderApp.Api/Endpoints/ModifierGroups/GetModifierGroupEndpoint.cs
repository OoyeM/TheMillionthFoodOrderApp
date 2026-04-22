using FastEndpoints;
using TheMillionthFoodOrderApp.Application.ModifierGroups;

namespace TheMillionthFoodOrderApp.Api.Endpoints.ModifierGroups;

public sealed record GetModifierGroupRequest(
    [property: RouteParam] string BrandSlug,
    [property: RouteParam] Guid Id);

public sealed class GetModifierGroupEndpoint(IModifierGroupService modifierGroupService)
    : Endpoint<GetModifierGroupRequest, ModifierGroupResponse>
{
    public const string Route = "/api/brands/{brandSlug}/modifier-groups/{id}";

    public override void Configure()
    {
        Get(Route);
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Get a modifier group by id";
            s.Response<ModifierGroupResponse>(200, "Modifier group found.");
            s.Response(404, "Modifier group not found.");
        });
    }

    public override async Task HandleAsync(GetModifierGroupRequest req, CancellationToken ct)
    {
        try
        {
            var response = await modifierGroupService.GetModifierGroupAsync(req.Id, ct);
            await HttpContext.Response.SendAsync(response, statusCode: 200, cancellation: ct);
        }
        catch (KeyNotFoundException)
        {
            await HttpContext.Response.SendNotFoundAsync(ct);
        }
    }
}

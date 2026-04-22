using FastEndpoints;
using TheMillionthFoodOrderApp.Application.ModifierGroups;

namespace TheMillionthFoodOrderApp.Api.Endpoints.ModifierGroups;

public sealed record DeleteModifierGroupRequest(
    [property: RouteParam] string BrandSlug,
    [property: RouteParam] Guid Id);

public sealed class DeleteModifierGroupEndpoint(IModifierGroupService modifierGroupService)
    : Endpoint<DeleteModifierGroupRequest>
{
    public const string Route = "/api/brands/{brandSlug}/modifier-groups/{id}";

    public override void Configure()
    {
        Delete(Route);
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Delete a modifier group (soft-delete)";
            s.Description = "Brand Admin soft-deletes a modifier group. Hidden from storefronts but retained for historical order records.";
            s.Response(204, "Modifier group deleted successfully.");
            s.Response(404, "Modifier group not found.");
        });
    }

    public override async Task HandleAsync(DeleteModifierGroupRequest req, CancellationToken ct)
    {
        try
        {
            await modifierGroupService.DeleteModifierGroupAsync(req.Id, ct);
            await HttpContext.Response.SendNoContentAsync(ct);
        }
        catch (KeyNotFoundException)
        {
            await HttpContext.Response.SendNotFoundAsync(ct);
        }
    }
}

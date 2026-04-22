using FastEndpoints;
using FluentValidation.Results;
using TheMillionthFoodOrderApp.Application.Identity;

namespace TheMillionthFoodOrderApp.Api.Endpoints.BrandStaff;

public sealed record DeactivateBrandStaffRequest(
    [property: RouteParam] string BrandSlug,
    [property: RouteParam] Guid RoleId);

public sealed class DeactivateBrandStaffEndpoint(IBrandStaffService brandStaffService)
    : Endpoint<DeactivateBrandStaffRequest>
{
    public const string Route = "/api/brands/{brandSlug}/staff/{roleId}/deactivate";

    public override void Configure()
    {
        Post(Route);
        AllowAnonymous();
        PreProcessor<BrandScopedPreProcessor<DeactivateBrandStaffRequest>>();
        Summary(s =>
        {
            s.Summary = "Deactivate a brand staff role assignment";
            s.Description = "Removes a role assignment from a brand staff member. Cannot remove the last BrandAdmin.";
            s.Response(204, "Staff role assignment deactivated successfully.");
            s.Response(404, "Role assignment not found.");
            s.Response(409, "Cannot remove the last BrandAdmin for this brand.");
        });
    }

    public override async Task HandleAsync(DeactivateBrandStaffRequest req, CancellationToken ct)
    {
        try
        {
            await brandStaffService.DeactivateAsync(req.BrandSlug, req.RoleId, ct);
            await HttpContext.Response.SendNoContentAsync(ct);
        }
        catch (KeyNotFoundException)
        {
            await HttpContext.Response.SendNotFoundAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            var failures = new List<ValidationFailure>
            {
                new("roleId", ex.Message)
            };
            await HttpContext.Response.SendErrorsAsync(failures, statusCode: 409, cancellation: ct);
        }
    }
}

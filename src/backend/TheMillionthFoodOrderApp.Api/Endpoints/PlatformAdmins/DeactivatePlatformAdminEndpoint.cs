using FastEndpoints;
using FluentValidation.Results;
using TheMillionthFoodOrderApp.Application.Identity;

namespace TheMillionthFoodOrderApp.Api.Endpoints.PlatformAdmins;

public sealed record DeactivatePlatformAdminRequest([property: RouteParam] Guid Id);

public sealed class DeactivatePlatformAdminEndpoint(IPlatformAdminService platformAdminService)
    : Endpoint<DeactivatePlatformAdminRequest>
{
    public override void Configure()
    {
        Post("/api/platform-admins/{id}/deactivate");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Deactivate a platform admin";
            s.Description = "Revokes platform admin privileges from the specified user. Cannot deactivate the last admin.";
            s.Response(204, "Platform admin deactivated successfully.");
            s.Response(404, "Platform admin not found.");
            s.Response(409, "Cannot deactivate the last platform admin.");
        });
    }

    public override async Task HandleAsync(DeactivatePlatformAdminRequest req, CancellationToken ct)
    {
        try
        {
            await platformAdminService.DeactivateAsync(req.Id, ct);
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
                new("id", ex.Message)
            };
            await HttpContext.Response.SendErrorsAsync(failures, statusCode: 409, cancellation: ct);
        }
    }
}

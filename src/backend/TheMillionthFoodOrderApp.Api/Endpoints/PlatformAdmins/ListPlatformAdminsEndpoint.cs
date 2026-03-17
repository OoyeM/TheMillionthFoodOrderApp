using FastEndpoints;
using TheMillionthFoodOrderApp.Application.Identity;

namespace TheMillionthFoodOrderApp.Api.Endpoints.PlatformAdmins;

public sealed class ListPlatformAdminsEndpoint(IPlatformAdminService platformAdminService)
    : EndpointWithoutRequest<IReadOnlyList<PlatformAdminResponse>>
{
    public override void Configure()
    {
        Get("/api/platform-admins");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "List all platform admins";
            s.Description = "Returns all users who currently hold platform admin privileges.";
            s.Response<IReadOnlyList<PlatformAdminResponse>>(200, "List of platform admins.");
        });
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var response = await platformAdminService.ListAsync(ct);
        await HttpContext.Response.SendAsync(response, statusCode: 200, cancellation: ct);
    }
}

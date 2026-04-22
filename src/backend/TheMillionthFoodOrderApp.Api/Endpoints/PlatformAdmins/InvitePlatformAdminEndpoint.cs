using FastEndpoints;
using FluentValidation;
using FluentValidation.Results;
using TheMillionthFoodOrderApp.Application.Identity;

namespace TheMillionthFoodOrderApp.Api.Endpoints.PlatformAdmins;

public sealed record InvitePlatformAdminRequest(string Email, string DisplayName);

public sealed class InvitePlatformAdminRequestValidator : Validator<InvitePlatformAdminRequest>
{
    public InvitePlatformAdminRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email must be a valid email address.")
            .MaximumLength(320);

        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("Display name is required.")
            .MaximumLength(200);
    }
}

public sealed class InvitePlatformAdminEndpoint(IPlatformAdminService platformAdminService)
    : Endpoint<InvitePlatformAdminRequest, PlatformAdminResponse>
{
    public const string Route = "/api/platform-admins";

    public override void Configure()
    {
        Post(Route);
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Invite a platform admin";
            s.Description = "Creates a new platform admin or promotes an existing user. The invited user is linked to their real identity on first OIDC login.";
            s.Response<PlatformAdminResponse>(201, "Platform admin invited successfully.");
            s.Response(400, "Validation error.");
            s.Response(409, "User is already a platform admin.");
        });
    }

    public override async Task HandleAsync(InvitePlatformAdminRequest req, CancellationToken ct)
    {
        try
        {
            var appRequest = new Application.Identity.InvitePlatformAdminRequest(req.Email, req.DisplayName);
            var response = await platformAdminService.InviteAsync(appRequest, ct);
            await HttpContext.Response.SendAsync(response, statusCode: 201, cancellation: ct);
        }
        catch (InvalidOperationException ex)
        {
            var failures = new List<ValidationFailure>
            {
                new("email", ex.Message)
            };
            await HttpContext.Response.SendErrorsAsync(failures, statusCode: 409, cancellation: ct);
        }
    }
}

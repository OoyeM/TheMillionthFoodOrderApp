using FastEndpoints;
using FluentValidation;
using FluentValidation.Results;
using TheMillionthFoodOrderApp.Application.Identity;
using TheMillionthFoodOrderApp.Domain.Identity;

namespace TheMillionthFoodOrderApp.Api.Endpoints.BrandStaff;

public sealed record InviteBrandStaffRequest(
    [property: RouteParam] string BrandSlug,
    string Email,
    string DisplayName,
    StaffRole Role,
    Guid? ShopId);

public sealed class InviteBrandStaffRequestValidator : Validator<InviteBrandStaffRequest>
{
    public InviteBrandStaffRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email must be a valid email address.")
            .MaximumLength(320);

        RuleFor(x => x.DisplayName)
            .NotEmpty().WithMessage("Display name is required.")
            .MaximumLength(200);

        RuleFor(x => x.Role)
            .IsInEnum().WithMessage(
                "Role must be a valid StaffRole value (0 = BrandAdmin, 1 = ShopManager, 2 = CounterStaff, 3 = KitchenStaff, 4 = FloorStaff).")
            .Must(r => r != StaffRole.Customer).WithMessage(
                "Customer role cannot be assigned via staff management. Customers self-register.");

        // Shop-level roles require a ShopId
        RuleFor(x => x.ShopId)
            .NotNull().WithMessage("ShopId is required for shop-level roles (ShopManager, CounterStaff, KitchenStaff, FloorStaff).")
            .When(x => x.Role is StaffRole.ShopManager or StaffRole.CounterStaff
                or StaffRole.KitchenStaff or StaffRole.FloorStaff);
    }
}

public sealed class InviteBrandStaffEndpoint(IBrandStaffService brandStaffService)
    : Endpoint<InviteBrandStaffRequest, StaffMemberResponse>
{
    public const string Route = "/api/brands/{brandSlug}/staff";

    public override void Configure()
    {
        Post(Route);
        AllowAnonymous();
        PreProcessor<BrandScopedPreProcessor<InviteBrandStaffRequest>>();
        Summary(s =>
        {
            s.Summary = "Invite a staff member to a brand";
            s.Description = "Creates a new staff role assignment. Creates a pending user if the email is not yet registered.";
            s.Response<StaffMemberResponse>(201, "Staff member invited successfully.");
            s.Response(400, "Validation error.");
            s.Response(409, "User already holds this role for the brand/shop.");
        });
    }

    public override async Task HandleAsync(InviteBrandStaffRequest req, CancellationToken ct)
    {
        try
        {
            var appRequest = new Application.Identity.InviteBrandStaffRequest(
                req.Email,
                req.DisplayName,
                req.Role,
                req.ShopId);

            var response = await brandStaffService.InviteAsync(req.BrandSlug, appRequest, ct);
            await HttpContext.Response.SendAsync(response, statusCode: 201, cancellation: ct);
        }
        catch (InvalidOperationException ex)
        {
            var failures = new List<ValidationFailure>
            {
                new("role", ex.Message)
            };
            await HttpContext.Response.SendErrorsAsync(failures, statusCode: 409, cancellation: ct);
        }
        catch (KeyNotFoundException)
        {
            await HttpContext.Response.SendNotFoundAsync(ct);
        }
    }
}

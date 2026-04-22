using FastEndpoints;
using FluentValidation;
using TheMillionthFoodOrderApp.Application.ModifierGroups;

namespace TheMillionthFoodOrderApp.Api.Endpoints.ModifierGroups;

public sealed record ProductModifierGroupAssignmentInput(Guid ModifierGroupId, int SortOrder);

public sealed record SetProductModifierGroupsApiRequest(
    [property: RouteParam] string BrandSlug,
    [property: RouteParam] Guid ProductId,
    List<ProductModifierGroupAssignmentInput> Assignments);

public sealed class SetProductModifierGroupsRequestValidator : Validator<SetProductModifierGroupsApiRequest>
{
    public SetProductModifierGroupsRequestValidator()
    {
        RuleFor(x => x.ProductId)
            .NotEmpty().WithMessage("Product id is required.");

        RuleFor(x => x.Assignments)
            .NotNull().WithMessage("Assignments list is required.");

        RuleForEach(x => x.Assignments).ChildRules(a =>
        {
            a.RuleFor(x => x.ModifierGroupId)
                .NotEmpty().WithMessage("Modifier group id is required.");

            a.RuleFor(x => x.SortOrder)
                .GreaterThanOrEqualTo(0).WithMessage("Sort order must be zero or greater.");
        });

        RuleFor(x => x.Assignments)
            .Must(assignments => assignments?.Select(a => a.ModifierGroupId).Distinct().Count() == assignments?.Count)
            .When(x => x.Assignments is not null)
            .WithMessage("Duplicate modifier group ids are not allowed.");
    }
}

public sealed class SetProductModifierGroupsEndpoint(IModifierGroupService modifierGroupService)
    : Endpoint<SetProductModifierGroupsApiRequest, IReadOnlyList<ProductModifierGroupResponse>>
{
    public const string Route = "/api/brands/{brandSlug}/products/{productId}/modifier-groups";

    public override void Configure()
    {
        Put(Route);
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Set modifier groups for a product";
            s.Description = "Replaces all modifier group assignments for the product with the provided list. Send an empty list to remove all assignments.";
            s.Response<IReadOnlyList<ProductModifierGroupResponse>>(200, "Modifier group assignments updated.");
            s.Response(400, "Validation error.");
        });
    }

    public override async Task HandleAsync(SetProductModifierGroupsApiRequest req, CancellationToken ct)
    {
        var appRequest = new SetProductModifierGroupsRequest(
            req.Assignments
                .Select(a => new ProductModifierGroupAssignment(a.ModifierGroupId, a.SortOrder))
                .ToList().AsReadOnly());

        await modifierGroupService.SetProductModifierGroupsAsync(req.ProductId, appRequest, ct);

        var response = await modifierGroupService.GetProductModifierGroupsAsync(req.ProductId, ct);
        await HttpContext.Response.SendAsync(response, statusCode: 200, cancellation: ct);
    }
}

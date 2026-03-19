using FastEndpoints;
using FluentValidation;
using TheMillionthFoodOrderApp.Application.ModifierGroups;

namespace TheMillionthFoodOrderApp.Api.Endpoints.ModifierGroups;

public sealed record UpdateModifierGroupApiRequest(
    [property: RouteParam] string BrandSlug,
    [property: RouteParam] Guid Id,
    List<GroupTranslationInput> Translations,
    List<ModifierInput> Modifiers);

public sealed class UpdateModifierGroupRequestValidator : Validator<UpdateModifierGroupApiRequest>
{
    public UpdateModifierGroupRequestValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Modifier group id is required.");

        RuleFor(x => x.Translations)
            .NotEmpty().WithMessage("At least one translation is required.");

        RuleForEach(x => x.Translations).ChildRules(t =>
        {
            t.RuleFor(x => x.LanguageCode)
                .NotEmpty().WithMessage("Language code is required.")
                .Must(lc => lc is "nl" or "fr" or "de")
                .WithMessage("Language code must be 'nl', 'fr', or 'de'.");

            t.RuleFor(x => x.Name)
                .NotEmpty().WithMessage("Translation name is required.")
                .MaximumLength(200);
        });

        RuleFor(x => x.Translations)
            .Must(ts => ts?.Select(t => t.LanguageCode).Distinct().Count() == ts?.Count)
            .When(x => x.Translations is not null)
            .WithMessage("Duplicate language codes are not allowed.");

        RuleFor(x => x.Modifiers)
            .NotEmpty().WithMessage("At least one modifier is required.");

        RuleForEach(x => x.Modifiers).ChildRules(m =>
        {
            m.RuleFor(x => x.SortOrder)
                .GreaterThanOrEqualTo(0).WithMessage("Sort order must be zero or greater.");

            m.RuleFor(x => x.Translations)
                .NotEmpty().WithMessage("Each modifier requires at least one translation.");

            m.RuleForEach(x => x.Translations).ChildRules(t =>
            {
                t.RuleFor(x => x.LanguageCode)
                    .NotEmpty().WithMessage("Language code is required.")
                    .Must(lc => lc is "nl" or "fr" or "de")
                    .WithMessage("Language code must be 'nl', 'fr', or 'de'.");

                t.RuleFor(x => x.Name)
                    .NotEmpty().WithMessage("Modifier translation name is required.")
                    .MaximumLength(200);
            });
        });
    }
}

public sealed class UpdateModifierGroupEndpoint(IModifierGroupService modifierGroupService)
    : Endpoint<UpdateModifierGroupApiRequest, ModifierGroupResponse>
{
    public override void Configure()
    {
        Put("/api/brands/{brandSlug}/modifier-groups/{id}");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Update a modifier group";
            s.Description = "Brand Admin updates modifier group details including translations and modifiers.";
            s.Response<ModifierGroupResponse>(200, "Modifier group updated successfully.");
            s.Response(400, "Validation error.");
            s.Response(404, "Modifier group not found.");
        });
    }

    public override async Task HandleAsync(UpdateModifierGroupApiRequest req, CancellationToken ct)
    {
        try
        {
            var appRequest = new UpdateModifierGroupRequest(
                req.Translations
                    .Select(t => new GroupTranslationRequest(t.LanguageCode, t.Name))
                    .ToList().AsReadOnly(),
                req.Modifiers
                    .Select(m => new ModifierRequest(
                        m.PriceAdjustment,
                        m.SortOrder,
                        m.Translations
                            .Select(t => new ModifierTranslationRequest(t.LanguageCode, t.Name))
                            .ToList().AsReadOnly()))
                    .ToList().AsReadOnly());

            var response = await modifierGroupService.UpdateModifierGroupAsync(req.Id, appRequest, ct);

            await HttpContext.Response.SendAsync(response, statusCode: 200, cancellation: ct);
        }
        catch (KeyNotFoundException)
        {
            await HttpContext.Response.SendNotFoundAsync(ct);
        }
    }
}

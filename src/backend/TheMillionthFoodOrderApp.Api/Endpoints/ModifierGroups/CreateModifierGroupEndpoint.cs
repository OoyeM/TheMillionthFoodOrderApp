using FastEndpoints;
using FluentValidation;
using FluentValidation.Results;
using TheMillionthFoodOrderApp.Application.ModifierGroups;

namespace TheMillionthFoodOrderApp.Api.Endpoints.ModifierGroups;

public sealed record ModifierTranslationInput(string LanguageCode, string Name);

public sealed record ModifierInput(
    decimal PriceAdjustment,
    int SortOrder,
    List<ModifierTranslationInput> Translations);

public sealed record GroupTranslationInput(string LanguageCode, string Name);

public sealed record CreateModifierGroupApiRequest(
    [property: RouteParam] string BrandSlug,
    List<GroupTranslationInput> Translations,
    List<ModifierInput> Modifiers);

public sealed class CreateModifierGroupRequestValidator : Validator<CreateModifierGroupApiRequest>
{
    public CreateModifierGroupRequestValidator()
    {
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

public sealed class CreateModifierGroupEndpoint(IModifierGroupService modifierGroupService)
    : Endpoint<CreateModifierGroupApiRequest, ModifierGroupResponse>
{
    public override void Configure()
    {
        Post("/api/brands/{brandSlug}/modifier-groups");
        AllowAnonymous();
        Summary(s =>
        {
            s.Summary = "Create a new modifier group within a brand";
            s.Description = "Brand Admin creates a modifier group with multilingual name and a list of modifiers.";
            s.Response<ModifierGroupResponse>(201, "Modifier group created successfully.");
            s.Response(400, "Validation error.");
        });
    }

    public override async Task HandleAsync(CreateModifierGroupApiRequest req, CancellationToken ct)
    {
        try
        {
            var appRequest = new CreateModifierGroupRequest(
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

            var response = await modifierGroupService.CreateModifierGroupAsync(appRequest, ct);

            await HttpContext.Response.SendAsync(response, statusCode: 201, cancellation: ct);
        }
        catch (InvalidOperationException ex)
        {
            var failures = new List<ValidationFailure> { new("translations", ex.Message) };
            await HttpContext.Response.SendErrorsAsync(failures, statusCode: 400, cancellation: ct);
        }
    }
}

using FastEndpoints;
using FluentValidation;
using FluentValidation.Results;
using TheMillionthFoodOrderApp.Application.TaxConfiguration;

namespace TheMillionthFoodOrderApp.Api.Endpoints.TaxConfiguration;

public sealed record VatRateInput(string ConsumptionMode, decimal RatePercentage);

public sealed record UpdateTaxConfigurationApiRequest(
    [property: RouteParam] string BrandSlug,
    IReadOnlyList<VatRateInput> VatRates);

public sealed class UpdateTaxConfigurationValidator : Validator<UpdateTaxConfigurationApiRequest>
{
    private static readonly HashSet<string> ValidConsumptionModes = ["Takeaway", "EatIn"];

    public UpdateTaxConfigurationValidator()
    {
        RuleFor(x => x.VatRates)
            .NotEmpty().WithMessage("At least one VAT rate must be provided.");

        RuleForEach(x => x.VatRates).ChildRules(rate =>
        {
            rate.RuleFor(r => r.ConsumptionMode)
                .NotEmpty().WithMessage("ConsumptionMode is required.")
                .Must(m => ValidConsumptionModes.Contains(m))
                .WithMessage("ConsumptionMode must be 'Takeaway' or 'EatIn'.");

            rate.RuleFor(r => r.RatePercentage)
                .GreaterThanOrEqualTo(0).WithMessage("RatePercentage must be greater than or equal to 0.")
                .LessThanOrEqualTo(100).WithMessage("RatePercentage must be less than or equal to 100.");
        });

        RuleFor(x => x.VatRates)
            .Must(rates =>
            {
                if (rates is null) return true;
                var modes = rates.Select(r => r.ConsumptionMode).ToList();
                return modes.Count == modes.Distinct().Count();
            })
            .WithMessage("Duplicate ConsumptionMode values are not allowed. Each consumption mode may only appear once.");
    }
}

public sealed class UpdateTaxConfigurationEndpoint(ITaxConfigurationService service)
    : Endpoint<UpdateTaxConfigurationApiRequest, TaxConfigurationResponse>
{
    public override void Configure()
    {
        Put("/api/brands/{brandSlug}/tax-configuration");
        // TODO: Require BrandAdmin role when auth is implemented (US-FP-046)
        AllowAnonymous();
        PreProcessor<BrandScopedPreProcessor<UpdateTaxConfigurationApiRequest>>();
        Summary(s =>
        {
            s.Summary = "Update tax configuration for a brand";
            s.Description = "Creates or replaces the VAT rate configuration for the specified brand.";
            s.Response<TaxConfigurationResponse>(200, "Tax configuration updated successfully.");
            s.Response(400, "Validation error.");
        });
    }

    public override async Task HandleAsync(UpdateTaxConfigurationApiRequest req, CancellationToken ct)
    {
        try
        {
            var appRequest = new UpdateTaxConfigurationRequest(
                req.VatRates.Select(r => new VatRateDto(r.ConsumptionMode, r.RatePercentage)).ToList());

            var response = await service.UpsertAsync(appRequest, ct);

            await HttpContext.Response.SendAsync(response, statusCode: 200, cancellation: ct);
        }
        catch (ArgumentException ex)
        {
            var failures = new List<ValidationFailure>
            {
                new("vatRates", ex.Message)
            };
            await HttpContext.Response.SendErrorsAsync(failures, statusCode: 400, cancellation: ct);
        }
    }
}

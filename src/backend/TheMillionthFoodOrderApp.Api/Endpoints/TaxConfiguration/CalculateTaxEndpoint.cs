using FastEndpoints;
using FluentValidation;
using TheMillionthFoodOrderApp.Application.TaxConfiguration;

namespace TheMillionthFoodOrderApp.Api.Endpoints.TaxConfiguration;

public sealed record CalculateTaxApiRequest(
    [property: RouteParam] string BrandSlug,
    decimal GrossAmount,
    string ConsumptionMode);

public sealed class CalculateTaxValidator : Validator<CalculateTaxApiRequest>
{
    private static readonly HashSet<string> ValidConsumptionModes = ["Takeaway", "EatIn"];

    public CalculateTaxValidator()
    {
        RuleFor(x => x.GrossAmount)
            .GreaterThan(0).WithMessage("GrossAmount must be greater than 0.");

        RuleFor(x => x.ConsumptionMode)
            .NotEmpty().WithMessage("ConsumptionMode is required.")
            .Must(m => ValidConsumptionModes.Contains(m))
            .WithMessage("ConsumptionMode must be 'Takeaway' or 'EatIn'.");
    }
}

public sealed class CalculateTaxEndpoint(ITaxConfigurationService service)
    : Endpoint<CalculateTaxApiRequest, TaxBreakdownDto>
{
    public const string Route = "/api/brands/{brandSlug}/tax-configuration/calculate";

    public override void Configure()
    {
        Post(Route);
        // TODO: Require appropriate role when auth is implemented (US-FP-046)
        AllowAnonymous();
        PreProcessor<BrandScopedPreProcessor<CalculateTaxApiRequest>>();
        Summary(s =>
        {
            s.Summary = "Calculate tax breakdown for a given amount and consumption mode";
            s.Description = "Calculates the net amount, VAT amount, and gross amount breakdown using the brand's configured VAT rates.";
            s.Response<TaxBreakdownDto>(200, "Tax breakdown calculated successfully.");
            s.Response(400, "Validation error.");
            s.Response(404, "No tax configuration found for this brand.");
        });
    }

    public override async Task HandleAsync(CalculateTaxApiRequest req, CancellationToken ct)
    {
        try
        {
            var appRequest = new CalculateTaxRequest(req.GrossAmount, req.ConsumptionMode);
            var response = await service.CalculateAsync(appRequest, ct);
            await HttpContext.Response.SendAsync(response, statusCode: 200, cancellation: ct);
        }
        catch (KeyNotFoundException)
        {
            await HttpContext.Response.SendNotFoundAsync(ct);
        }
    }
}

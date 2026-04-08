namespace TheMillionthFoodOrderApp.Application.TaxConfiguration;

public sealed record VatRateDto(string ConsumptionMode, decimal RatePercentage);

public sealed record TaxConfigurationResponse(
    Guid Id,
    IReadOnlyList<VatRateDto> VatRates,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

public sealed record UpdateTaxConfigurationRequest(IReadOnlyList<VatRateDto> VatRates);

public sealed record TaxBreakdownDto(
    decimal NetAmount,
    decimal VatAmount,
    decimal GrossAmount,
    decimal VatRatePercentage);

public sealed record CalculateTaxRequest(decimal GrossAmount, string ConsumptionMode);

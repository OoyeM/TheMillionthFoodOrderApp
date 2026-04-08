namespace TheMillionthFoodOrderApp.Application.TaxConfiguration;

/// <summary>
/// Represents a single VAT rate entry for a given consumption mode.
/// <see cref="ConsumptionMode"/> must be a valid name from the <c>ConsumptionMode</c> enum (e.g. "Takeaway", "EatIn").
/// <see cref="RatePercentage"/> must be between 0 and 100 inclusive.
/// </summary>
public sealed record VatRateDto(string ConsumptionMode, decimal RatePercentage);

/// <summary>
/// Read model returned after retrieving or upserting a tax configuration.
/// Contains all configured VAT rates and the record's audit timestamps.
/// </summary>
public sealed record TaxConfigurationResponse(
    Guid Id,
    IReadOnlyList<VatRateDto> VatRates,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);

/// <summary>
/// Request payload for creating or replacing the active tax configuration.
/// <see cref="VatRates"/> must contain exactly one entry per defined <c>ConsumptionMode</c> value (no duplicates, no omissions).
/// </summary>
public sealed record UpdateTaxConfigurationRequest(IReadOnlyList<VatRateDto> VatRates);

/// <summary>
/// Result of a tax breakdown calculation.
/// All monetary values are in the same currency as the input gross amount.
/// </summary>
public sealed record TaxBreakdownDto(
    decimal NetAmount,
    decimal VatAmount,
    decimal GrossAmount,
    decimal VatRatePercentage);

/// <summary>
/// Request payload for calculating the tax breakdown of a gross price.
/// <see cref="GrossAmount"/> must be >= 0.
/// <see cref="ConsumptionMode"/> must be a valid name from the <c>ConsumptionMode</c> enum (e.g. "Takeaway", "EatIn").
/// </summary>
public sealed record CalculateTaxRequest(decimal GrossAmount, string ConsumptionMode);

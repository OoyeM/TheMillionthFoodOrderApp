namespace TheMillionthFoodOrderApp.Application.TaxConfiguration;

public interface ITaxConfigurationService
{
    Task<TaxConfigurationResponse?> GetAsync(CancellationToken cancellationToken = default);
    Task<TaxConfigurationResponse> UpsertAsync(UpdateTaxConfigurationRequest request, CancellationToken cancellationToken = default);
    Task<TaxBreakdownDto> CalculateAsync(CalculateTaxRequest request, CancellationToken cancellationToken = default);
}

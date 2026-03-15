namespace TheMillionthFoodOrderApp.Application.BrandSettings;

/// <summary>
/// Application service for managing brand-level settings in the brand-specific database.
/// </summary>
public interface IBrandSettingsService
{
    /// <summary>
    /// Returns the current brand settings, or <c>null</c> if not yet provisioned.
    /// </summary>
    Task<BrandSettingsResponse?> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the brand settings, creating the record if it does not exist.
    /// </summary>
    Task<BrandSettingsResponse> UpsertAsync(
        UpdateBrandSettingsRequest request,
        CancellationToken cancellationToken = default);
}

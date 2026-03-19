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

    /// <summary>
    /// Updates the visual theming configuration (colors, typography, custom domain).
    /// Returns <c>null</c> if no BrandSettings record exists yet.
    /// </summary>
    Task<BrandSettingsResponse?> UpdateThemingAsync(
        UpdateBrandThemingRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves the uploaded logo file and stores the resulting URL on BrandSettings.
    /// Returns <c>null</c> if no BrandSettings record exists yet.
    /// </summary>
    Task<UploadBrandLogoResponse?> UploadLogoAsync(
        string fileName,
        string contentType,
        Stream fileStream,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the lightweight public theme DTO for the storefront.
    /// Always returns a response — applies default values when theming is not configured.
    /// Returns <c>null</c> if no BrandSettings record exists yet.
    /// </summary>
    Task<BrandThemeResponse?> GetThemeAsync(CancellationToken cancellationToken = default);
}

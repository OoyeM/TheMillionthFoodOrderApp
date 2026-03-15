namespace TheMillionthFoodOrderApp.Domain.BrandSettings;

/// <summary>
/// Repository interface for <see cref="BrandSettings"/> in the brand-specific database.
/// </summary>
public interface IBrandSettingsRepository
{
    /// <summary>
    /// Returns the brand settings for the current brand, or <c>null</c> if not yet seeded.
    /// </summary>
    Task<BrandSettings?> GetAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new BrandSettings record to the current brand database.
    /// </summary>
    Task AddAsync(BrandSettings settings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists pending changes to the current brand database.
    /// </summary>
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

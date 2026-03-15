using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using TheMillionthFoodOrderApp.Application.Multitenancy;
using TheMillionthFoodOrderApp.Infrastructure.Persistence;

namespace TheMillionthFoodOrderApp.Infrastructure.Multitenancy;

/// <summary>
/// Validates a brand slug against the platform registry, with a 30-second in-memory cache
/// to avoid hitting the platform database on every request.
/// </summary>
public sealed class BrandContextValidator(
    PlatformDbContext platformDbContext,
    IMemoryCache memoryCache) : IBrandContextValidator
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);

    // Cache entries store (exists, isActive) tuples
    private const string CacheKeyPrefix = "brand_validation:";

    /// <inheritdoc />
    public async Task<BrandValidationResult> ValidateAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        var cacheKey = CacheKeyPrefix + slug.ToLowerInvariant();

        if (!memoryCache.TryGetValue(cacheKey, out BrandCacheEntry? entry) || entry is null)
        {
            var brand = await platformDbContext.Brands
                .AsNoTracking()
                .Where(b => b.Slug == slug)
                .Select(b => new BrandCacheEntry(true, b.IsActive))
                .FirstOrDefaultAsync(cancellationToken);

            entry = brand ?? new BrandCacheEntry(false, false);

            memoryCache.Set(cacheKey, entry, CacheTtl);
        }

        if (!entry.Exists)
            return BrandValidationResult.NotFound;

        if (!entry.IsActive)
            return BrandValidationResult.Inactive;

        return BrandValidationResult.Valid;
    }

    private sealed record BrandCacheEntry(bool Exists, bool IsActive);
}

namespace TheMillionthFoodOrderApp.Application.Multitenancy;

/// <summary>
/// Provides the current brand slug for the active HTTP request.
/// The slug is resolved from the route data or request headers by the brand context middleware.
/// </summary>
public interface IBrandContextAccessor
{
    /// <summary>
    /// Gets the brand slug for the current request, or null if no brand context is active.
    /// </summary>
    string? BrandSlug { get; }
}

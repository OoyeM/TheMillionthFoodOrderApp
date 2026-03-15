namespace TheMillionthFoodOrderApp.Application.Multitenancy;

/// <summary>
/// Validates that a brand slug corresponds to a real, active brand in the platform registry.
/// Used by <c>BrandContextMiddleware</c> to guard all brand-scoped routes.
/// </summary>
public interface IBrandContextValidator
{
    /// <summary>
    /// Validates the given brand slug.
    /// </summary>
    /// <param name="slug">The brand slug extracted from the request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="BrandValidationResult"/> indicating the outcome.</returns>
    Task<BrandValidationResult> ValidateAsync(string slug, CancellationToken cancellationToken = default);
}

/// <summary>
/// The outcome of a brand slug validation check.
/// </summary>
public enum BrandValidationResult
{
    /// <summary>Brand exists and is active — request may proceed.</summary>
    Valid,

    /// <summary>No brand with this slug exists in the platform registry — return 404.</summary>
    NotFound,

    /// <summary>Brand exists but is currently inactive — return 403.</summary>
    Inactive,
}

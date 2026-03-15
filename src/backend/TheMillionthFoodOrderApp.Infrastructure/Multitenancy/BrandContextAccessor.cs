using TheMillionthFoodOrderApp.Application.Multitenancy;

namespace TheMillionthFoodOrderApp.Infrastructure.Multitenancy;

/// <summary>
/// Thread-safe, request-scoped holder for the brand slug.
/// Set by <c>BrandContextMiddleware</c> early in the pipeline;
/// consumed by <c>BrandDbContextFactory</c> to resolve the connection string.
/// </summary>
public sealed class BrandContextAccessor : IBrandContextAccessor
{
    /// <inheritdoc />
    public string? BrandSlug { get; set; }
}

using TheMillionthFoodOrderApp.Application.Multitenancy;
using TheMillionthFoodOrderApp.Infrastructure.Multitenancy;

namespace TheMillionthFoodOrderApp.Api.Middleware;

/// <summary>
/// Extracts the brand slug from the request, validates it against the platform registry,
/// and stores it in <see cref="BrandContextAccessor"/> for the rest of the pipeline.
///
/// Resolution order:
///   1. Route value <c>{brandSlug}</c>
///   2. Request header <c>X-Brand-Slug</c>
///
/// Validation outcomes:
///   - Unknown brand slug  → 404 Not Found
///   - Inactive brand slug → 403 Forbidden
///   - No slug present     → passes through (non-brand-scoped routes are unaffected)
/// </summary>
public sealed class BrandContextMiddleware(RequestDelegate next)
{
    private const string RouteKey = "brandSlug";
    private const string HeaderKey = "X-Brand-Slug";

    public async Task InvokeAsync(
        HttpContext httpContext,
        BrandContextAccessor brandContextAccessor,
        IBrandContextValidator brandContextValidator)
    {
        string? slug = null;

        // 1. Try route data first (e.g. /brands/{brandSlug}/...)
        if (httpContext.Request.RouteValues.TryGetValue(RouteKey, out var routeValue) &&
            routeValue is string routeSlug &&
            !string.IsNullOrWhiteSpace(routeSlug))
        {
            slug = routeSlug;
        }
        // 2. Fall back to header
        else if (httpContext.Request.Headers.TryGetValue(HeaderKey, out var headerValue) &&
                 !string.IsNullOrWhiteSpace(headerValue))
        {
            slug = headerValue.ToString();
        }

        // If no brand slug in route or header, this is a platform-level request — pass through.
        if (slug is null)
        {
            await next(httpContext);
            return;
        }

        // Validate slug against platform registry (cached for 30 s)
        var validationResult = await brandContextValidator.ValidateAsync(slug, httpContext.RequestAborted);

        switch (validationResult)
        {
            case BrandValidationResult.NotFound:
                httpContext.Response.StatusCode = StatusCodes.Status404NotFound;
                await httpContext.Response.WriteAsJsonAsync(
                    new { error = $"Brand '{slug}' was not found." },
                    httpContext.RequestAborted);
                return;

            case BrandValidationResult.Inactive:
                httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;
                await httpContext.Response.WriteAsJsonAsync(
                    new { error = $"Brand '{slug}' is currently inactive." },
                    httpContext.RequestAborted);
                return;

            case BrandValidationResult.Valid:
            default:
                brandContextAccessor.BrandSlug = slug;
                await next(httpContext);
                break;
        }
    }
}

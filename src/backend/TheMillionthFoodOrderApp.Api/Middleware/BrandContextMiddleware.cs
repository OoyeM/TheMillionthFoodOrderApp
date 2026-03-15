using TheMillionthFoodOrderApp.Infrastructure.Multitenancy;

namespace TheMillionthFoodOrderApp.Api.Middleware;

/// <summary>
/// Extracts the brand slug from the request and stores it in <see cref="BrandContextAccessor"/>
/// so it is available throughout the rest of the pipeline.
/// Resolution order:
///   1. Route value <c>{brandSlug}</c>
///   2. Request header <c>X-Brand-Slug</c>
/// </summary>
public sealed class BrandContextMiddleware(RequestDelegate next)
{
    private const string RouteKey = "brandSlug";
    private const string HeaderKey = "X-Brand-Slug";

    public async Task InvokeAsync(HttpContext httpContext, BrandContextAccessor brandContextAccessor)
    {
        // 1. Try route data first (e.g. /brands/{brandSlug}/...)
        if (httpContext.Request.RouteValues.TryGetValue(RouteKey, out var routeValue) &&
            routeValue is string routeSlug &&
            !string.IsNullOrWhiteSpace(routeSlug))
        {
            brandContextAccessor.BrandSlug = routeSlug;
        }
        // 2. Fall back to header
        else if (httpContext.Request.Headers.TryGetValue(HeaderKey, out var headerValue) &&
                 !string.IsNullOrWhiteSpace(headerValue))
        {
            brandContextAccessor.BrandSlug = headerValue.ToString();
        }

        await next(httpContext);
    }
}

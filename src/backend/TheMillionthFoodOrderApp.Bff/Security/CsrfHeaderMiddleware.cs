using TheMillionthFoodOrderApp.Bff.Auth;

namespace TheMillionthFoodOrderApp.Bff.Security;

/// <summary>
/// Lightweight CSRF defence: rejects state-changing requests
/// (POST/PUT/PATCH/DELETE) under <c>/bff</c> or <c>/api</c> when the request is
/// missing the <c>X-CSRF: 1</c> header. Browsers cannot send custom headers
/// cross-origin without a successful CORS preflight, so the header's presence
/// proves the request originated from same-origin script.
///
/// The check is bypassed for unauthenticated requests so anonymous flows
/// (e.g. <c>POST /bff/logout</c> when no session exists, idempotent by design)
/// keep working without false-positive 403s.
/// </summary>
public sealed class CsrfHeaderMiddleware(RequestDelegate next)
{
    private static readonly HashSet<string> ProtectedMethods = new(StringComparer.OrdinalIgnoreCase)
    {
        HttpMethods.Post,
        HttpMethods.Put,
        HttpMethods.Patch,
        HttpMethods.Delete,
    };

    public async Task InvokeAsync(HttpContext context)
    {
        if (!ShouldEnforce(context))
        {
            await next(context);
            return;
        }

        var values = context.Request.Headers[AuthConstants.Headers.Csrf];
        var ok = values.Any(v => string.Equals(v, AuthConstants.Headers.CsrfExpectedValue, StringComparison.Ordinal));

        if (!ok)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(
                new { error = "csrf_required" },
                context.RequestAborted);
            return;
        }

        await next(context);
    }

    private static bool ShouldEnforce(HttpContext context)
    {
        if (!ProtectedMethods.Contains(context.Request.Method))
            return false;

        var path = context.Request.Path;
        if (!path.StartsWithSegments("/bff") && !path.StartsWithSegments("/api"))
            return false;

        // Anonymous calls bypass — there's no session to forge against.
        return context.User.Identity?.IsAuthenticated == true;
    }
}

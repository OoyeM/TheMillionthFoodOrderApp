namespace TheMillionthFoodOrderApp.Bff.Security;

/// <summary>
/// Writes a baseline set of OWASP-recommended security response headers on every
/// outbound response and removes server-fingerprinting headers. Registered first
/// in the pipeline so headers are present even on early-returns from downstream
/// middleware (e.g. 401/403 responses).
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context)
    {
        context.Response.OnStarting(static state =>
        {
            var ctx = (HttpContext)state;
            var headers = ctx.Response.Headers;

            // Locked down for the BFF surface — the BFF serves no inline scripts
            // and is never embedded in another origin's iframe.
            headers["Content-Security-Policy"] =
                "default-src 'self'; " +
                "frame-ancestors 'none'; " +
                "base-uri 'self'; " +
                "form-action 'self'";

            headers["X-Content-Type-Options"]   = "nosniff";
            headers["X-Frame-Options"]          = "DENY";
            headers["Referrer-Policy"]          = "strict-origin-when-cross-origin";
            headers["Permissions-Policy"]       =
                "camera=(), microphone=(), geolocation=(), payment=()";

            headers.Remove("Server");
            headers.Remove("X-Powered-By");

            return Task.CompletedTask;
        }, context);

        return next(context);
    }
}

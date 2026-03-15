using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace TheMillionthFoodOrderApp.Api.Auth;

/// <summary>
/// Development-only no-op authentication handler.
/// Used in place of real JWT bearer validation so the authentication middleware
/// is present in the pipeline without ever rejecting requests.
///
/// All API endpoints currently use AllowAnonymous; this handler ensures that
/// adding auth policies in the future works correctly without needing a running
/// identity provider during local development.
///
/// This handler is NEVER registered outside of Development environment.
/// </summary>
internal sealed class DevPassThroughHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        => Task.FromResult(AuthenticateResult.NoResult());
}

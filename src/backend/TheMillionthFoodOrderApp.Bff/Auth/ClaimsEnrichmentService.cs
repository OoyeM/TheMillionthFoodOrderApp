using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using TheMillionthFoodOrderApp.Application.Identity;

namespace TheMillionthFoodOrderApp.Bff.Auth;

/// <summary>
/// Enriches OIDC claims with platform roles from the database.
/// Runs once during the OIDC callback (OnTokenValidated) — enriched claims
/// are stored in the cookie so subsequent requests have zero DB overhead.
/// </summary>
public sealed class ClaimsEnrichmentService(
    IIdentityService identityService,
    ILogger<ClaimsEnrichmentService> logger)
{
    public async Task EnrichClaimsAsync(TokenValidatedContext context)
    {
        var sub = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier)
                  ?? context.Principal?.FindFirstValue("sub");

        if (sub is null)
        {
            logger.LogWarning("OIDC token validated but no 'sub' claim found — skipping enrichment");
            return;
        }

        var principal = context.Principal!;
        var email = principal.FindFirstValue(ClaimTypes.Email)
                    ?? principal.FindFirstValue("email")
                    ?? "";
        var name = principal.FindFirstValue("name")
                   ?? principal.FindFirstValue(ClaimTypes.Name)
                   ?? "";

        var user = await identityService.ProvisionUserAsync(sub, email, name);
        var userWithRoles = await identityService.GetUserWithRolesAsync(user.Id);

        if (userWithRoles is null)
        {
            logger.LogWarning("User {Sub} provisioned but GetUserWithRolesAsync returned null", sub);
            return;
        }

        var identity = principal.Identity as ClaimsIdentity;
        if (identity is null) return;

        if (userWithRoles.IsPlatformAdmin)
        {
            identity.AddClaim(new Claim(ClaimTypes.Role, AuthConstants.Roles.PlatformAdmin));
            identity.AddClaim(new Claim(AuthConstants.Claims.PlatformRole, AuthConstants.Roles.PlatformAdmin));
        }

        foreach (var role in userWithRoles.Roles)
        {
            identity.AddClaim(new Claim(ClaimTypes.Role, role.Role.ToString()));
        }

        logger.LogInformation(
            "Enriched claims for user {Sub} (platformAdmin={IsPlatformAdmin}, roles={RoleCount})",
            sub, userWithRoles.IsPlatformAdmin, userWithRoles.Roles.Count);
    }
}

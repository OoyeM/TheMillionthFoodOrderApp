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
        var cancellationToken = context.HttpContext.RequestAborted;

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

        var user = await identityService.ProvisionUserAsync(sub, email, name, cancellationToken);
        var userWithRoles = await identityService.GetUserWithRolesAsync(user.Id, cancellationToken);

        if (userWithRoles is null)
        {
            logger.LogError(
                "User {Sub} provisioned (Id={UserId}) but GetUserWithRolesAsync returned null — denying login",
                sub, user.Id);
            context.Fail("Claims enrichment failed: user not found after provisioning.");
            return;
        }

        var identity = principal.Identity as ClaimsIdentity;
        if (identity is null) return;

        // Platform admin role
        if (userWithRoles.IsPlatformAdmin)
        {
            identity.AddClaim(new Claim(ClaimTypes.Role, AuthConstants.Roles.PlatformAdmin));
            identity.AddClaim(new Claim(AuthConstants.Claims.PlatformRole, AuthConstants.Roles.PlatformAdmin));
        }

        // Brand-scoped roles with slugs (matching mock auth claim format)
        var brandSlugs = userWithRoles.Roles.Select(r => r.BrandSlug).Distinct();
        foreach (var slug in brandSlugs)
        {
            identity.AddClaim(new Claim(AuthConstants.Claims.BrandSlug, slug));
        }

        foreach (var role in userWithRoles.Roles)
        {
            identity.AddClaim(new Claim(ClaimTypes.Role, role.Role.ToString()));
            identity.AddClaim(new Claim(
                AuthConstants.Claims.BrandRoles,
                $"{role.BrandSlug}:{role.Role}"));
        }

        logger.LogInformation(
            "Enriched claims for user {Sub} (platformAdmin={IsPlatformAdmin}, roles={RoleCount})",
            sub, userWithRoles.IsPlatformAdmin, userWithRoles.Roles.Count);
    }
}

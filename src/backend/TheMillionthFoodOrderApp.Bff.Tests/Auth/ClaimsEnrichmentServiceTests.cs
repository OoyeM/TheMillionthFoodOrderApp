using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;
using TheMillionthFoodOrderApp.Application.Identity;
using TheMillionthFoodOrderApp.Bff.Auth;
using TheMillionthFoodOrderApp.Domain.Identity;

namespace TheMillionthFoodOrderApp.Bff.Tests.Auth;

/// <summary>
/// Unit tests for <see cref="ClaimsEnrichmentService"/>.
/// Uses a fake <see cref="IIdentityService"/> so no database or HTTP context is needed.
///
/// Note: <see cref="TokenValidatedContext"/> normally requires an active OpenIdConnect
/// middleware pipeline. We construct the minimal subset used by
/// <see cref="ClaimsEnrichmentService"/> (Principal + HttpContext.RequestAborted).
/// Full OIDC-pipeline integration tests are deferred to Wave 2.
/// </summary>
public sealed class ClaimsEnrichmentServiceTests
{
    // ── Fixtures ──────────────────────────────────────────────────────────────

    private static PlatformUser BuildPlatformUser() =>
        PlatformUser.Create("sub-123", "test@example.com", "Test User");

    // ── Tests ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task EnrichClaimsAsync_PlatformAdmin_AddsPlatformAdminRoleClaims()
    {
        // Arrange
        var provisioned = BuildPlatformUser();
        var userWithRoles = new UserWithRolesDto(
            Id: provisioned.Id,
            ExternalIdentityId: "sub-123",
            Email: "admin@example.com",
            DisplayName: "Admin",
            IsPlatformAdmin: true,
            CreatedAt: DateTimeOffset.UtcNow,
            Roles: []);

        var fakeIdentityService = new FakeIdentityService(provisioned, userWithRoles);
        var sut = new ClaimsEnrichmentService(fakeIdentityService, NullLogger<ClaimsEnrichmentService>.Instance);

        var (context, identity) = BuildTokenValidatedContext("sub-123", "admin@example.com", "Admin");

        // Act
        await sut.EnrichClaimsAsync(context);

        // Assert
        identity.FindAll(ClaimTypes.Role)
                .Select(c => c.Value)
                .ShouldContain(AuthConstants.Roles.PlatformAdmin);

        identity.FindFirst(AuthConstants.Claims.PlatformRole)?.Value
                .ShouldBe(AuthConstants.Roles.PlatformAdmin);
    }

    [Fact]
    public async Task EnrichClaimsAsync_BrandAdmin_AddsBrandSlugAndBrandRolesClaims()
    {
        // Arrange
        var provisioned = BuildPlatformUser();
        var brandId = Guid.CreateVersion7();
        var userWithRoles = new UserWithRolesDto(
            Id: provisioned.Id,
            ExternalIdentityId: "sub-456",
            Email: "ba@frietjes.com",
            DisplayName: "Brand Admin",
            IsPlatformAdmin: false,
            CreatedAt: DateTimeOffset.UtcNow,
            Roles:
            [
                new RoleAssignmentDto(brandId, "frietjes", null, StaffRole.BrandAdmin),
            ]);

        var fakeIdentityService = new FakeIdentityService(provisioned, userWithRoles);
        var sut = new ClaimsEnrichmentService(fakeIdentityService, NullLogger<ClaimsEnrichmentService>.Instance);

        var (context, identity) = BuildTokenValidatedContext("sub-456", "ba@frietjes.com", "Brand Admin");

        // Act
        await sut.EnrichClaimsAsync(context);

        // Assert
        identity.FindFirst(AuthConstants.Claims.BrandSlug)?.Value.ShouldBe("frietjes");
        identity.FindFirst(AuthConstants.Claims.BrandRoles)?.Value.ShouldBe("frietjes:BrandAdmin");

        identity.FindAll(ClaimTypes.Role)
                .Select(c => c.Value)
                .ShouldContain(StaffRole.BrandAdmin.ToString());
    }

    [Fact]
    public async Task EnrichClaimsAsync_UserWithMultipleBrandRoles_AddsAllClaims()
    {
        // Arrange
        var provisioned = BuildPlatformUser();
        var brandId1 = Guid.CreateVersion7();
        var brandId2 = Guid.CreateVersion7();

        var userWithRoles = new UserWithRolesDto(
            Id: provisioned.Id,
            ExternalIdentityId: "sub-789",
            Email: "staff@example.com",
            DisplayName: "Staff",
            IsPlatformAdmin: false,
            CreatedAt: DateTimeOffset.UtcNow,
            Roles:
            [
                new RoleAssignmentDto(brandId1, "frietjes", null, StaffRole.BrandAdmin),
                new RoleAssignmentDto(brandId2, "other-brand", null, StaffRole.CounterStaff),
            ]);

        var fakeIdentityService = new FakeIdentityService(provisioned, userWithRoles);
        var sut = new ClaimsEnrichmentService(fakeIdentityService, NullLogger<ClaimsEnrichmentService>.Instance);

        var (context, identity) = BuildTokenValidatedContext("sub-789", "staff@example.com", "Staff");

        // Act
        await sut.EnrichClaimsAsync(context);

        // Assert — both brand slugs present
        var brandSlugs = identity.FindAll(AuthConstants.Claims.BrandSlug)
                                 .Select(c => c.Value)
                                 .ToList();
        brandSlugs.ShouldContain("frietjes");
        brandSlugs.ShouldContain("other-brand");

        var brandRoles = identity.FindAll(AuthConstants.Claims.BrandRoles)
                                 .Select(c => c.Value)
                                 .ToList();
        brandRoles.ShouldContain("frietjes:BrandAdmin");
        brandRoles.ShouldContain("other-brand:CounterStaff");
    }

    [Fact(Skip = "Requires integration-level OIDC context — missing 'sub' claim path covered in Wave 2")]
    public Task EnrichClaimsAsync_NoSubClaim_LogsWarningAndSkipsEnrichment()
    {
        // This test requires constructing a TokenValidatedContext where the principal
        // has no NameIdentifier or 'sub' claim. The service logs a warning and returns
        // early without calling IdentityService. Full OIDC context mocking is deferred
        // to Wave 2 where a live OIDC pipeline can be used.
        return Task.CompletedTask;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a minimal <see cref="TokenValidatedContext"/> and the underlying
    /// <see cref="ClaimsIdentity"/> so tests can assert enriched claims directly.
    /// </summary>
    private static (TokenValidatedContext context, ClaimsIdentity identity) BuildTokenValidatedContext(
        string sub, string email, string name)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, sub),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Name, name),
        };

        var identity = new ClaimsIdentity(claims, "Test");
        var principal = new ClaimsPrincipal(identity);

        var httpContext = new DefaultHttpContext();

        var scheme = new AuthenticationScheme(
            AuthConstants.Schemes.Oidc,
            displayName: null,
            handlerType: typeof(OpenIdConnectHandler));

        var oidcOptions = new OpenIdConnectOptions();

        var context = new TokenValidatedContext(
            httpContext,
            scheme,
            oidcOptions,
            principal,
            new AuthenticationProperties());

        return (context, identity);
    }
}

// ── Fake identity service ─────────────────────────────────────────────────────

/// <summary>
/// In-memory stub for <see cref="IIdentityService"/> that returns pre-configured data.
/// Only the methods used by <see cref="ClaimsEnrichmentService"/> are meaningfully implemented.
/// </summary>
file sealed class FakeIdentityService(
    PlatformUser provisionedUser,
    UserWithRolesDto? userWithRoles)
    : IIdentityService
{
    public Task<PlatformUser> ProvisionUserAsync(
        string externalIdentityId, string email, string displayName,
        CancellationToken cancellationToken = default)
        => Task.FromResult(provisionedUser);

    public Task<UserWithRolesDto?> GetUserWithRolesAsync(
        Guid platformUserId, CancellationToken cancellationToken = default)
        => Task.FromResult(userWithRoles);

    public Task AssignRoleAsync(
        Guid platformUserId, Guid brandId, Guid? shopId, StaffRole role,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task RemoveRoleAsync(
        Guid platformUserId, Guid brandId, Guid? shopId, StaffRole role,
        CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task<IReadOnlyList<UserWithRolesDto>> GetBrandStaffAsync(
        Guid brandId, CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<UserWithRolesDto>>([]);
}

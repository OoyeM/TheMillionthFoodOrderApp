using System.Security.Claims;
using Shouldly;
using TheMillionthFoodOrderApp.Bff.Auth;

namespace TheMillionthFoodOrderApp.Bff.Tests.Auth;

/// <summary>
/// Pure unit tests for <see cref="MockAuthHandler.BuildPrincipal"/>.
/// No HTTP stack is involved — tests call the static method directly.
/// </summary>
public sealed class MockAuthHandlerTests
{
    // ── Known personas ────────────────────────────────────────────────────────

    [Theory]
    [InlineData(MockPersonas.PlatformAdmin)]
    [InlineData(MockPersonas.BrandAdminFrietjes)]
    [InlineData(MockPersonas.CounterStaffFrietjes)]
    [InlineData(MockPersonas.Customer)]
    public void BuildPrincipal_KnownPersona_ReturnsNonNullPrincipal(string persona)
    {
        var principal = MockAuthHandler.BuildPrincipal(persona);

        principal.ShouldNotBeNull();
    }

    [Theory]
    [InlineData(MockPersonas.PlatformAdmin)]
    [InlineData(MockPersonas.BrandAdminFrietjes)]
    [InlineData(MockPersonas.CounterStaffFrietjes)]
    [InlineData(MockPersonas.Customer)]
    public void BuildPrincipal_KnownPersona_HasMockAuthSchemeIdentity(string persona)
    {
        var principal = MockAuthHandler.BuildPrincipal(persona);

        principal!.Identity!.AuthenticationType.ShouldBe(AuthConstants.Schemes.Mock);
    }

    [Fact]
    public void BuildPrincipal_PlatformAdmin_HasPlatformAdminRole()
    {
        var principal = MockAuthHandler.BuildPrincipal(MockPersonas.PlatformAdmin);

        principal.ShouldNotBeNull();
        principal.IsInRole(AuthConstants.Roles.PlatformAdmin).ShouldBeTrue();
    }

    [Fact]
    public void BuildPrincipal_PlatformAdmin_HasPlatformRoleClaim()
    {
        var principal = MockAuthHandler.BuildPrincipal(MockPersonas.PlatformAdmin);

        principal!.FindFirstValue(AuthConstants.Claims.PlatformRole)
                  .ShouldBe(AuthConstants.Roles.PlatformAdmin);
    }

    [Fact]
    public void BuildPrincipal_PlatformAdmin_HasNoBrandSlugClaim()
    {
        // Platform admin is not scoped to a brand
        var principal = MockAuthHandler.BuildPrincipal(MockPersonas.PlatformAdmin);

        principal!.FindFirstValue(AuthConstants.Claims.BrandSlug).ShouldBeNull();
    }

    [Fact]
    public void BuildPrincipal_BrandAdminFrietjes_HasBrandAdminRole()
    {
        var principal = MockAuthHandler.BuildPrincipal(MockPersonas.BrandAdminFrietjes);

        principal.ShouldNotBeNull();
        principal.IsInRole(AuthConstants.Roles.BrandAdmin).ShouldBeTrue();
    }

    [Fact]
    public void BuildPrincipal_BrandAdminFrietjes_HasFrietjesBrandSlug()
    {
        var principal = MockAuthHandler.BuildPrincipal(MockPersonas.BrandAdminFrietjes);

        principal!.FindFirstValue(AuthConstants.Claims.BrandSlug).ShouldBe("frietjes");
    }

    [Fact]
    public void BuildPrincipal_BrandAdminFrietjes_HasBrandRolesClaim()
    {
        var principal = MockAuthHandler.BuildPrincipal(MockPersonas.BrandAdminFrietjes);

        principal!.FindFirstValue(AuthConstants.Claims.BrandRoles).ShouldBe("frietjes:BrandAdmin");
    }

    [Fact]
    public void BuildPrincipal_CounterStaffFrietjes_HasCounterStaffRole()
    {
        var principal = MockAuthHandler.BuildPrincipal(MockPersonas.CounterStaffFrietjes);

        principal.ShouldNotBeNull();
        principal.IsInRole(AuthConstants.Roles.CounterStaff).ShouldBeTrue();
    }

    [Fact]
    public void BuildPrincipal_CounterStaffFrietjes_HasFrietjesBrandSlug()
    {
        var principal = MockAuthHandler.BuildPrincipal(MockPersonas.CounterStaffFrietjes);

        principal!.FindFirstValue(AuthConstants.Claims.BrandSlug).ShouldBe("frietjes");
    }

    [Fact]
    public void BuildPrincipal_CounterStaffFrietjes_HasBrandRolesClaim()
    {
        var principal = MockAuthHandler.BuildPrincipal(MockPersonas.CounterStaffFrietjes);

        principal!.FindFirstValue(AuthConstants.Claims.BrandRoles).ShouldBe("frietjes:CounterStaff");
    }

    [Fact]
    public void BuildPrincipal_Customer_HasCustomerRole()
    {
        var principal = MockAuthHandler.BuildPrincipal(MockPersonas.Customer);

        principal.ShouldNotBeNull();
        principal.IsInRole(AuthConstants.Roles.Customer).ShouldBeTrue();
    }

    [Fact]
    public void BuildPrincipal_Customer_HasNoBrandSlugClaim()
    {
        var principal = MockAuthHandler.BuildPrincipal(MockPersonas.Customer);

        principal!.FindFirstValue(AuthConstants.Claims.BrandSlug).ShouldBeNull();
    }

    // ── Unknown persona ───────────────────────────────────────────────────────

    [Fact]
    public void BuildPrincipal_UnknownPersona_ReturnsNull()
    {
        var principal = MockAuthHandler.BuildPrincipal("unknown-persona");

        principal.ShouldBeNull();
    }

    [Fact]
    public void BuildPrincipal_EmptyString_ReturnsNull()
    {
        var principal = MockAuthHandler.BuildPrincipal(string.Empty);

        principal.ShouldBeNull();
    }

}

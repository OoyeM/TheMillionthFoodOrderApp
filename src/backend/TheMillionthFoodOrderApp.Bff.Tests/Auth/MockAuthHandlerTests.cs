using System.Security.Claims;
using TheMillionthFoodOrderApp.Bff.Auth;

namespace TheMillionthFoodOrderApp.Bff.Tests.Auth;

/// <summary>
/// Pure unit tests for <see cref="MockAuthHandler.BuildPrincipal"/>.
/// No HTTP stack is involved — tests call the static method directly.
/// </summary>
public sealed class MockAuthHandlerTests
{
    // ── Known personas ────────────────────────────────────────────────────────

    [Test]
    [Arguments(MockPersonas.PlatformAdmin)]
    [Arguments(MockPersonas.BrandAdminFrietjes)]
    [Arguments(MockPersonas.CounterStaffFrietjes)]
    [Arguments(MockPersonas.Customer)]
    public async Task BuildPrincipal_KnownPersona_ReturnsNonNullPrincipal(string persona)
    {
        var principal = MockAuthHandler.BuildPrincipal(persona);

        await Assert.That(principal).IsNotNull();
    }

    [Test]
    [Arguments(MockPersonas.PlatformAdmin)]
    [Arguments(MockPersonas.BrandAdminFrietjes)]
    [Arguments(MockPersonas.CounterStaffFrietjes)]
    [Arguments(MockPersonas.Customer)]
    public async Task BuildPrincipal_KnownPersona_HasMockAuthSchemeIdentity(string persona)
    {
        var principal = MockAuthHandler.BuildPrincipal(persona);

        await Assert.That(principal!.Identity!.AuthenticationType).IsEqualTo(AuthConstants.Schemes.Mock);
    }

    [Test]
    public async Task BuildPrincipal_PlatformAdmin_HasPlatformAdminRole()
    {
        var principal = MockAuthHandler.BuildPrincipal(MockPersonas.PlatformAdmin);

        await Assert.That(principal).IsNotNull();
        await Assert.That(principal!.IsInRole(AuthConstants.Roles.PlatformAdmin)).IsTrue();
    }

    [Test]
    public async Task BuildPrincipal_PlatformAdmin_HasPlatformRoleClaim()
    {
        var principal = MockAuthHandler.BuildPrincipal(MockPersonas.PlatformAdmin);

        await Assert.That(principal!.FindFirstValue(AuthConstants.Claims.PlatformRole))
            .IsEqualTo(AuthConstants.Roles.PlatformAdmin);
    }

    [Test]
    public async Task BuildPrincipal_PlatformAdmin_HasNoBrandSlugClaim()
    {
        // Platform admin is not scoped to a brand
        var principal = MockAuthHandler.BuildPrincipal(MockPersonas.PlatformAdmin);

        await Assert.That(principal!.FindFirstValue(AuthConstants.Claims.BrandSlug)).IsNull();
    }

    [Test]
    public async Task BuildPrincipal_BrandAdminFrietjes_HasBrandAdminRole()
    {
        var principal = MockAuthHandler.BuildPrincipal(MockPersonas.BrandAdminFrietjes);

        await Assert.That(principal).IsNotNull();
        await Assert.That(principal!.IsInRole(AuthConstants.Roles.BrandAdmin)).IsTrue();
    }

    [Test]
    public async Task BuildPrincipal_BrandAdminFrietjes_HasFrietjesBrandSlug()
    {
        var principal = MockAuthHandler.BuildPrincipal(MockPersonas.BrandAdminFrietjes);

        await Assert.That(principal!.FindFirstValue(AuthConstants.Claims.BrandSlug)).IsEqualTo("frietjes");
    }

    [Test]
    public async Task BuildPrincipal_BrandAdminFrietjes_HasBrandRolesClaim()
    {
        var principal = MockAuthHandler.BuildPrincipal(MockPersonas.BrandAdminFrietjes);

        await Assert.That(principal!.FindFirstValue(AuthConstants.Claims.BrandRoles)).IsEqualTo("frietjes:BrandAdmin");
    }

    [Test]
    public async Task BuildPrincipal_CounterStaffFrietjes_HasCounterStaffRole()
    {
        var principal = MockAuthHandler.BuildPrincipal(MockPersonas.CounterStaffFrietjes);

        await Assert.That(principal).IsNotNull();
        await Assert.That(principal!.IsInRole(AuthConstants.Roles.CounterStaff)).IsTrue();
    }

    [Test]
    public async Task BuildPrincipal_CounterStaffFrietjes_HasFrietjesBrandSlug()
    {
        var principal = MockAuthHandler.BuildPrincipal(MockPersonas.CounterStaffFrietjes);

        await Assert.That(principal!.FindFirstValue(AuthConstants.Claims.BrandSlug)).IsEqualTo("frietjes");
    }

    [Test]
    public async Task BuildPrincipal_CounterStaffFrietjes_HasBrandRolesClaim()
    {
        var principal = MockAuthHandler.BuildPrincipal(MockPersonas.CounterStaffFrietjes);

        await Assert.That(principal!.FindFirstValue(AuthConstants.Claims.BrandRoles)).IsEqualTo("frietjes:CounterStaff");
    }

    [Test]
    public async Task BuildPrincipal_Customer_HasCustomerRole()
    {
        var principal = MockAuthHandler.BuildPrincipal(MockPersonas.Customer);

        await Assert.That(principal).IsNotNull();
        await Assert.That(principal!.IsInRole(AuthConstants.Roles.Customer)).IsTrue();
    }

    [Test]
    public async Task BuildPrincipal_Customer_HasNoBrandSlugClaim()
    {
        var principal = MockAuthHandler.BuildPrincipal(MockPersonas.Customer);

        await Assert.That(principal!.FindFirstValue(AuthConstants.Claims.BrandSlug)).IsNull();
    }

    // ── Customer persona: OIDC profile claims (US-FP-051) ────────────────────

    [Test]
    public async Task BuildPrincipal_Customer_HasGivenNameClaimTest()
    {
        var principal = MockAuthHandler.BuildPrincipal(MockPersonas.Customer);

        await Assert.That(principal!.FindFirstValue(AuthConstants.Claims.GivenName))
            .IsEqualTo("Test");
    }

    [Test]
    public async Task BuildPrincipal_Customer_HasFamilyNameClaimCustomer()
    {
        var principal = MockAuthHandler.BuildPrincipal(MockPersonas.Customer);

        await Assert.That(principal!.FindFirstValue(AuthConstants.Claims.FamilyName))
            .IsEqualTo("Customer");
    }

    [Test]
    public async Task BuildPrincipal_Customer_HasPhoneNumberClaim()
    {
        var principal = MockAuthHandler.BuildPrincipal(MockPersonas.Customer);

        await Assert.That(principal!.FindFirstValue(AuthConstants.Claims.PhoneNumber))
            .IsEqualTo("+32470000004");
    }

    // ── Unknown persona ───────────────────────────────────────────────────────

    [Test]
    public async Task BuildPrincipal_UnknownPersona_ReturnsNull()
    {
        var principal = MockAuthHandler.BuildPrincipal("unknown-persona");

        await Assert.That(principal).IsNull();
    }

    [Test]
    public async Task BuildPrincipal_EmptyString_ReturnsNull()
    {
        var principal = MockAuthHandler.BuildPrincipal(string.Empty);

        await Assert.That(principal).IsNull();
    }
}

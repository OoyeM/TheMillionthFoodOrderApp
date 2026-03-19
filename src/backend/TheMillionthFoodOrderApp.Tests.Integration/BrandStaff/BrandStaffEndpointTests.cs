using System.Net;
using System.Net.Http.Json;
using Shouldly;
using TheMillionthFoodOrderApp.Application.Identity;
using TheMillionthFoodOrderApp.Domain.Identity;
using TheMillionthFoodOrderApp.Tests.Integration.Fixtures;

namespace TheMillionthFoodOrderApp.Tests.Integration.BrandStaff;

/// <summary>
/// Integration tests for brand staff account endpoints.
/// Runs against a real SQL Server via Testcontainers.
/// </summary>
public sealed class BrandStaffEndpointTests(IntegrationTestBase fixture)
    : IClassFixture<IntegrationTestBase>
{
    private HttpClient CreateClient() => fixture.Factory.CreateClient();

    private static string StaffUrl(string brandSlug) =>
        $"/api/brands/{brandSlug}/staff";

    private static string ShopStaffUrl(string brandSlug, Guid shopId) =>
        $"/api/brands/{brandSlug}/shops/{shopId}/staff";

    private static string DeactivateUrl(string brandSlug, Guid roleId) =>
        $"/api/brands/{brandSlug}/staff/{roleId}/deactivate";

    private static object MakeInviteRequest(
        string email,
        string displayName,
        StaffRole role,
        Guid? shopId = null)
        => new { Email = email, DisplayName = displayName, Role = (int)role, ShopId = shopId };

    // ── List ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListBrandStaff_EmptyBrand_Returns200WithEmptyList()
    {
        var client = CreateClient();

        var response = await client.GetAsync(StaffUrl(IntegrationTestBase.GammaSlug));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var staff = await response.Content.ReadFromJsonAsync<List<StaffMemberResponse>>();
        staff.ShouldNotBeNull();
        staff.ShouldBeEmpty();
    }

    // ── Invite ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task InviteBrandStaff_BrandAdmin_Returns201()
    {
        var client = CreateClient();
        var email = $"brand-admin-{Guid.NewGuid():N}@test.com";

        var response = await client.PostAsJsonAsync(
            StaffUrl(IntegrationTestBase.AlphaSlug),
            MakeInviteRequest(email, "Brand Admin User", StaffRole.BrandAdmin));

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var member = await response.Content.ReadFromJsonAsync<StaffMemberResponse>();
        member.ShouldNotBeNull();
        member.Id.ShouldNotBe(Guid.Empty);
        member.Email.ShouldBe(email);
        member.DisplayName.ShouldBe("Brand Admin User");
        member.Role.ShouldBe(StaffRole.BrandAdmin);
        member.ShopId.ShouldBeNull();
        member.RoleId.ShouldNotBe(Guid.Empty);
    }

    [Fact]
    public async Task InviteBrandStaff_ShopRole_WithShopId_Returns201()
    {
        var client = CreateClient();

        // First create a shop to get a shop ID
        var shopId = Guid.CreateVersion7();

        // We use a fixed known shopId from test seed — since we don't have one from seed,
        // we invite with a shop-level role providing a shop ID. The service does not validate
        // shop existence (it stores the FK directly). For the name resolution it will return null.
        var email = $"counter-staff-{Guid.NewGuid():N}@test.com";

        var response = await client.PostAsJsonAsync(
            StaffUrl(IntegrationTestBase.AlphaSlug),
            MakeInviteRequest(email, "Counter Staff User", StaffRole.CounterStaff, shopId));

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var member = await response.Content.ReadFromJsonAsync<StaffMemberResponse>();
        member.ShouldNotBeNull();
        member.Role.ShouldBe(StaffRole.CounterStaff);
        member.ShopId.ShouldBe(shopId);
    }

    [Fact]
    public async Task InviteBrandStaff_ShopRoleWithoutShopId_Returns400()
    {
        var client = CreateClient();
        var email = $"manager-{Guid.NewGuid():N}@test.com";

        var response = await client.PostAsJsonAsync(
            StaffUrl(IntegrationTestBase.AlphaSlug),
            MakeInviteRequest(email, "Shop Manager", StaffRole.ShopManager, shopId: null));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task InviteBrandStaff_DuplicateRole_Returns409()
    {
        var client = CreateClient();
        var email = $"dup-admin-{Guid.NewGuid():N}@test.com";

        // First invite succeeds
        var firstResponse = await client.PostAsJsonAsync(
            StaffUrl(IntegrationTestBase.AlphaSlug),
            MakeInviteRequest(email, "Admin", StaffRole.BrandAdmin));
        firstResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Second invite with the same role returns 409
        var secondResponse = await client.PostAsJsonAsync(
            StaffUrl(IntegrationTestBase.AlphaSlug),
            MakeInviteRequest(email, "Admin", StaffRole.BrandAdmin));
        secondResponse.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task InviteBrandStaff_ExistingUserNewRole_Returns201()
    {
        var client = CreateClient();
        var email = $"multi-role-{Guid.NewGuid():N}@test.com";
        var shopId = Guid.CreateVersion7();

        // Invite as BrandAdmin first
        var firstResponse = await client.PostAsJsonAsync(
            StaffUrl(IntegrationTestBase.AlphaSlug),
            MakeInviteRequest(email, "Multi Role User", StaffRole.BrandAdmin));
        firstResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var firstMember = await firstResponse.Content.ReadFromJsonAsync<StaffMemberResponse>();
        firstMember.ShouldNotBeNull();

        // Then invite the same user as CounterStaff (different role) — should succeed
        var secondResponse = await client.PostAsJsonAsync(
            StaffUrl(IntegrationTestBase.AlphaSlug),
            MakeInviteRequest(email, "Multi Role User", StaffRole.CounterStaff, shopId));
        secondResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var secondMember = await secondResponse.Content.ReadFromJsonAsync<StaffMemberResponse>();
        secondMember.ShouldNotBeNull();
        secondMember.Id.ShouldBe(firstMember.Id); // Same user
        secondMember.Role.ShouldBe(StaffRole.CounterStaff);
        secondMember.RoleId.ShouldNotBe(firstMember.RoleId); // Different role assignment
    }

    // ── Deactivate ────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeactivateBrandStaff_Returns204()
    {
        var client = CreateClient();

        // Invite two BrandAdmins so we can deactivate one without hitting the last-admin guard
        var keepEmail = $"keep-brand-admin-{Guid.NewGuid():N}@test.com";
        await client.PostAsJsonAsync(
            StaffUrl(IntegrationTestBase.BetaSlug),
            MakeInviteRequest(keepEmail, "Keep Admin", StaffRole.BrandAdmin));

        var deactivateEmail = $"deactivate-brand-admin-{Guid.NewGuid():N}@test.com";
        var inviteResponse = await client.PostAsJsonAsync(
            StaffUrl(IntegrationTestBase.BetaSlug),
            MakeInviteRequest(deactivateEmail, "Temp Admin", StaffRole.BrandAdmin));
        var member = await inviteResponse.Content.ReadFromJsonAsync<StaffMemberResponse>();
        member.ShouldNotBeNull();

        var response = await client.PostAsJsonAsync(
            DeactivateUrl(IntegrationTestBase.BetaSlug, member.RoleId), (object?)null);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeactivateBrandStaff_LastBrandAdmin_Returns409()
    {
        var client = CreateClient();
        var brandSlug = IntegrationTestBase.AlphaSlug;

        // Get current list of all brand admins
        var listResponse = await client.GetAsync(StaffUrl(brandSlug));
        var allStaff = await listResponse.Content.ReadFromJsonAsync<List<StaffMemberResponse>>();
        allStaff.ShouldNotBeNull();

        // Invite a dedicated anchor BrandAdmin
        var anchorEmail = $"anchor-brand-admin-{Guid.NewGuid():N}@test.com";
        var anchorResponse = await client.PostAsJsonAsync(
            StaffUrl(brandSlug),
            MakeInviteRequest(anchorEmail, "Anchor Admin", StaffRole.BrandAdmin));
        var anchor = await anchorResponse.Content.ReadFromJsonAsync<StaffMemberResponse>();
        anchor.ShouldNotBeNull();

        // Deactivate all pre-existing BrandAdmins (stop on 409 if we hit the last one)
        foreach (var existing in allStaff.Where(s => s.Role == StaffRole.BrandAdmin))
        {
            var dr = await client.PostAsJsonAsync(DeactivateUrl(brandSlug, existing.RoleId), (object?)null);
            if (dr.StatusCode == HttpStatusCode.Conflict)
                break;
        }

        // Verify exactly 1 BrandAdmin remains (the anchor)
        var currentListResponse = await client.GetAsync(StaffUrl(brandSlug));
        var currentStaff = await currentListResponse.Content.ReadFromJsonAsync<List<StaffMemberResponse>>();
        currentStaff.ShouldNotBeNull();
        currentStaff.Count(s => s.Role == StaffRole.BrandAdmin).ShouldBe(1);

        // Attempt to deactivate the last BrandAdmin — must return 409
        var lastDeactivateResponse = await client.PostAsJsonAsync(
            DeactivateUrl(brandSlug, anchor.RoleId), (object?)null);

        lastDeactivateResponse.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DeactivateBrandStaff_NotFound_Returns404()
    {
        var client = CreateClient();
        var nonExistentRoleId = Guid.NewGuid();

        var response = await client.PostAsJsonAsync(
            DeactivateUrl(IntegrationTestBase.AlphaSlug, nonExistentRoleId), (object?)null);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // ── Shop-filtered list ────────────────────────────────────────────────────

    [Fact]
    public async Task ListShopStaff_ReturnsOnlyShopRoles()
    {
        var client = CreateClient();
        var shopId = Guid.CreateVersion7();
        var brandSlug = IntegrationTestBase.AlphaSlug;

        // Invite a staff member scoped to the shop
        var shopEmail = $"shop-staff-{Guid.NewGuid():N}@test.com";
        await client.PostAsJsonAsync(
            StaffUrl(brandSlug),
            MakeInviteRequest(shopEmail, "Shop Staff", StaffRole.KitchenStaff, shopId));

        // Invite a brand-level staff member (no shop)
        var brandEmail = $"brand-only-{Guid.NewGuid():N}@test.com";
        await client.PostAsJsonAsync(
            StaffUrl(brandSlug),
            MakeInviteRequest(brandEmail, "Brand Only", StaffRole.BrandAdmin));

        var response = await client.GetAsync(ShopStaffUrl(brandSlug, shopId));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var shopStaff = await response.Content.ReadFromJsonAsync<List<StaffMemberResponse>>();
        shopStaff.ShouldNotBeNull();
        shopStaff.ShouldAllBe(s => s.ShopId == shopId);
    }
}

using System.Net;
using System.Net.Http.Json;
using TheMillionthFoodOrderApp.Application.Identity;
using TheMillionthFoodOrderApp.Domain.Identity;
using TheMillionthFoodOrderApp.Tests.Integration.Fixtures;

namespace TheMillionthFoodOrderApp.Tests.Integration.BrandStaff;

/// <summary>
/// Integration tests for brand staff account endpoints.
/// Runs against a real SQL Server via Testcontainers.
/// </summary>
/// <remarks>
/// All tests carry [NotInParallel("brand-staff")] so they run sequentially with each other.
/// Every test in this class mutates or reads the shared brand databases (alpha/beta/gamma),
/// and parallel execution causes data races — e.g. concurrent invitations interfere with the
/// last-admin guard test, and reads on gamma can race against writes on alpha.
/// Other test classes (products, shops, etc.) are unaffected and still run in parallel.
/// </remarks>
[ClassDataSource<IntegrationTestBase>(Shared = SharedType.PerClass)]
public sealed class BrandStaffEndpointTests(IntegrationTestBase fixture)
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

    [Test]
    [NotInParallel("brand-staff")]
    public async Task ListBrandStaff_EmptyBrand_Returns200WithEmptyList()
    {
        var client = CreateClient();

        var response = await client.GetAsync(StaffUrl(IntegrationTestBase.GammaSlug));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var staff = await response.Content.ReadFromJsonAsync<List<StaffMemberResponse>>();
        await Assert.That(staff).IsNotNull();
        await Assert.That(staff!).IsEmpty();
    }

    // ── Invite ────────────────────────────────────────────────────────────────

    [Test]
    [NotInParallel("brand-staff")]
    public async Task InviteBrandStaff_BrandAdmin_Returns201()
    {
        var client = CreateClient();
        var email = $"brand-admin-{Guid.NewGuid():N}@test.com";

        var response = await client.PostAsJsonAsync(
            StaffUrl(IntegrationTestBase.AlphaSlug),
            MakeInviteRequest(email, "Brand Admin User", StaffRole.BrandAdmin));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var member = await response.Content.ReadFromJsonAsync<StaffMemberResponse>();
        await Assert.That(member).IsNotNull();
        await Assert.That(member!.Id).IsNotEqualTo(Guid.Empty);
        await Assert.That(member.Email).IsEqualTo(email);
        await Assert.That(member.DisplayName).IsEqualTo("Brand Admin User");
        await Assert.That(member.Role).IsEqualTo(StaffRole.BrandAdmin);
        await Assert.That(member.ShopId).IsNull();
        await Assert.That(member.RoleId).IsNotEqualTo(Guid.Empty);
    }

    [Test]
    [NotInParallel("brand-staff")]
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

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var member = await response.Content.ReadFromJsonAsync<StaffMemberResponse>();
        await Assert.That(member).IsNotNull();
        await Assert.That(member!.Role).IsEqualTo(StaffRole.CounterStaff);
        await Assert.That(member.ShopId).IsEqualTo(shopId);
    }

    [Test]
    [NotInParallel("brand-staff")]
    public async Task InviteBrandStaff_ShopRoleWithoutShopId_Returns400()
    {
        var client = CreateClient();
        var email = $"manager-{Guid.NewGuid():N}@test.com";

        var response = await client.PostAsJsonAsync(
            StaffUrl(IntegrationTestBase.AlphaSlug),
            MakeInviteRequest(email, "Shop Manager", StaffRole.ShopManager, shopId: null));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    [Test]
    [NotInParallel("brand-staff")]
    public async Task InviteBrandStaff_DuplicateRole_Returns409()
    {
        var client = CreateClient();
        var email = $"dup-admin-{Guid.NewGuid():N}@test.com";

        // First invite succeeds
        var firstResponse = await client.PostAsJsonAsync(
            StaffUrl(IntegrationTestBase.AlphaSlug),
            MakeInviteRequest(email, "Admin", StaffRole.BrandAdmin));
        await Assert.That(firstResponse.StatusCode).IsEqualTo(HttpStatusCode.Created);

        // Second invite with the same role returns 409
        var secondResponse = await client.PostAsJsonAsync(
            StaffUrl(IntegrationTestBase.AlphaSlug),
            MakeInviteRequest(email, "Admin", StaffRole.BrandAdmin));
        await Assert.That(secondResponse.StatusCode).IsEqualTo(HttpStatusCode.Conflict);
    }

    [Test]
    [NotInParallel("brand-staff")]
    public async Task InviteBrandStaff_ExistingUserNewRole_Returns201()
    {
        var client = CreateClient();
        var email = $"multi-role-{Guid.NewGuid():N}@test.com";
        var shopId = Guid.CreateVersion7();

        // Invite as BrandAdmin first
        var firstResponse = await client.PostAsJsonAsync(
            StaffUrl(IntegrationTestBase.AlphaSlug),
            MakeInviteRequest(email, "Multi Role User", StaffRole.BrandAdmin));
        await Assert.That(firstResponse.StatusCode).IsEqualTo(HttpStatusCode.Created);
        var firstMember = await firstResponse.Content.ReadFromJsonAsync<StaffMemberResponse>();
        await Assert.That(firstMember).IsNotNull();

        // Then invite the same user as CounterStaff (different role) — should succeed
        var secondResponse = await client.PostAsJsonAsync(
            StaffUrl(IntegrationTestBase.AlphaSlug),
            MakeInviteRequest(email, "Multi Role User", StaffRole.CounterStaff, shopId));
        await Assert.That(secondResponse.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var secondMember = await secondResponse.Content.ReadFromJsonAsync<StaffMemberResponse>();
        await Assert.That(secondMember).IsNotNull();
        await Assert.That(secondMember!.Id).IsEqualTo(firstMember!.Id); // Same user
        await Assert.That(secondMember.Role).IsEqualTo(StaffRole.CounterStaff);
        await Assert.That(secondMember.RoleId).IsNotEqualTo(firstMember.RoleId); // Different role assignment
    }

    // ── Deactivate ────────────────────────────────────────────────────────────

    [Test]
    [NotInParallel("brand-staff")]
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
        await Assert.That(member).IsNotNull();

        var response = await client.PostAsJsonAsync(
            DeactivateUrl(IntegrationTestBase.BetaSlug, member!.RoleId), (object?)null);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NoContent);
    }

    [Test]
    [NotInParallel("brand-staff")]
    public async Task DeactivateBrandStaff_LastBrandAdmin_Returns409()
    {
        var client = CreateClient();
        var brandSlug = IntegrationTestBase.AlphaSlug;

        // Invite a fresh anchor so we know exactly which role will be the last
        var anchorEmail = $"last-admin-{Guid.NewGuid():N}@test.com";
        var anchorResponse = await client.PostAsJsonAsync(
            StaffUrl(brandSlug),
            MakeInviteRequest(anchorEmail, "Last Admin", StaffRole.BrandAdmin));
        var anchor = await anchorResponse.Content.ReadFromJsonAsync<StaffMemberResponse>();
        await Assert.That(anchor).IsNotNull();

        // Drain every other BrandAdmin accumulated by previous tests in this run.
        // Sequential execution (NotInParallel) guarantees no new admins arrive mid-drain.
        var listResponse = await client.GetAsync(StaffUrl(brandSlug));
        var currentStaff = await listResponse.Content.ReadFromJsonAsync<List<StaffMemberResponse>>();
        foreach (var other in currentStaff!.Where(s => s.Role == StaffRole.BrandAdmin && s.RoleId != anchor!.RoleId))
            await client.PostAsJsonAsync(DeactivateUrl(brandSlug, other.RoleId), (object?)null);

        // Anchor is now the only BrandAdmin — deactivating it must return 409
        var response = await client.PostAsJsonAsync(
            DeactivateUrl(brandSlug, anchor!.RoleId), (object?)null);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Conflict);
    }

    [Test]
    [NotInParallel("brand-staff")]
    public async Task DeactivateBrandStaff_NotFound_Returns404()
    {
        var client = CreateClient();
        var nonExistentRoleId = Guid.NewGuid();

        var response = await client.PostAsJsonAsync(
            DeactivateUrl(IntegrationTestBase.AlphaSlug, nonExistentRoleId), (object?)null);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    // ── Shop-filtered list ────────────────────────────────────────────────────

    [Test]
    [NotInParallel("brand-staff")]
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

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var shopStaff = await response.Content.ReadFromJsonAsync<List<StaffMemberResponse>>();
        await Assert.That(shopStaff).IsNotNull();
        await Assert.That(shopStaff!.All(s => s.ShopId == shopId)).IsTrue();
    }
}

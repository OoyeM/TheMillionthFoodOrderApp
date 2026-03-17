using System.Net;
using System.Net.Http.Json;
using Shouldly;
using TheMillionthFoodOrderApp.Application.Identity;
using TheMillionthFoodOrderApp.Tests.Integration.Fixtures;

namespace TheMillionthFoodOrderApp.Tests.Integration.PlatformAdmins;

/// <summary>
/// Integration tests for platform admin account endpoints.
/// Runs against a real SQL Server via Testcontainers.
/// </summary>
public sealed class PlatformAdminEndpointTests(IntegrationTestBase fixture)
    : IClassFixture<IntegrationTestBase>
{
    private HttpClient CreateClient() => fixture.Factory.CreateClient();

    private const string AdminsUrl = "/api/platform-admins";

    private static string DeactivateUrl(Guid id) => $"/api/platform-admins/{id}/deactivate";

    private static object MakeInviteRequest(string email, string displayName) =>
        new { Email = email, DisplayName = displayName };

    // ── List ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ListPlatformAdmins_Returns200()
    {
        var client = CreateClient();

        var response = await client.GetAsync(AdminsUrl);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var admins = await response.Content.ReadFromJsonAsync<List<PlatformAdminResponse>>();
        admins.ShouldNotBeNull();
    }

    // ── Invite ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task InvitePlatformAdmin_Returns201_CreatesUser()
    {
        var client = CreateClient();
        var email = $"new-admin-{Guid.NewGuid():N}@test.com";

        var response = await client.PostAsJsonAsync(
            AdminsUrl, MakeInviteRequest(email, "New Admin"));

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        var admin = await response.Content.ReadFromJsonAsync<PlatformAdminResponse>();
        admin.ShouldNotBeNull();
        admin.Id.ShouldNotBe(Guid.Empty);
        admin.Email.ShouldBe(email);
        admin.DisplayName.ShouldBe("New Admin");
        admin.IsPlatformAdmin.ShouldBeTrue();
    }

    [Fact]
    public async Task InvitePlatformAdmin_ExistingNonAdmin_PromotesToAdmin()
    {
        var client = CreateClient();
        var email = $"promote-{Guid.NewGuid():N}@test.com";

        // First invite creates the user as admin
        var firstResponse = await client.PostAsJsonAsync(
            AdminsUrl, MakeInviteRequest(email, "Promotable User"));
        firstResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await firstResponse.Content.ReadFromJsonAsync<PlatformAdminResponse>();
        created.ShouldNotBeNull();

        // Deactivate (revoke admin) so the user is no longer an admin
        // We need at least 2 admins — invite another first
        var otherEmail = $"other-{Guid.NewGuid():N}@test.com";
        await client.PostAsJsonAsync(AdminsUrl, MakeInviteRequest(otherEmail, "Other Admin"));

        var deactivateResponse = await client.PostAsJsonAsync(DeactivateUrl(created.Id), (object?)null);
        deactivateResponse.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Now invite the original email again — should promote the existing user
        var promoteResponse = await client.PostAsJsonAsync(
            AdminsUrl, MakeInviteRequest(email, "Promotable User"));

        promoteResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var promoted = await promoteResponse.Content.ReadFromJsonAsync<PlatformAdminResponse>();
        promoted.ShouldNotBeNull();
        promoted.Id.ShouldBe(created.Id); // Same user, not a new one
        promoted.IsPlatformAdmin.ShouldBeTrue();
    }

    [Fact]
    public async Task InvitePlatformAdmin_InvalidEmail_Returns400()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync(
            AdminsUrl, MakeInviteRequest(string.Empty, "Display Name"));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // ── Deactivate ────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeactivatePlatformAdmin_Returns204()
    {
        var client = CreateClient();

        // Invite a first admin to ensure we have at least one before deactivating a second
        var keepEmail = $"keep-admin-{Guid.NewGuid():N}@test.com";
        await client.PostAsJsonAsync(AdminsUrl, MakeInviteRequest(keepEmail, "Keep Admin"));

        // Invite the admin that will be deactivated
        var deactivateEmail = $"deactivate-{Guid.NewGuid():N}@test.com";
        var inviteResponse = await client.PostAsJsonAsync(
            AdminsUrl, MakeInviteRequest(deactivateEmail, "Temp Admin"));
        var admin = await inviteResponse.Content.ReadFromJsonAsync<PlatformAdminResponse>();
        admin.ShouldNotBeNull();

        var response = await client.PostAsJsonAsync(DeactivateUrl(admin.Id), (object?)null);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task DeactivatePlatformAdmin_LastAdmin_Returns409()
    {
        var client = CreateClient();

        // Invite a single admin, then invite a second, then deactivate the second
        // so only the first remains. Then attempt to deactivate the first → 409.
        // Note: because the shared container may already contain admins from other
        // tests, we seed exactly 2 fresh admins and deactivate all existing ones first.

        // Get all current admins
        var listResponse = await client.GetAsync(AdminsUrl);
        var allAdmins = await listResponse.Content.ReadFromJsonAsync<List<PlatformAdminResponse>>();
        allAdmins.ShouldNotBeNull();

        // Invite a dedicated "anchor" admin that we will keep
        var anchorEmail = $"anchor-{Guid.NewGuid():N}@test.com";
        var anchorResponse = await client.PostAsJsonAsync(
            AdminsUrl, MakeInviteRequest(anchorEmail, "Anchor Admin"));
        var anchor = await anchorResponse.Content.ReadFromJsonAsync<PlatformAdminResponse>();
        anchor.ShouldNotBeNull();

        // Deactivate all pre-existing admins (not the anchor just created)
        foreach (var admin in allAdmins)
        {
            var dr = await client.PostAsJsonAsync(DeactivateUrl(admin.Id), (object?)null);
            // 409 means we hit the last-admin guard — that's fine, stop here
            if (dr.StatusCode == HttpStatusCode.Conflict)
                break;
        }

        // Verify we now have exactly 1 admin (the anchor)
        var currentListResponse = await client.GetAsync(AdminsUrl);
        var currentAdmins = await currentListResponse.Content.ReadFromJsonAsync<List<PlatformAdminResponse>>();
        currentAdmins.ShouldNotBeNull();
        currentAdmins.Count.ShouldBe(1);
        currentAdmins[0].Id.ShouldBe(anchor.Id);

        // Attempt to deactivate the last remaining admin — must return 409
        var lastDeactivateResponse = await client.PostAsJsonAsync(DeactivateUrl(anchor.Id), (object?)null);

        lastDeactivateResponse.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task DeactivatePlatformAdmin_NotFound_Returns404()
    {
        var client = CreateClient();
        var nonExistentId = Guid.NewGuid();

        var response = await client.PostAsJsonAsync(DeactivateUrl(nonExistentId), (object?)null);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}

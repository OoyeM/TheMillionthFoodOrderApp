using System.Net;
using System.Net.Http.Json;
using TheMillionthFoodOrderApp.Application.Identity;
using TheMillionthFoodOrderApp.Tests.Integration.Fixtures;

namespace TheMillionthFoodOrderApp.Tests.Integration.PlatformAdmins;

/// <summary>
/// Integration tests for platform admin account endpoints.
/// Runs against a real SQL Server via Testcontainers.
/// </summary>
[ClassDataSource<IntegrationTestBase>(Shared = SharedType.PerClass)]
public sealed class PlatformAdminEndpointTests(IntegrationTestBase fixture)
{
    private HttpClient CreateClient() => fixture.Factory.CreateClient();

    private const string AdminsUrl = "/api/platform-admins";

    private static string DeactivateUrl(Guid id) => $"/api/platform-admins/{id}/deactivate";

    private static object MakeInviteRequest(string email, string displayName) =>
        new { Email = email, DisplayName = displayName };

    // ── List ──────────────────────────────────────────────────────────────────

    [Test]
    public async Task ListPlatformAdmins_Returns200()
    {
        var client = CreateClient();

        var response = await client.GetAsync(AdminsUrl);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var admins = await response.Content.ReadFromJsonAsync<List<PlatformAdminResponse>>();
        await Assert.That(admins).IsNotNull();
    }

    // ── Invite ────────────────────────────────────────────────────────────────

    [Test]
    public async Task InvitePlatformAdmin_Returns201_CreatesUser()
    {
        var client = CreateClient();
        var email = $"new-admin-{Guid.NewGuid():N}@test.com";

        var response = await client.PostAsJsonAsync(
            AdminsUrl, MakeInviteRequest(email, "New Admin"));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var admin = await response.Content.ReadFromJsonAsync<PlatformAdminResponse>();
        await Assert.That(admin).IsNotNull();
        await Assert.That(admin!.Id).IsNotEqualTo(Guid.Empty);
        await Assert.That(admin.Email).IsEqualTo(email);
        await Assert.That(admin.DisplayName).IsEqualTo("New Admin");
        await Assert.That(admin.IsPlatformAdmin).IsTrue();
    }

    [Test]
    public async Task InvitePlatformAdmin_ExistingNonAdmin_PromotesToAdmin()
    {
        var client = CreateClient();
        var email = $"promote-{Guid.NewGuid():N}@test.com";

        // First invite creates the user as admin
        var firstResponse = await client.PostAsJsonAsync(
            AdminsUrl, MakeInviteRequest(email, "Promotable User"));
        await Assert.That(firstResponse.StatusCode).IsEqualTo(HttpStatusCode.Created);
        var created = await firstResponse.Content.ReadFromJsonAsync<PlatformAdminResponse>();
        await Assert.That(created).IsNotNull();

        // Deactivate (revoke admin) so the user is no longer an admin
        // We need at least 2 admins — invite another first
        var otherEmail = $"other-{Guid.NewGuid():N}@test.com";
        await client.PostAsJsonAsync(AdminsUrl, MakeInviteRequest(otherEmail, "Other Admin"));

        var deactivateResponse = await client.PostAsJsonAsync(DeactivateUrl(created!.Id), (object?)null);
        await Assert.That(deactivateResponse.StatusCode).IsEqualTo(HttpStatusCode.NoContent);

        // Now invite the original email again — should promote the existing user
        var promoteResponse = await client.PostAsJsonAsync(
            AdminsUrl, MakeInviteRequest(email, "Promotable User"));

        await Assert.That(promoteResponse.StatusCode).IsEqualTo(HttpStatusCode.Created);
        var promoted = await promoteResponse.Content.ReadFromJsonAsync<PlatformAdminResponse>();
        await Assert.That(promoted).IsNotNull();
        await Assert.That(promoted!.Id).IsEqualTo(created.Id); // Same user, not a new one
        await Assert.That(promoted.IsPlatformAdmin).IsTrue();
    }

    [Test]
    public async Task InvitePlatformAdmin_InvalidEmail_Returns400()
    {
        var client = CreateClient();

        var response = await client.PostAsJsonAsync(
            AdminsUrl, MakeInviteRequest(string.Empty, "Display Name"));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.BadRequest);
    }

    // ── Deactivate ────────────────────────────────────────────────────────────

    [Test]
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
        await Assert.That(admin).IsNotNull();

        var response = await client.PostAsJsonAsync(DeactivateUrl(admin!.Id), (object?)null);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task DeactivatePlatformAdmin_LastAdmin_Returns409()
    {
        var client = CreateClient();

        // Create the anchor admin first so it exists before we start draining others.
        // Parallel tests may create admins at any point, so we loop until the anchor
        // is the only one left rather than doing a one-shot deactivation pass.
        var anchorEmail = $"anchor-{Guid.NewGuid():N}@test.com";
        var anchorResponse = await client.PostAsJsonAsync(
            AdminsUrl, MakeInviteRequest(anchorEmail, "Anchor Admin"));
        var anchor = await anchorResponse.Content.ReadFromJsonAsync<PlatformAdminResponse>();
        await Assert.That(anchor).IsNotNull();

        List<PlatformAdminResponse>? remaining;
        do
        {
            var listResponse = await client.GetAsync(AdminsUrl);
            remaining = await listResponse.Content.ReadFromJsonAsync<List<PlatformAdminResponse>>();
            foreach (var other in remaining!.Where(a => a.Id != anchor!.Id))
                await client.PostAsJsonAsync(DeactivateUrl(other.Id), (object?)null);
        }
        while (remaining!.Any(a => a.Id != anchor!.Id));

        // Attempt to deactivate the last remaining admin — must return 409
        var lastDeactivateResponse = await client.PostAsJsonAsync(DeactivateUrl(anchor!.Id), (object?)null);

        await Assert.That(lastDeactivateResponse.StatusCode).IsEqualTo(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task DeactivatePlatformAdmin_NotFound_Returns404()
    {
        var client = CreateClient();
        var nonExistentId = Guid.NewGuid();

        var response = await client.PostAsJsonAsync(DeactivateUrl(nonExistentId), (object?)null);

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }
}

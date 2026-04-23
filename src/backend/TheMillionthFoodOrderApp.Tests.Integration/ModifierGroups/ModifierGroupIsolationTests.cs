using System.Net;
using System.Net.Http.Json;
using TheMillionthFoodOrderApp.Application.ModifierGroups;
using TheMillionthFoodOrderApp.Tests.Integration.Fixtures;

namespace TheMillionthFoodOrderApp.Tests.Integration.ModifierGroups;

/// <summary>
/// Integration tests verifying cross-brand isolation for modifier groups.
/// Modifier groups created in Brand Alpha must not be visible to Brand Beta.
/// </summary>
[ClassDataSource<IntegrationTestBase>(Shared = SharedType.PerClass)]
public sealed class ModifierGroupIsolationTests(IntegrationTestBase fixture)
{
    private HttpClient CreateClient() => fixture.Factory.CreateClient();

    private static string ModifierGroupsUrl(string brandSlug) =>
        $"/api/brands/{brandSlug}/modifier-groups";

    [Test]
    public async Task ModifierGroupsCreatedInAlpha_NotVisibleInBeta()
    {
        var client = CreateClient();

        // Create a modifier group in Alpha
        var request = new
        {
            Translations = new[] { new { LanguageCode = "nl", Name = "Alpha-Only Groep" } },
            Modifiers = new[]
            {
                new
                {
                    PriceAdjustment = 0.50m,
                    SortOrder = 0,
                    Translations = new[] { new { LanguageCode = "nl", Name = "Alpha Optie" } }
                },
            }
        };

        var createResponse = await client.PostAsJsonAsync(
            ModifierGroupsUrl(IntegrationTestBase.AlphaSlug), request);
        await Assert.That(createResponse.StatusCode).IsEqualTo(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<ModifierGroupResponse>();
        await Assert.That(created).IsNotNull();

        // List modifier groups in Beta — should not contain Alpha's group
        var betaListResponse = await client.GetAsync(ModifierGroupsUrl(IntegrationTestBase.BetaSlug));
        await Assert.That(betaListResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var betaGroups = await betaListResponse.Content.ReadFromJsonAsync<List<ModifierGroupListItemResponse>>();
        await Assert.That(betaGroups).IsNotNull();
        await Assert.That(betaGroups!.Any(g => g.Id == created!.Id)).IsFalse();

        // Confirm Alpha's own list does contain the group
        var alphaListResponse = await client.GetAsync(ModifierGroupsUrl(IntegrationTestBase.AlphaSlug));
        await Assert.That(alphaListResponse.StatusCode).IsEqualTo(HttpStatusCode.OK);

        var alphaGroups = await alphaListResponse.Content.ReadFromJsonAsync<List<ModifierGroupListItemResponse>>();
        await Assert.That(alphaGroups).IsNotNull();
        await Assert.That(alphaGroups!.Any(g => g.Id == created!.Id)).IsTrue();
    }
}

using System.Net;
using System.Net.Http.Json;
using Shouldly;
using TheMillionthFoodOrderApp.Application.ModifierGroups;
using TheMillionthFoodOrderApp.Tests.Integration.Fixtures;

namespace TheMillionthFoodOrderApp.Tests.Integration.ModifierGroups;

/// <summary>
/// Integration tests verifying cross-brand isolation for modifier groups.
/// Modifier groups created in Brand Alpha must not be visible to Brand Beta.
/// </summary>
public sealed class ModifierGroupIsolationTests(IntegrationTestBase fixture)
    : IClassFixture<IntegrationTestBase>
{
    private HttpClient CreateClient() => fixture.Factory.CreateClient();

    private static string ModifierGroupsUrl(string brandSlug) =>
        $"/api/brands/{brandSlug}/modifier-groups";

    [Fact]
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
        createResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var created = await createResponse.Content.ReadFromJsonAsync<ModifierGroupResponse>();
        created.ShouldNotBeNull();

        // List modifier groups in Beta — should not contain Alpha's group
        var betaListResponse = await client.GetAsync(ModifierGroupsUrl(IntegrationTestBase.BetaSlug));
        betaListResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var betaGroups = await betaListResponse.Content.ReadFromJsonAsync<List<ModifierGroupListItemResponse>>();
        betaGroups.ShouldNotBeNull();
        betaGroups.ShouldNotContain(g => g.Id == created.Id);

        // Confirm Alpha's own list does contain the group
        var alphaListResponse = await client.GetAsync(ModifierGroupsUrl(IntegrationTestBase.AlphaSlug));
        alphaListResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var alphaGroups = await alphaListResponse.Content.ReadFromJsonAsync<List<ModifierGroupListItemResponse>>();
        alphaGroups.ShouldNotBeNull();
        alphaGroups.ShouldContain(g => g.Id == created.Id);
    }
}
